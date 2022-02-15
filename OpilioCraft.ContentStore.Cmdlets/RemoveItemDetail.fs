namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Remove, "ItemDetail", DefaultParameterSetName="ByPath")>]
[<OutputType(typeof<Void>)>]
type public RemoveItemDetailCommand () =
    inherit RepositoryCommandBase ()

    // validator
    static let isWriteable =
        function
        | Slot.Camera
        | Slot.DateTaken -> false
        | slot when slot.StartsWith(SlotPrefix.ExifTool) -> false
        | _ -> true

    // cmdlet params
    [<Parameter(ParameterSetName="ByPath", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    [<Parameter(ParameterSetName="ById", Position=0, Mandatory=true)>]
    member val Id = String.Empty with get, set

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
            if x.Id |> String.IsNullOrEmpty // parameter set ByPath?
            then
                x.Path
                |> x.ToAbsolutePath
                |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
                |> Option.map Fingerprint.fingerprintAsString
            else
                x.Id
                |> Some

            |> x.Assert x.ActiveRepository.IsManagedId $"given id is unknown: {x.Id}"

            |> Option.iter ( fun id -> x.ActiveRepository.UnsetDetail id x.Name )
        with
        | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
