namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open System.Text.RegularExpressions

open OpilioCraft.ContentStore.Core
open OpilioCraft.FSharp.Prelude

[<AbstractClass>]
type public RepositoryCommandBase () as this =
    inherit ContentStoreCommand ()

    // used to identify a possible item id
    let itemIdRegex = Regex(@"^([0-9a-z]{64})$", RegexOptions.Compiled)
    let isValidItemId = itemIdRegex.IsMatch

    // repository settings
    [<DefaultValue>] val mutable private RepositoryInstance : Lazy<Repository>
    member _.ActiveRepository = this.RepositoryInstance.Value

    [<Parameter>]
    member val Repository = String.Empty with get,set

    // item identification
    [<Parameter(ParameterSetName="ByIdentifier", Position=0, Mandatory=true, ValueFromPipeline=true)>]
    member val Identifier = String.Empty with get, set

    [<Parameter(ParameterSetName="ByPath", Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    [<Parameter(ParameterSetName="ByItemId", Position=0, Mandatory=true)>]
    member val Id = String.Empty with get, set
    
    // fingerprint requirements
    member val ForceFullFingerprint = SwitchParameter(false) with get,set

    // check given input
    member _.IsValidItemId = isValidItemId

    member x.TryInputAsId () =
        match x.ParameterSetName with
        | "ByItemId" -> Some x.Id
        | "ByIdentifier" -> Some x.Identifier |> Option.filter isValidItemId
        | _ -> None

    member x.GotId = x.TryInputAsId >> Option.isSome

    member x.TryInputAsPath () =
        match x.ParameterSetName with
        | "ByIdentifier" -> Some x.Identifier |> Option.filter (not << isValidItemId)
        | "ByPath" -> Some x.Path
        | _ -> None
        |> Option.map x.ToAbsolutePath

    member x.GotPath = x.TryInputAsPath >> Option.isSome

    // map input to item id
    member x.IdFromPath absolutePath =
        absolutePath
        |> Fingerprint.getFingerprint 
        |> function
            | Derived fingerprint when x.ForceFullFingerprint.ToBool() = false -> fingerprint
            | Full fingerprint -> fingerprint
            | _ -> Fingerprint.fingerprintAsString absolutePath
        
    member x.RetrieveItemId () =
        x.TryInputAsId() |> Option.orElse ( x.TryInputAsPath () |> Option.map x.IdFromPath )

    member x.AssertIdProvided () =
        x.TryInputAsId() |> Option.orElseWith (fun _ -> failwith $"expected an id as first parameter") |> ignore
        
    member x.AssertPathProvided () =
        x.TryInputAsPath() |> Option.orElseWith (fun _ -> failwith $"expected a path as first parameter") |> ignore
        
    member x.AssertIsManagedItem maybeItem =
        maybeItem
        |> Option.ifNone (fun _ -> failwith $"cannot derive item id from given input")
        |> Option.filterOrElseWith
            x.ActiveRepository.IsManagedId
            (fun itemId -> failwith $"provided item with id {itemId} is unknown to specified repository")

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
