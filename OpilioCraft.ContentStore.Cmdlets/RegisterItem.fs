namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsLifecycle.Register, "Item")>]
[<OutputType(typeof<ItemId>)>]
type public RegisterItemCommand () =
    inherit RepositoryCommandBase ()

    [<Parameter(Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = System.String.Empty with get, set

    [<Parameter>]
    member val ContentCategory = String.Empty with get, set

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
            |> Option.map ( fun path -> path |> IO.FileInfo |> Utils.identifyFile )
            |> Option.bind ( fun fident ->
                if x.ContentCategory |> String.IsNullOrEmpty
                then
                    x.ActiveRepository.AddToRepository(fident) |> Some
                else
                    x.ContentCategory
                    |> Utils.tryParseContentCategory
                    |> x.WarningIfNone $"invalid content category: {x.ContentCategory}"
                    |> Option.map ( fun category -> x.ActiveRepository.AddToRepository(fident, category) )

                |> Option.map ( fun itemId -> { Path = fident.FileInfo.FullName; Id = itemId } )
                )
            |> Option.iter x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified

and ItemId =
    {
        Path : string
        Id : string
    }