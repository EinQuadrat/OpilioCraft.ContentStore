module internal OpilioCraft.ContentStore.Cmdlets.ConditionHelper 

open System

open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core
open OpilioCraft.Lisp

type ObjectPathRuntime = OpilioCraft.ObjectPath.IRuntime
type LispRuntime = OpilioCraft.Lisp.IRuntime

// lisp function to integrate OQL
let itemDetailHandler (incoming : obj) : obj =
    match incoming with
    | :? ItemDetail as itemDetail -> itemDetail.Unwrap
    | otherwise -> otherwise

let applyObjectQuery (runtime : ObjectPathRuntime) _ (exprList : Expression list) : Expression =
    match exprList with
    | [ Atom (FlexibleValue.String oql) ] ->
        runtime.TryRun oql
        |> Option.map itemDetailHandler
        |> Option.map (FlexibleValue.Wrap >> Atom)
        |> Option.defaultValue (Symbol "#UNKNOWN-PROPERTY") // force no-match

    | [ (Atom (FlexibleValue.String oql)) ; Atom defaultValue ] ->
        runtime.RunWithDefault oql (defaultValue :> obj)
        |> (function | :? ItemDetail as itemDetail -> itemDetail.Unwrap | otherwise -> otherwise)
        |> FlexibleValue.Wrap |> Atom

    | _ -> raise <| new InvalidOperationException()

// LISP macros
let macroPropertyIs (exprList : Expression list) : Expression =
    match exprList with
    | [ Atom (FlexibleValue.String _) ; Atom _ ] as [ objectPath ; pattern ]->
        List [ Symbol "eq" ; List [ Symbol "property" ; objectPath ] ; pattern ]
    | _ -> raise <| InvalidLispExpressionException "property-is expects an oql string and an atom as arguments"

let macroPropertyIsNot (exprList : Expression list) : Expression =
    match exprList with
    | [ Atom (FlexibleValue.String _); Atom _ ] as [ objectPath ; pattern ]->
        List [ Symbol "not" ; List [ Symbol "property-is" ; objectPath ; pattern ] ]
    | _ -> raise <| InvalidLispExpressionException "property-is expects an oql string and an atom as arguments"

// runtimes
let createRuntimes () =
    let opRuntime : ObjectPathRuntime = new OpilioCraft.ObjectPath.DefaultRuntime ()

    let lispRuntime : LispRuntime = new OpilioCraft.Lisp.DefaultRuntime (OpilioCraft.Lisp.StandardLib.init)
    lispRuntime.RegisterFunction "property" (applyObjectQuery opRuntime)
    lispRuntime.RegisterMacro "property-is" macroPropertyIs
    lispRuntime.RegisterMacro "property-is-not" macroPropertyIsNot

    opRuntime, lispRuntime

// conditions
let applyCondition (opRuntime : ObjectPathRuntime) (lispRuntime : LispRuntime) compiledCondition item =
    opRuntime.ObjectData <- item

    lispRuntime.Eval compiledCondition
    |> function
        | LispBoolean result -> result
        | _ -> invalidArg "condition" "condition has to result into a boolean value"
