namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation

open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Rename, "ManagedFile")>]
[<OutputType(typeof<Void>)>]
type public RenameManagedFileCommand () =
    inherit RepositoryCommandBase ()

    // filename creator
    let mutable getFilename : RepositoryItem -> string = fun item -> item.Id + item.ContentType.FileExtension

    // cmdlet params
    [<Parameter(Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    [<Parameter(Position=1)>]
    member val NamePattern = "{date|yyyyMMddThhmmmss}_{seqno}_{owner}#{id}" with get, set

    [<Parameter>]
    member val ResetFileDate = SwitchParameter(false) with get,set

    [<Parameter>]
    member val WhatIf = SwitchParameter(false) with get,set

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
            x.Path <- x.ToAbsolutePath x.Path

            x.Path
            |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
            |> Option.map Fingerprint.fingerprintAsString

            |> x.Assert x.ActiveRepository.IsManagedId $"{x.Path} is not a managed file"

            |> Option.iter (
                fun id ->
                    let item = x.ActiveRepository.GetItem id
                    let filename = getFilename item
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
                        IO.File.SetCreationTimeUtc(targetPath, item.AsOf)
                        IO.File.SetLastWriteTimeUtc(targetPath, item.AsOf)
                        IO.File.SetLastAccessTimeUtc(targetPath, item.AsOf)
                )
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
