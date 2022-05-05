namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Reset, "ItemDetails", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<ItemDetails>, typeof<Void>)>]
type public ResetItemDetailsCommand () =
    inherit RepositoryCommandExtended ()

    // cmdlet params
    [<Parameter>]
    member val WhatIf = SwitchParameter(false) with get, set

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.RetrieveItemId()
            |> x.AssertIsManagedItem
            |> Option.get

            |> fun id ->
                if x.WhatIf.IsPresent
                then
                    id |> x.ActiveRepository.ReadDetailsFromFile |> x.WriteObject
                else
                    id |> x.ActiveRepository.ResetDetails

        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
