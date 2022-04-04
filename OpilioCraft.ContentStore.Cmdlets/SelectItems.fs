namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Select, "Items")>]
[<OutputType(typeof<ItemId>)>]
type public SelectItemsCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet params
    [<Parameter(Position=0)>]
    member val Filter = "(T)" with get, set // Lisp-like condition

    // cmdlet funtionality
    override x.EndProcessing() =
        base.EndProcessing()

        try
            let oqlRuntime, lispRuntime = ConditionHelper.createRuntimes ()
            let compiledCondition = lispRuntime.Parse x.Filter

            x.ActiveRepository.GetItemIds ()
            |> Seq.map x.ActiveRepository.GetItemWithoutCaching
            |> Seq.filter (fun item -> ConditionHelper.applyCondition oqlRuntime lispRuntime compiledCondition item)
            |> Seq.map (fun item -> item.Id)
            |> Seq.iter x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
