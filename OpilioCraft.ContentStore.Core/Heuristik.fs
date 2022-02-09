module OpilioCraft.ContentStore.Core.Heuristik

open FSharp.Data

let private loadRuleset (filename : string) =
    try
        JsonValue.Load(filename)
    with
    | exn -> System.Console.Error.WriteLine $"[ContentStore.Core] cannot load heuristic data: {exn.Message}"; JsonValue.Null

let OwnerRuleSet =
    loadRuleset Settings.OwnerRuleSetFilename
