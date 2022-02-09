module OpilioCraft.ContentStore.Core.Utils

open System.IO
open FSharp.Data
open OpilioCraft.FSharp.Prelude.ActivePatterns

// public api
let getContentType (fi : FileInfo) : ContentType =
    match fi.Extension.ToLower() with
    | Match("^(\.(arw|jpe?g|tiff?|gif))$") m -> { Category = ContentCategory.Image; FileExtension = m.Groups.[1].Value }
    | Match("^(\.(mov|mp4|mts))$") m -> { Category = ContentCategory.Movie; FileExtension = m.Groups.[1].Value }
    | ext -> { Category = ContentCategory.Unknown; FileExtension = ext }

let identifyFile (fi : FileInfo) =
    {
        FileInfo = fi
        AsOf = fi.LastWriteTimeUtc
        ContentType = fi |> getContentType
        Fingerprint = fi.FullName |> Fingerprint.fingerprintAsString
    }

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
    
let private getCategorySpecificData (fi : FileInfo) (category : ContentCategory) : ItemDetails =
    let result = new ItemDetails()

    match category with
    | ContentCategory.Image ->
        match fi |> ExifToolHelper.getMetadata with
        | Some exif ->
            result.Add(Slot.ExifTool, true |> ItemDetail.Boolean) // indicate that we have EXIF data
            for item in exif |> transformExifToItemDetails do result.Add($"{SlotPrefix.ExifTool}{item.Key}", item.Value)
            result.Add(Slot.Camera, exif |> ExifToolHelper.extractCamera |> ItemDetail.String )

            exif.["EXIF:DateTimeOriginal"]
                |> Option.orElse exif.["File:FileModifyDate"]
                |> Option.bind ExifToolHelper.tryAsDateTime
                |> Option.iter ( fun dateTime -> result.Add(Slot.DateTaken, dateTime |> ItemDetail.DateTime) )
        | _ -> ignore ()

    | ContentCategory.Movie ->
        match fi |> ExifToolHelper.getMetadata with
        | Some exif ->
            result.Add(Slot.ExifTool, true |> ItemDetail.Boolean) // indicate that we have EXIF data
            for item in exif |> transformExifToItemDetails do result.Add($"{SlotPrefix.ExifTool}{item.Key}", item.Value)
            result.Add(Slot.Camera, exif |> ExifToolHelper.extractCamera |> ItemDetail.String )

            exif.["EXIF:DateTimeOriginal"]
                |> Option.orElse exif.["H264:DateTimeOriginal"]
                |> Option.orElse exif.["QuickTime:TrackCreateDate"]
                |> Option.orElse exif.["File:FileModifyDate"]
                |> Option.bind ExifToolHelper.tryAsDateTime
                |> Option.iter ( fun dateTime -> result.Add(Slot.DateTaken, dateTime |> ItemDetail.DateTime) )
        | _ -> ignore ()

    | _ -> ignore ()

    result

let getExtendedData (fident : FileIdentificator) = getCategorySpecificData fident.FileInfo fident.ContentType.Category
let getExtendedDataByFileInfo (fi : FileInfo) = getCategorySpecificData fi (getContentType fi).Category
