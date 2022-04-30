module internal OpilioCraft.ContentStore.Cmdlets.StringTemplateHelper

open System

open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core
open OpilioCraft.StringTemplate

[<Literal>]
let DefaultDateTimeFormat = "yyyyMMddTHHmmss"

let tryGetDetail slot reposItem : ItemDetail option =
    reposItem.Details
    |> TryWrapper.tryGetValue slot
    
let getDateTakenUTC (reposItem : RepositoryItem) (args : string list) : string =
    let format =
        match args with
        | format :: _ -> format
        | _ -> DefaultDateTimeFormat

    reposItem.AsOf.ToString(format)

let getDateTaken (reposItem : RepositoryItem) (args : string list) : string =
    let (format, timezone) =
        match args with
        | [ format ; timezoneId ] -> format, TimeZoneInfo.FindSystemTimeZoneById(timezoneId)
        | [ format ] -> format, TimeZoneInfo.Local
        | [] -> DefaultDateTimeFormat, TimeZoneInfo.Local
        | _ -> failwith "date expects zero, one or two arguments"

    TimeZoneInfo.ConvertTimeFromUtc(reposItem.AsOf, timezone).ToString(format)

let getOwner (reposItem : RepositoryItem) _ : string =
    reposItem
    |> tryGetDetail "Owner"
    |> Option.bind (function | ItemDetail.String owner -> owner |> Some | _ -> None)
    |> Option.defaultValue "NA"

let getSeqNo (reposItem : RepositoryItem) _ : string =
    reposItem
    |> tryGetDetail "ExifTool:MakerNotes:SequenceImageNumber"
    |> Option.bind (function | ItemDetail.Number seqNo -> seqNo.ToString() |> Some | _ -> None)
    |> Option.defaultValue ""

let getId (reposItem : RepositoryItem) _ : string =
    reposItem.Id

let genericPlaceholderMap : Map<string, RepositoryItem -> string list -> string> =
    Map.ofList
        [
            "id", getId
            "date", getDateTaken
            "date-utc", getDateTakenUTC
            "owner", getOwner
            "seqno", getSeqNo
        ]

type FilenameCreator ( stringTemplate : StringTemplate ) =
    member _.Apply (item : RepositoryItem) : string =
        let placeholderMap =
            genericPlaceholderMap
            |> Map.map (fun _ (body : RepositoryItem -> string list -> string) -> body item)

        let filename =
            OpilioCraft.StringTemplate.Runtime.EvalRelaxed placeholderMap stringTemplate

        // automatically add extension
        filename + item.ContentType.FileExtension

    static member Initialize ( namePattern : string ) =
        let stringTemplate = OpilioCraft.StringTemplate.Runtime.Parse namePattern in
        new FilenameCreator(stringTemplate)

    static member Initialize () =
        FilenameCreator.Initialize "{id}"
