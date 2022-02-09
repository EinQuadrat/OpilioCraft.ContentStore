namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation

[<Cmdlet(VerbsLifecycle.Register, "Item")>]
[<OutputType(typeof<ItemId>)>]
type public RegisterItemCommand () =
    inherit RepositoryCommandBase ()

    [<Parameter(Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = System.String.Empty with get, set

    override x.BeginProcessing () =
        base.BeginProcessing () // initialize MMToolkit

        try
            x.ContentStoreManager.Value.UseExifTool()
        with
        | exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.NotSpecified

    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            x.Path
            |> x.ToAbsolutePath
            |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
            |> Option.map ( fun path -> { Path = path; Id = path |> x.ActiveRepository.AddToRepository } )
            |> Option.iter x.WriteObject
        with
        | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified

and ItemId =
    {
        Path : string
        Id : string
    }