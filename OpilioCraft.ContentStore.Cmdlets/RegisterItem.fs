namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsLifecycle.Register, "Item", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<ItemId>)>]
type public RegisterItemCommand () =
    inherit RepositoryCommandBase ()

    [<Parameter>]
    member val ContentCategory = String.Empty with get, set

    override x.BeginProcessing () =
        base.BeginProcessing () // initialize MMToolkit

        try
            x.ContentStoreManager.UseExifTool()
        with
            | exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.NotSpecified

    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            x.AssertPathProvided()

            x.TryInputAsPath()
            |> Option.map ( fun path -> path |> IO.FileInfo |> Utils.identifyFile )
            |> Option.bind (
                fun fident ->
                    if x.ContentCategory |> String.IsNullOrEmpty
                    then
                        x.ActiveRepository.AddToRepository(fident) |> Some
                    else
                        x.ContentCategory
                        |> Utils.tryParseContentCategory
                        |> x.WarnIfNone $"invalid content category: {x.ContentCategory}"
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