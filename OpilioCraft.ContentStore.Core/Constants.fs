namespace OpilioCraft.ContentStore.Core

[<RequireQualifiedAccess>]
module Slot =
    /// Camera maker and model combined into one field
    [<Literal>]
    let Camera = "Camera"
    
    /// DateTime when the item was created
    [<Literal>]
    let DateTaken = "DateTaken"

     /// Date taken without any corrections as stored in metadata
    [<Literal>]
    let DateTakenOriginal = "DateTaken:Original"
    /// Offset as TimeSpan to be added to DateTakenOriginal
    [<Literal>]
    let DateTakenOffset = "DateTaken:Offset"

     /// Used to show whether we have data from ExifTool at hand
    [<Literal>]
    let ExifTool = "ExifTool"

    /// Owner of the content
    [<Literal>]
    let Owner = "Owner"

[<RequireQualifiedAccess>]
module SlotPrefix =
     /// Prefix of all entries returned by ExifTool
    [<Literal>]
    let ExifTool = "ExifTool:"

[<RequireQualifiedAccess>]
module Defaults =
    [<Literal>]
    let UnknownCamera = "NA"

    [<Literal>]
    let UnknownOwner = "NA"
