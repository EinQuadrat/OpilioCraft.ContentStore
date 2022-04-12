module OpilioCraft.ContentStore.Core.Utils

open System.IO
open FSharp.Data
open OpilioCraft.FSharp.Prelude
open OpilioCraft.FSharp.Prelude.ActivePatterns

// public api
let getContentCategory (fi : FileInfo) : ContentCategory =
    match fi.Extension.ToLower() with
    | Match("^(\.(arw|jpe?g|tiff?|gif))$") _ -> ContentCategory.Image
    | Match("^(\.(mov|mp4|mts))$") _ -> ContentCategory.Movie
    | _ -> ContentCategory.Unknown

let getContentType (fi : FileInfo) : ContentType =
    {
        Category = fi |> getContentCategory
        FileExtension = fi.Extension.ToLower()
    }

let identifyFile (fi : FileInfo) =
    {
        FileInfo = fi
        AsOf = fi.LastWriteTimeUtc
        Fingerprint = fi.FullName |> Fingerprint.fingerprintAsString
    }

let tryParseContentCategory (input : string) : ContentCategory option =
    match System.Enum.TryParse<ContentCategory>(input, true) with
    | true, value -> Some value
    | _ -> None

let tryParseRelationType (input : string) : RelationType option =
    match System.Enum.TryParse<RelationType>(input, true) with
    | true, value -> Some value
    | _ -> None

// exif related functions
let private transformExifToItemDetails (exif : ExifToolResult) =
    let transformJsonValue (nameAsHint : string) jsonValue : ItemDetail =
        match jsonValue with
        | JsonValue.Boolean x -> ItemDetail.Boolean x
        | JsonValue.Number x -> ItemDetail.Number x
        | JsonValue.Float x -> ItemDetail.Float x

        | JsonValue.String x -> // we use the nameAsHint to identify DateTime tags more reliable
            match x with
            | IsDateTime x when nameAsHint.Contains("Date") -> ItemDetail.DateTime x
            | _ -> ItemDetail.String x
    
        // container items are flattened to a string
        | JsonValue.Array x ->
            seq { for item in jsonValue -> item.AsString() }
            |> String.concat ", "
            |> fun flattenedArray -> ItemDetail.String $"#ARRAY# [ {flattenedArray} ]"
        
        | JsonValue.Record x ->
            seq { for (prop, item) in jsonValue.Properties() -> $"{prop} = {item.AsString()}" }
            |> String.concat ", "
            |> fun flattenedRecord -> ItemDetail.String $"#RECORD# {{ {flattenedRecord} }}"
    
        // error handling
        | _ -> failwith $"[ContentStore.Core] unexpected JsonValue of type \"{jsonValue.GetType().Name}\""

    exif.ParsedJson.Properties()
    |> Array.fold ( fun map (name, value) -> (transformJsonValue name value, map) ||> Map.add name ) Map.empty
    
let getCategorySpecificDetails (fi : FileInfo) (contentCategory : ContentCategory) (heuristic : Heuristic) : ItemDetails =
    let result = new ItemDetails()

    match contentCategory with
    | ContentCategory.Image ->
        match fi |> ExifToolHelper.getMetadata with
        | Some exif ->
            result.Add(Slot.ExifTool, true |> ItemDetail.Boolean) // indicate that we have EXIF data
            for item in exif |> transformExifToItemDetails do result.Add($"{SlotPrefix.ExifTool}{item.Key}", item.Value)

            exif
            |> ExifToolHelper.tryExtractCamera
            |> Option.map ItemDetail.String
            |> Option.orElse (result |> heuristic "camera")
            |> Option.iter ( fun camera -> result.Add(Slot.Camera, camera) )

            exif.["EXIF:DateTimeOriginal"]
            |> Option.orElse exif.["File:FileModifyDate"]
            |> Option.bind ExifToolHelper.tryAsDateTime
            |> Option.iter ( fun dateTaken -> result.Add(Slot.DateTaken, dateTaken |> ItemDetail.DateTime) )

        | _ -> ignore ()

    | ContentCategory.Movie ->
        match fi |> ExifToolHelper.getMetadata with
        | Some exif ->
            result.Add(Slot.ExifTool, true |> ItemDetail.Boolean) // indicate that we have EXIF data
            for item in exif |> transformExifToItemDetails do result.Add($"{SlotPrefix.ExifTool}{item.Key}", item.Value)

            exif
            |> ExifToolHelper.tryExtractCamera
            |> Option.map ItemDetail.String
            |> Option.orElse (result |> heuristic "camera")
            |> Option.orElse (ItemDetail.String "NA" |> Some)
            |> Option.iter ( fun camera -> result.Add(Slot.Camera, camera) )

            exif.["EXIF:DateTimeOriginal"]
            |> Option.orElse exif.["H264:DateTimeOriginal"]
            |> Option.orElse exif.["QuickTime:TrackCreateDate"]
            |> Option.orElse exif.["File:FileModifyDate"]
            |> Option.bind ExifToolHelper.tryAsDateTime
            |> Option.iter ( fun dateTime -> result.Add(Slot.DateTaken, dateTime |> ItemDetail.DateTime) )

        | _ -> ignore ()

    | _ -> ignore ()

    result
