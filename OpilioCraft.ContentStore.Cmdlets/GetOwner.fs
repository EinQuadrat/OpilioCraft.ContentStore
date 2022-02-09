namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "Owner")>]
type public GetOwnerCmdlet () =
    inherit CommandBase ()
       
    let mutable _rules = None

    [<Parameter>]
    member val InputObject : PSObject = PSObject.AsPSObject(System.String.Empty) with get, set

    override x.BeginProcessing () =
        base.BeginProcessing () // initialize MMToolkit
        
        try
            _rules <- Some <| Heuristik.OwnerRuleSet
        with
        | :? RuleSetError as exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.InvalidData
        | exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.ResourceUnavailable

    override x.ProcessRecord () =
        base.ProcessRecord ()

    override x.EndProcessing () =
        base.EndProcessing ()
