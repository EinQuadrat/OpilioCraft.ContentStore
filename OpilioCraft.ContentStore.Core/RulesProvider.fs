namespace OpilioCraft.ContentStore.Core

open System.IO

open OpilioCraft.FSharp.Prelude
open OpilioCraft.Lisp

exception InvalidRuleDefinitionException of Path:string
    with override x.ToString () = $"rule defined in file {x.Path} is not valid"

exception UnexpectedRuleResultException of Name:string * Result:obj
    with override x.ToString () = $"rule {x.Name} returned an unexpected result type: {x.Result.GetType().FullName}"

type RulesProvider ( frameworkConfig : FrameworkConfig ) =
    let lispRuntime = LispRuntime.Initialize().InjectResultHook(ItemDetailHelper.unwrapItemDetail)

    let loadRuleDefinition (ruleName : string) (pathToRuleDefinition : string) : Expression =
        let path = if Path.IsPathRooted(pathToRuleDefinition) then pathToRuleDefinition else Path.Combine(Settings.AppDataLocation, pathToRuleDefinition)

        if not <| File.Exists path
        then
            raise <| new FileNotFoundException($"could not find rule definition file for rule {ruleName}", path)

        lispRuntime.LoadFile path
        |> lispRuntime.ParseWithResult
        |> function | Ok compiledRule -> compiledRule | _ -> raise <| InvalidRuleDefinitionException path

    let rules : Map<string, Expression> =
        if Directory.Exists(Settings.RulesLocation)
        then
            Directory.EnumerateFiles(Settings.RulesLocation, "*.lisp")
            |> Seq.map (fun file -> Path.GetFileNameWithoutExtension(file), file)
            |> Seq.map (fun (name, path) -> name, loadRuleDefinition name path)
            |> Map.ofSeq
        else
            Map.empty

    // constructors
    new () =
        // initialize with empty rules table
        RulesProvider { Version = Settings.FrameworkVersion; Repositories = Map.empty; Rules = Map.empty }

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
