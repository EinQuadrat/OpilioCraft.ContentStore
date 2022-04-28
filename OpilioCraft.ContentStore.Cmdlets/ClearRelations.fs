namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.FSharp.Prelude

[<Cmdlet(VerbsCommon.Clear, "Relations", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<Void>)>]
type public ClearRelationsCommand () =
    inherit RepositoryCommandBase ()

    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            x.TryDetermineItemId ()
            |> x.AssertIsManagedItem "Clear-Relations"
            |> Option.iter x.ActiveRepository.ForgetRelations
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
