namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.ContentStore.Core
open OpilioCraft.FSharp.Prelude.TryWrapper

[<Cmdlet(VerbsCommon.Set, "DateTaken", DefaultParameterSetName="ByPathShift")>]
[<OutputType(typeof<Void>)>]
type public SetDateTakenCommand () =
    inherit RepositoryCommandBase ()

    let adjustDateTaken (itemDetails : ItemDetails) (offset : TimeSpan) =
        let tryGetValue = tryWrapper itemDetails.TryGetValue

        let isDateTime =
            function
            | ItemDetail.DateTime dateTime -> Some dateTime
            | _ -> None
    
        let dateTaken = tryGetValue Slot.DateTaken |> Option.bind isDateTime
        let dateTakenOriginal = tryGetValue Slot.DateTakenOriginal |> Option.bind isDateTime |> Option.orElse dateTaken

        match dateTaken, dateTakenOriginal with
        | (Some dt, Some dto) ->
            [
                (Slot.DateTakenOriginal, dto |> ItemDetail.DateTime)
                (Slot.DateTakenOffset, offset |> ItemDetail.TimeSpan)
                (Slot.DateTaken, dt.Add(offset) |> ItemDetail.DateTime)
            ] |> Map.ofSeq
        | _ -> Map.empty

    // cmdlet params
    [<Parameter(ParameterSetName="ByPathShift", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Parameter(ParameterSetName="ByPathReset", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    [<Parameter(ParameterSetName="ByIdShift", Position=0, Mandatory=true)>]
    [<Parameter(ParameterSetName="ByIdReset", Position=0, Mandatory=true)>]
    member val Id = String.Empty with get, set

    [<Parameter(ParameterSetName="ByPathReset")>]
    [<Parameter(ParameterSetName="ByIdReset")>]
    member val Reset = SwitchParameter(false) with get, set

    [<Parameter(ParameterSetName="ByPathShift")>]
    [<Parameter(ParameterSetName="ByIdShift")>]
    member val Shift = TimeSpan.Zero with get, set

    // cmdlet funtionality
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
            |> Option.iter
                ( fun item ->
                    if x.Reset.IsPresent && item.Details.ContainsKey(Slot.DateTakenOriginal)
                    then
                        item.Details.[Slot.DateTakenOriginal]
                        |> x.ActiveRepository.SetDetail item.Id Slot.DateTaken
                        x.ActiveRepository.UnsetDetails item.Id [ Slot.DateTakenOriginal; Slot.DateTakenOffset ]
                    else if not x.Reset.IsPresent
                    then
                        let modifications = adjustDateTaken item.Details x.Shift in
                        x.ActiveRepository.SetDetails item.Id modifications
                )
        with
        | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
