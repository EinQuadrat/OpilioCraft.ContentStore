namespace OpilioCraft.ContentStore.Core

open System.IO

open OpilioCraft.FSharp.Prelude
open OpilioCraft.Lisp

exception InvalidRuleDefinitionException of Path:string
    with override x.ToString () = $"rule defined in file {x.Path} is not valid"

exception UnexpectedRuleResultException of Name:string * Result:obj
    with override x.ToString () = $"rule {x.Name} returned an unexpected result type: {x.Result.GetType().FullName}"

type RulesProvider ( rulesLocation ) =
    let lispRuntime = LispRuntime.Initialize().InjectResultHook(ItemDetailHelper.unwrapItemDetail)

    let loadRule (ruleName : string) (pathToRuleDefinition : string) : Expression =
        if not <| File.Exists pathToRuleDefinition
        then
            raise <| new FileNotFoundException($"could not find definition file for rule {ruleName}", pathToRuleDefinition)

        lispRuntime.LoadFile pathToRuleDefinition
        |> lispRuntime.ParseWithResult
        |> function | Ok compiledRule -> compiledRule | _ -> raise <| InvalidRuleDefinitionException pathToRuleDefinition

    let rules : Map<string, Expression> =
        if Directory.Exists rulesLocation
        then
            Directory.EnumerateFiles(rulesLocation, "*.lisp")
            |> Seq.map (fun file -> Path.GetFileNameWithoutExtension(file), file)
            |> Seq.map (fun (name, path) -> name, loadRule name path)
            |> Map.ofSeq
        else
            Map.empty

    // constructors
    new () =
        // initialize from default rules location
        RulesProvider ( Settings.RulesLocation )

    // rule access
    member _.TryGetRule name =
        rules |> Map.tryFind name

    member x.TryApplyRule name data =
        x.TryGetRule name
        |> Option.bind (
            fun rule ->
                lispRuntime.InjectObjectData(data).EvalWithResult rule
                |> function | Ok (Atom fval) -> fval.ToString() |> Some | _ -> None
            )
