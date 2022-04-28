namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.FSharp.Prelude


[<Cmdlet(VerbsCommon.Get, "Owner", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<string>)>]
type public GetOwnerCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.RetrieveItemId()
            |> x.AssertIsManagedItem

            |> Option.map x.ActiveRepository.GetItem
            |> Option.bind (x.ContentStoreManager.RulesProvider.TryApplyRule "GuessOwner")
            |> Option.defaultValue "#UNKNOWN"

            |> x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
