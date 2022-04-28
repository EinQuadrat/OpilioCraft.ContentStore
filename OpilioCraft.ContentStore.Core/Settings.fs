namespace OpilioCraft.ContentStore.Core

open System
type Path = IO.Path

[<RequireQualifiedAccess>]
module internal Settings =
    let FrameworkVersion = Version(2, 1)

    // location of runtime; e.g. for side-by-side apps
    let AssemblyLocation = Uri(Reflection.Assembly.GetExecutingAssembly().Location).LocalPath
    let RuntimeBase = Path.GetDirectoryName(AssemblyLocation)

    // location of app specific data
    let AppDataLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ContentStoreFramework")
    let RulesLocation = Path.Combine(AppDataLocation, "rules")

    // configuration files
    let FrameworkConfigFile = Path.Combine(AppDataLocation, "config.json")

// ------------------------------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module SlotPrefix =
     /// Prefix of all entries returned by ExifTool
    [<Literal>]
    let ExifTool = "ExifTool:"

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
