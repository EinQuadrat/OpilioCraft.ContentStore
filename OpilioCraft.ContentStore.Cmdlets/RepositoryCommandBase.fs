namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.ContentStore.Core

type private RepositoryConnection =
    | NotInitialized
    | Active of Repository

[<AbstractClass>]
type public RepositoryCommandBase () =
    inherit ContentStoreCommand ()

    // repository status
    let mutable repositoryConnection = NotInitialized

    member _.ActiveRepository =
        match repositoryConnection with
        | NotInitialized -> failwith "[FATAL] connection to repository is not initialized yet"
        | Active repos -> repos

    // params
    [<Parameter>]
    member val SelectRepository = String.Empty with get,set

    // functionality
    override x.BeginProcessing () =
        base.BeginProcessing ()

        try
            repositoryConnection <- 
                if String.IsNullOrEmpty x.SelectRepository
                then
                    Active <| ContentStoreManager.getDefaultRepository ()
                else
                    Active <| ContentStoreManager.getRepository x.SelectRepository
        with
            | exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.ResourceUnavailable 
