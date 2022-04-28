namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation

open OpilioCraft.ContentStore.Core
open OpilioCraft.FSharp.Prelude
open OpilioCraft.Lisp

[<Cmdlet(VerbsLifecycle.Invoke, "ItemRule", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<obj>)>]
type public InvokeItemRuleCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet params
    [<Parameter>]
    member val RuleName = String.Empty with get, set // load predefined rule

    [<Parameter>]
    member val Rule = String.Empty with get, set // LISP expression

    // private params
    member val private LispRuntime = LispRuntime.Initialize().InjectResultHook(ItemDetailHelper.unwrapItemDetail)
    member val private CompiledRule = LispFalse with get,set

    // cmdlet funtionality
    override x.BeginProcessing () =
        base.BeginProcessing () // initialize MMToolkit

        try
            // initialize rule
            if x.RuleName |> String.IsNullOrEmpty
            then
                x.LispRuntime.TryParse x.Rule
                |> Option.defaultWith (fun _ -> failwith "invalid rule")
            else
                x.ContentStoreManager.RulesProvider.TryGetRule x.RuleName
                |> Option.defaultWith (fun _ -> failwith "unknown rule")

            |> (fun expr -> x.CompiledRule <- expr)

        with
            | exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.ResourceUnavailable

    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.TryDetermineItemId ()
            |> x.AssertIsManagedItem "Invoke-ItemRule"

            |> Option.map x.ActiveRepository.GetItem

            |> Option.bind (fun item -> x.LispRuntime.InjectObjectData(item).EvalWithResult x.CompiledRule |> Result.toOption)
            |> Option.bind (function | Atom fval -> Some fval | _ -> None)
            |> Option.iter x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
