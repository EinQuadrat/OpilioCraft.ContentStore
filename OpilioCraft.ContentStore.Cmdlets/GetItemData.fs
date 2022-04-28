namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "ItemData", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<RepositoryItem>)>]
type public GetItemDataCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.TryDetermineItemId ()
            |> x.AssertIsManagedItem "Get-ItemData"
            |> Option.map x.ActiveRepository.GetItem
            |> Option.iter x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
