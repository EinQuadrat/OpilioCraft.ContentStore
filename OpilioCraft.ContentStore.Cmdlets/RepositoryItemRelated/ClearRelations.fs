namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.FSharp.Prelude

[<Cmdlet(VerbsCommon.Clear, "Relations", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<Void>)>]
type public ClearRelationsCommand () =
    inherit RepositoryCommandExtended ()

    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            x.RetrieveItemId()
            |> x.AssertIsManagedItem
            |> Option.iter x.ActiveRepository.ForgetRelations
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
