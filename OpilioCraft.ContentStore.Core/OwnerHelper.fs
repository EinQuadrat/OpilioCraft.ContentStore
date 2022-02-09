module opilio.solutions.MMToolkit.OwnerHelper

open System.IO
open Newtonsoft.Json
open ConfigHelper

// json stuff
let jsonSerializer = JsonSerializer.Create(Settings.Json)

// low-level api
let existsOwnerRulesFile () =
    config().OwnerRulesFile |> File.Exists

let saveOwnerRules rules =
    use ownerRulesFile = config().OwnerRulesFile |> File.CreateText
    jsonSerializer.Serialize(ownerRulesFile, rules);

let initOwnerRulesFile overWrite =
    if (not <| existsOwnerRulesFile()) || overWrite then
        saveOwnerRules ""

let private loadOrInitOwnerRulesFile =
    lazy(
        // init owner rules on the fly if needed
        initOwnerRulesFile false

        // load owner rules files
        use rulesFile = config().OwnerRulesFile |> File.OpenText
        jsonSerializer.Deserialize(rulesFile, typeof<string>) :?> string
    )

let guessOwner (exif : Exif) : string option =
    None