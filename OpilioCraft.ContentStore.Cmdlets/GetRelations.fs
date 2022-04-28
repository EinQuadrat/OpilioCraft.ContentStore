namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "Relations", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<Relation list>)>]
type public GetRelationsCommand () =
    inherit RepositoryCommandBase ()

    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            x.TryDetermineItemId ()
            |> x.AssertIsManagedItem "Get-Relation"
            |> Option.map x.ActiveRepository.GetRelations
            |> Option.iter x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
