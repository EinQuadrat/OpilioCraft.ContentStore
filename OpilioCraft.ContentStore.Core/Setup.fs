module opilio.solutions.MMToolkit.Setup

open System.IO

let setupPending : bool =
    Settings.AppDataLocation |> Directory.Exists |> not
    
let runSetup (force : bool) =
    if (not <| setupPending) || force then
        if not <| Directory.Exists(Settings.AppDataLocation) then Directory.CreateDirectory(Settings.AppDataLocation) |> ignore
        ConfigHelper.saveConfig ConfigHelper.Configuration.Default
