namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.FSharp.Prelude

[<Cmdlet(VerbsLifecycle.Unregister, "Item", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<Void>)>]
type public UnregisterItemCommand () =
    inherit RepositoryCommandExtended ()

    // cmdlet functionality
    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            x.RetrieveItemId()
            |> x.AssertIsManagedItem
            |> Option.iter x.ActiveRepository.Forget
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
