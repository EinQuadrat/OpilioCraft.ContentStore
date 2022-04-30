namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<AbstractClass>]
type public RepositoryCommandBase () as this =
    inherit ContentStoreCommand ()

    // repository settings
    [<DefaultValue>] val mutable private RepositoryInstance : Lazy<Repository>
    member _.ActiveRepository = this.RepositoryInstance.Value

    [<Parameter>]
    member val Repository = String.Empty with get,set

    // functionality
    override x.BeginProcessing () =
        base.BeginProcessing ()

        try
            x.RepositoryInstance <- 
                if String.IsNullOrEmpty(x.Repository)
                then
                    lazy ( x.ContentStoreManager.GetDefaultRepository() )
                else
                    lazy ( x.ContentStoreManager.GetRepository(x.Repository) )

            x.RepositoryInstance.Force () |> ignore
        with
            | exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.ResourceUnavailable 
