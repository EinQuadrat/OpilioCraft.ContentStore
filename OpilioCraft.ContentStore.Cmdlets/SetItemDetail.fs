namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.ContentStore.Core
open OpilioCraft.FSharp.Prelude.ActivePatterns

[<Cmdlet(VerbsCommon.Set, "ItemDetail", DefaultParameterSetName="ByPath")>]
[<OutputType(typeof<Void>)>]
type public SetItemDetailCommand () =
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

    [<Parameter(Position=2, Mandatory=true)>]
    member val Value = String.Empty with get, set

    [<Parameter>]
    member val AsString = SwitchParameter(false) with get, set

    // cmdlet funtionality
    override x.BeginProcessing() =
        if not <| isWriteable x.Name
        then
            x.ThrowValidationError $"detail member is read-only: {x.Name}" ErrorCategory.InvalidArgument

    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            if x.Id |> String.IsNullOrEmpty // parameterset ByPath?
            then
                x.Path
                |> x.ToAbsolutePath
                |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
                |> Option.map Fingerprint.fingerprintAsString
            else
                x.Id
                |> Some

            |> x.Assert x.ActiveRepository.IsManagedId $"given id is unknown: {x.Id}"
            |> Option.map x.ActiveRepository.FetchItem

            // automatic type conversion
            |> Option.map
                ( fun item ->
                    item,
                    match x.Value with
                    | v when x.AsString.IsPresent -> ItemDetail.String v    // ignore value type if requested

                    | IsBoolean v -> ItemDetail.Boolean v // try to detect the represented type
                    | IsDateTime v -> ItemDetail.DateTime v
                    | IsDecimal v -> ItemDetail.Number v
                    | IsTimeSpan v -> ItemDetail.TimeSpan v

                    | v -> ItemDetail.String v // default case
                )
            
            // store detail into repository
            |> Option.iter ( fun (item, itemDetail) -> x.ActiveRepository.SetDetail item.Id x.Name itemDetail )
        with
        | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
