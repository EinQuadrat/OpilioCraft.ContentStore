namespace OpilioCraft.ContentStore.Core

// system
open System
open System.Diagnostics
open System.Globalization
open System.IO
open System.Threading

open FSharp.Data
open FSharp.Data.JsonExtensions

open OpilioCraft.FSharp.Prelude


// managed exiftool result structure
type ExifToolResult(jsonString : string) =
    member val RawData = jsonString
        // IMPORTANT: exiftool delivers a json array containing a record with all tags

    member val ParsedJson =
        jsonString
        |> JsonValue.Parse
        |> fun jsonValue -> jsonValue.AsArray().[0]
        // IMPORTANT: be sure to keep Exif.EmptyInstance in sync with exiftool.exe result structure
            
    member x.Item
        with get (name : string) =
#if DEBUG
            System.Console.Out.WriteLine $"[DEBUG] ExifToolResult: requested item: {name}"
#endif
            x.ParsedJson.TryGetProperty name

    static member val EmptyInstance = ExifToolResult @"[{}]"
        // minimal value compatible with exiftool.exe result

// ------------------------------------------------------------------------------------------------

// integration of exiftool.exe
type ExifToolCommand =
    | RequestMetadata of Filename:string * AsyncReplyChannel<ExifToolResult>
    | Quit

module ExifTool =
    let exifExecutable = Path.Combine(Settings.RuntimeBase, "exiftool.exe")
    let exifArgsFile = Path.Combine(Settings.AppDataLocation, "exiftool_args.txt")

    do
        if not <| File.Exists exifExecutable then failwith "[ExifTool] exiftool executable not accessible"
    
    let launchExif () =
        try
            // create empty args file; at least one line is preventing rare error conditions
            File.WriteAllText(exifArgsFile, "# MKToolkit argfile\n")

            // create start info
            let psi = ProcessStartInfo(exifExecutable)
            psi.Arguments <- $"-stay_open true -@ \"{exifArgsFile}\" -common_args -json -G0 --composite:all --directory --filename --creatortool -d \"%%Y-%%m-%%dT%%H:%%M:%%S%%z\" -charset FileName=UTF8"
            // psi.Arguments <- $"-stay_open true -@ \"{exifArgsFile}\" -common_args -json -d \"%%Y-%%m-%%dT%%H:%%M:%%S\" --creatortool -charset FileName=UTF8"
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true
            psi.UseShellExecute <- false
            psi.CreateNoWindow <- false

            // async error handler
            let exifErrorHandler (_ : obj) (errLine : DataReceivedEventArgs) : unit =
                if (not <| String.IsNullOrEmpty(errLine.Data)) then Console.Error.WriteLine $"[ExifTool] exiftool.exe error: {errLine.Data}"

            // create process instance
            let ps = new Process()
            ps.StartInfo <- psi
            ps.ErrorDataReceived.AddHandler(new DataReceivedEventHandler(exifErrorHandler))

            // run it
            ps.Start() |> ignore
            ps.BeginErrorReadLine()
            ps
        with
        | exn -> Console.Error.Write $"[ExifTool] cannot launch exiftool: {exn.Message}"; reraise()

    let createExifToolHandler () = lazy MailboxProcessor<ExifToolCommand>.Start(fun inbox ->
        let exifRuntime = launchExif ()

        let readResponse (processInstance : Process) =
            let rec readLines (response : string) =
                match processInstance.StandardOutput.ReadLine() with
                | "{ready}" -> response
                | line -> readLines (response + line)
            
            readLines ""

        let rec loop () =
            async {
                let! msg = inbox.Receive()

                match msg with
                | RequestMetadata(filename, replyChannel) ->
                    try
                        File.AppendAllLines(exifArgsFile, [ filename; "-execute" ])
                        exifRuntime |> readResponse |> ExifToolResult
                    with
                    | exn ->
                        System.Console.Error.WriteLine $"[ExifTool] error while processing request: {exn.Message}"
                        ExifToolResult.EmptyInstance

                    |> replyChannel.Reply

                    return! loop ()

                | Quit ->
                    try
                        File.AppendAllLines(exifArgsFile, [ "-stay_open"; "False" ])
                        exifRuntime.StandardOutput.ReadToEnd() |> ignore
                        exifRuntime.WaitForExit()
                        exifArgsFile |> File.Delete
                    with
                    | exn -> System.Console.Error.WriteLine $"[ExifTool] error while terminating processing instance: {exn.Message}"
                    
                    return ()
            }

        loop ()
    )

[<Sealed>]
type ExifTool() =
    inherit DisposableBase ()

    static let _refCounter = ref 0
    static let mutable _exifHandler = None

    do
        if Interlocked.Increment _refCounter = 1 then // init needed?
            _exifHandler <- Some <| ExifTool.createExifToolHandler ()

    // guarded exiftool access
    static member private Mailbox =
        match _exifHandler with
        | Some x -> x.Value
        | None -> failwith "[ExifTool] illegal internal state"

    // more fluent instance creation
    static member Proxy() = new ExifTool()

    // public API
    member _.RequestMetadata filename =
        ExifTool.Mailbox.PostAndReply(fun reply -> RequestMetadata(filename, reply))


    // cleanup resources
    override _.DisposeManagedResources () =
        if _exifHandler.IsSome then
            if Interlocked.Decrement _refCounter < 1 then
                ExifTool.Mailbox.Post(Quit)
                _exifHandler <- None

module ExifToolHelper =
    // simplify dealing with Exif structure
    let asString = function | JsonValue.String x -> x | x -> x.ToString()
    let asTrimString = asString >> Text.trim

    let tryAsDateTime (jvalue : JsonValue) =
        try
            if jvalue = JsonValue.Null || String.IsNullOrEmpty(jvalue.AsString()) || jvalue.AsString().Equals("0000:00:00 00:00:00")
            then
                None
            else
                jvalue.AsDateTime(CultureInfo.InvariantCulture) |> Some
        with
        | _ -> Console.Error.WriteLine $"[ExifTool] value is not DateTime compatible: {jvalue.ToString()}"; None

    let tryExtractCamera (exif : ExifToolResult) : string option =
        let make = exif.["EXIF:Make"] |> Option.map asTrimString |> Option.defaultValue ""
        let model = exif.["EXIF:Model"] |> Option.map asTrimString |> Option.defaultValue ""
    
        match make, model with
        | "", "" -> None
        | "", model -> Some model
        | make, "" -> Some make
        | make, model -> (if (model.StartsWith(make)) then model else $"{make} {model}") |> Some

    // simplify metadata extraction
    let getMetadata (fi : FileInfo) : ExifToolResult option =
        try
            using (ExifTool.Proxy()) ( fun exifTool -> exifTool.RequestMetadata fi.FullName |> Some )
        with
        | exn ->
            System.Console.Error.WriteLine $"[ExifTool] cannot process request: {exn.Message}"
            None
