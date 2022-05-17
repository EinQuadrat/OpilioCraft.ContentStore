namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation

open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Rename, "ItemUsingMetadata", DefaultParameterSetName="ByPath")>]
[<OutputType(typeof<Void>)>]
type public RenameManagedFileCommand () =
    inherit RepositoryCommandExtended ()

    // filename creator
    let mutable applyPattern : RepositoryItem -> string = fun item -> item.Id

    // cmdlet params
    [<Parameter>]
    member val NamePattern = "{date|yyyyMMddTHHmmmss}_{seqno}_{owner}#{id}" with get, set

    [<Parameter>]
    member val ResetFileDate = SwitchParameter(false) with get,set

    [<Parameter>]
    member val WhatIf = SwitchParameter(false) with get,set

    // cmdlet funtionality
    override x.BeginProcessing() =
        base.BeginProcessing()

        try
            applyPattern <- StringTemplateHelper.FilenameCreator.Initialize(x.NamePattern).Apply
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified

    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.AssertPathProvided()
            x.Path <- x.ToAbsolutePath x.Path

            x.Path
            |> x.AssertFileExists $"given file does not exist or is not accessible: {x.Path}"
            |> Fingerprint.fingerprintAsString

            |> Some
            |> x.AssertIsManagedItem
            |> Option.get

            |> fun id ->
                let item = x.ActiveRepository.GetItem id
                let filename = (applyPattern item) + item.ContentType.FileExtension
                let targetPath = IO.Path.Combine(Path.GetDirectoryName(x.Path), filename)

                if not <| x.Path.Equals(targetPath) // skip unchanged names
                then
                    if x.WhatIf.IsPresent
                    then
                        System.Console.WriteLine $"rename {x.Path} to {targetPath}"
                    else
                        x.WriteVerbose($"renaming {x.Path} to {targetPath}")
                        IO.File.Move(x.Path, targetPath, false)
                else
                    x.WriteVerbose($"skipping {x.Path}")

                if x.ResetFileDate.IsPresent
                then
                    let dateTaken = item.Details.["DateTaken"] |> function | ItemDetail.DateTime dateTime -> dateTime | _ -> failwith "expected data type" 
                    IO.File.SetCreationTimeUtc(targetPath, dateTaken)
                    IO.File.SetLastWriteTimeUtc(targetPath, dateTaken)
                    IO.File.SetLastAccessTimeUtc(targetPath, dateTaken)

        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
