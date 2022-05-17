namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation

open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsData.Export, "ItemFile", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<Void>)>]
type public ExportItemFileCommand () =
    inherit RepositoryCommandExtended ()

    // filename creator
    let mutable getFilename : RepositoryItem -> string = fun item -> item.Id + item.ContentType.FileExtension

    // cmdlet params
    [<Parameter(Position=1, Mandatory=true)>]
    member val TargetPath = String.Empty with get, set

    [<Parameter>]
    member val NamePattern = "{date|yyyyMMddThhmmmss}_{seqno}_{owner}#{id}" with get, set

    [<Parameter>]
    member val SkipExisting = SwitchParameter(false) with get,set

    [<Parameter>]
    member val Overwrite = SwitchParameter(false) with get,set

    // cmdlet funtionality
    override x.BeginProcessing() =
        base.BeginProcessing()

        try
            getFilename <- StringTemplateHelper.FilenameCreator.Initialize(x.NamePattern).Apply
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified

    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.AssertItemIdProvided()

            x.RetrieveItemId()
            |> x.AssertIsManagedItem
            |> Option.iter (
                fun id ->
                    let item = x.ActiveRepository.GetItem id
                    let filename = getFilename item
                    let targetPath = IO.Path.Combine(x.TargetPath, filename)
                    let targetExists = IO.File.Exists(targetPath)

                    match (targetExists, x.SkipExisting.IsPresent) with
                    | false, _ -> Some targetPath
                    | true, false when x.Overwrite.IsPresent -> Some targetPath
                    | true, true -> None
                    | _ -> failwith $"file already exists; specify -Overwrite to replace it: {targetPath}"
                    |> Option.iter (fun targetPath -> x.ActiveRepository.ExportFile id targetPath x.Overwrite.IsPresent)
                )
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
