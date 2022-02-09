namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<AbstractClass>]
type public RepositoryCommandBase () as this =
    inherit CommandBase ()

    [<DefaultValue>] val mutable private RepositoryInstance : Lazy<Repository>
    member _.ActiveRepository = this.RepositoryInstance.Value

    [<Parameter>]
    member val Repository = String.Empty with get,set

    override x.BeginProcessing () =
        base.BeginProcessing ()

        try
            if String.IsNullOrEmpty(x.Repository)
            then
                x.RepositoryInstance <- lazy ( Repository.LoadDefaultRepository() )
            else
                x.RepositoryInstance <- lazy ( Repository.LoadRepository(x.Repository) )

            x.RepositoryInstance.Force () |> ignore
        with
        | exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.ResourceUnavailable 
