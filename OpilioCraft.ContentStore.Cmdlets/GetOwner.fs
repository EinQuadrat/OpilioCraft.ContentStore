namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "Owner", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<string>)>]
type public GetOwnerCommand () =
    inherit RepositoryCommandExtended ()

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.RetrieveItemId()
            |> x.AssertIsManagedItem

            |> Option.map x.ActiveRepository.GetItem
            |> Option.bind (ContentStoreManager.tryApplyRule "GuessOwner")
            |> Option.defaultValue "#UNKNOWN"

            |> x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
