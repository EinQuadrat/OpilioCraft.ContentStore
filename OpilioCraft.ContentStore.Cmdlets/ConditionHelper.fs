module internal OpilioCraft.ContentStore.Cmdlets.ConditionHelper 

open System

open OpilioCraft.FSharp.Prelude
open OpilioCraft.FSharp.OQL
open OpilioCraft.FSharp.YaLisp
open OpilioCraft.ContentStore.Core

// lisp function to integrate OQL
let applyObjectQuery (oqlRuntime : IOqlRuntime) (_ : IYaLispRuntime) (exprList : YaLispExpression list) : YaLispExpression =
    match exprList with
    | [ YaLispAtom (FlexibleValue.String oql) ] ->
        oqlRuntime.TryRun oql
        |> Option.map (function | :? ItemDetail as itemDetail -> itemDetail.Unwrap | otherwise -> otherwise)
        |> Option.map (FlexibleValue.Wrap >> YaLispAtom)
        |> Option.defaultValue (YaLispSymbol "#UNKNOWN-PROPERTY") // force no-match

    | [ (YaLispAtom (FlexibleValue.String oql)) ; YaLispAtom defaultValue ] ->
        oqlRuntime.RunWithDefault oql (defaultValue :> obj)
        |> (function | :? ItemDetail as itemDetail -> itemDetail.Unwrap | otherwise -> otherwise)
        |> FlexibleValue.Wrap |> YaLispAtom

    | _ -> raise <| new InvalidOperationException()

// LISP macros
let macroPropertyIs (exprList : YaLispExpression list) : YaLispExpression =
    match exprList with
    | [ YaLispAtom (FlexibleValue.String oql); YaLispAtom pattern ] ->
        let getProperty = YaLispList [ YaLispSymbol "property" ; YaLispAtom (FlexibleValue.String oql) ] in
        YaLispList [ YaLispSymbol "eq" ; getProperty ; YaLispAtom pattern ]
    | _ -> raise <| InvalidYaLispExpressionException "property-is expects an oql string and an atom as arguments"

let macroPropertyIsNot (exprList : YaLispExpression list) : YaLispExpression =
    match exprList with
    | [ YaLispAtom (FlexibleValue.String oql); YaLispAtom pattern ] ->
        let getProperty = YaLispList [ YaLispSymbol "property" ; YaLispAtom (FlexibleValue.String oql) ] in
        YaLispList [ YaLispSymbol "not" ; YaLispList [ YaLispSymbol "eq" ; getProperty ; YaLispAtom pattern ] ]
    | _ -> raise <| InvalidYaLispExpressionException "property-is expects an oql string and an atom as arguments"

// runtimes
let createRuntimes () =
    let oqlRuntime = new OpilioCraft.FSharp.OQL.DefaultOqlRuntime () :> OpilioCraft.FSharp.OQL.IOqlRuntime

    let lispRuntime : IYaLispRuntime = new DefaultYaLispRuntime (OpilioCraft.FSharp.YaLisp.StandardLib.init)
    lispRuntime.RegisterFunction "property" (applyObjectQuery oqlRuntime)
    lispRuntime.RegisterMacro "property-is" macroPropertyIs
    lispRuntime.RegisterMacro "property-is-not" macroPropertyIs

    oqlRuntime, lispRuntime

// conditions
let applyCondition (oqlRuntime : IOqlRuntime) (lispRuntime : IYaLispRuntime) compiledCondition item =
    oqlRuntime.ObjectData <- item

    lispRuntime.Eval compiledCondition
    |> function
        | YBoolean result -> result
        | _ -> invalidArg "condition" "condition has to result into a boolean value"
