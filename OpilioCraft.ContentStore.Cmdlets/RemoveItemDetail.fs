namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Remove, "ItemDetail", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<Void>)>]
type public RemoveItemDetailCommand () =
    inherit RepositoryCommandBase ()

    // validator
    static let isWriteable = function
        | Slot.Camera
        | Slot.DateTaken -> false
        | slot when slot.StartsWith(SlotPrefix.ExifTool) -> false
        | _ -> true

    // cmdlet params
    [<Parameter(Position=1, Mandatory=true)>]
    member val Name = String.Empty with get, set

    // cmdlet funtionality
    override x.BeginProcessing() =
        base.BeginProcessing () // initialize MMToolkit

        if not <| isWriteable x.Name
        then
            x.ThrowValidationError $"detail slot is read-only: {x.Name}" ErrorCategory.InvalidArgument

    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.RetrieveItemId()
            |> x.AssertIsManagedItem
            |> Option.iter ( fun id -> x.ActiveRepository.UnsetDetail id x.Name )
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
