namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation

[<Cmdlet(VerbsCommon.Get, "ContentType")>]
[<OutputType(typeof<ContentType>)>]
type public GetContentTypeCommand () =
    inherit CommandBase ()

    [<Parameter(Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    override _.BeginProcessing () =
        base.BeginProcessing () // initialize MMToolkit

    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            x.Path
            |> x.ToAbsolutePath
            |> x.AssertFileExists $"given file does not exist or is not accessible: {x.Path}"
            |> fun path -> 
                {
                    Path = path
                    ContentType = path |> IO.FileInfo |> OpilioCraft.ContentStore.Core.Utils.getContentType
                }
            |> x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified

and ContentType =
    {
        Path : string
        ContentType : OpilioCraft.ContentStore.Core.ContentType
    }