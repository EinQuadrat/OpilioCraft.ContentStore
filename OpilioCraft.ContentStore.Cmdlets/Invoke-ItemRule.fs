namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation

open OpilioCraft.ContentStore.Core
open OpilioCraft.FSharp.Prelude
open OpilioCraft.Lisp

[<Cmdlet(VerbsLifecycle.Invoke, "ItemRule")>]
[<OutputType(typeof<obj>)>]
type public InvokeItemRuleCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet params
    [<Parameter(ParameterSetName="ByPath_Predefined", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Parameter(ParameterSetName="ByPath_UserDefined", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    [<Parameter(ParameterSetName="ById_Predefined", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Parameter(ParameterSetName="ById_UserDefined", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    member val Id = String.Empty with get, set

    [<Parameter(ParameterSetName="ByPath_Predefined", Mandatory=true)>]
    [<Parameter(ParameterSetName="ById_Predefined", Mandatory=true)>]
    member val RuleName = String.Empty with get, set // load predefined rule

    [<Parameter(ParameterSetName="ByPath_UserDefined", Mandatory=true)>]
    [<Parameter(ParameterSetName="ById_UserDefined", Mandatory=true)>]
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
            if x.Id |> String.IsNullOrEmpty // parameter set ByPath?
            then
                x.Path
                |> x.ToAbsolutePath
                |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
                |> Option.map Fingerprint.fingerprintAsString
            else
                x.Id
                |> Some

            |> x.Assert x.ActiveRepository.IsManagedId $"no managed item with id {x.Id}"

            |> Option.map x.ActiveRepository.GetItem
            |> Option.bind (fun item -> x.LispRuntime.InjectObjectData(item).EvalWithResult x.CompiledRule |> Result.toOption)
            |> Option.bind (function | Atom fval -> Some fval | _ -> None)
            |> Option.iter x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
