namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.ContentStore.Core
open OpilioCraft.FSharp.Prelude
open OpilioCraft.FSharp.Prelude.TryWrapper

[<Cmdlet(VerbsCommon.Set, "DateTaken", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<Void>)>]
type public SetDateTakenCommand () =
    inherit RepositoryCommandBase ()

    let adjustDateTaken (itemDetails : ItemDetails) (offset : TimeSpan) =
        let tryGetValue = tryWrapper itemDetails.TryGetValue

        let isDateTime = function
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
    [<Parameter>]
    member val Reset = SwitchParameter(false) with get, set

    [<Parameter>]
    member val Shift = TimeSpan.Zero with get, set

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.TryDetermineItemId ()
            |> x.AssertIsManagedItem "Set-DateTaken"
            |> Option.get

            |> x.ActiveRepository.GetItem
            |> fun item ->
                if x.Reset.IsPresent && item.Details.ContainsKey(Slot.DateTakenOriginal)
                then
                    item.Details.[Slot.DateTakenOriginal]
                    |> x.ActiveRepository.SetDetail item.Id Slot.DateTaken
                    x.ActiveRepository.UnsetDetails item.Id [ Slot.DateTakenOriginal; Slot.DateTakenOffset ]
                else if not x.Reset.IsPresent
                then
                    let modifications = adjustDateTaken item.Details x.Shift in
                    x.ActiveRepository.SetDetails item.Id modifications
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
