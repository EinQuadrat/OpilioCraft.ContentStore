namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation

open OpilioCraft.ContentStore.Core
open OpilioCraft.FSharp.Prelude
open OpilioCraft.Lisp

[<Cmdlet(VerbsCommon.Select, "Items")>]
[<OutputType(typeof<ItemId>)>]
type public SelectItemsCommand () =
    inherit RepositoryCommandBase ()

    // helpers
    let applyFilter (runtime : LispRuntime) (filter : Expression) item =
        runtime.EvalWithContextAndResult item filter
        |> function
            | Ok (Atom (FlexibleValue.Boolean true)) -> true
            | _ -> false

    // cmdlet params
    [<Parameter(Position=0)>]
    member val Filter = "T" with get, set // Lisp-like condition

    member val private LispRuntime = LispRuntime.Initialize().InjectResultHook(ItemDetailHelper.unwrapItemDetail)

    // cmdlet funtionality
    override x.EndProcessing() =
        base.EndProcessing()

        try
            let compiledFilter =
                x.LispRuntime.TryParse x.Filter
                |> Option.defaultWith (fun _ -> failwith "invalid filter")

            x.ActiveRepository.GetItemIds ()
            |> Seq.map x.ActiveRepository.GetItem
            |> Seq.filter (applyFilter x.LispRuntime compiledFilter)
            |> Seq.map (fun item -> item.Id)
            |> Seq.iter x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
