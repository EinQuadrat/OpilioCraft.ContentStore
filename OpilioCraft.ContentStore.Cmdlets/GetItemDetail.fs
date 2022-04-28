namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "ItemDetail", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<obj>)>]
type public GetItemDetailCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet params
    [<Parameter(Position=1, Mandatory=true)>]
    member val Name = String.Empty with get, set

    [<Parameter(Position=2)>]
    member val DefaultValue = String.Empty with get, set

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.RetrieveItemId()
            |> x.AssertIsManagedItem

            |> Option.map x.ActiveRepository.GetItem

            |> Option.iter (
                fun item ->
                    if item.Details.ContainsKey(x.Name)
                    then
                        item.Details.Item(x.Name).ToString()
                    else
                        x.DefaultValue
                    
                    |> x.WriteObject
                )
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
