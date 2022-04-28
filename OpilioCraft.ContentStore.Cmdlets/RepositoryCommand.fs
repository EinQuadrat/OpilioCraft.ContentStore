namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open System.Text.RegularExpressions

open OpilioCraft.ContentStore.Core
open OpilioCraft.FSharp.Prelude

[<AbstractClass>]
type public RepositoryCommandBase () as this =
    inherit ContentStoreCommand ()

    let itemIdRegex = Regex(@"^([0-9a-z]{64})$", RegexOptions.Compiled)
    let looksLikeAnItemId input = itemIdRegex.IsMatch input

    let mutable itemId = String.Empty

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

    // helpers
    member _.LooksLikeAnItemId = looksLikeAnItemId

    member x.IdentifierLooksLikeAnItemId =
        match x.ParameterSetName with
        | "ByItemId" -> true
        | "ByIdentifier" -> looksLikeAnItemId x.Identifier
        | _ -> false
        
    member x.TryIdentifierAsPath () =
        match x.ParameterSetName with
        | "ByIdentifier" -> Some x.Identifier
        | "ByPath" -> Some x.Path
        | _ -> None
        |> Option.map x.ToAbsolutePath
        |> Option.filterOrElseWith IO.File.Exists (fun _ -> x.WriteWarning "given file does not exist or is not accessible")

    member x.TryDetermineItemId () : ItemId option =
        match x.ParameterSetName with
        | "ByItemId" -> Some x.Id
        | _ ->
            if x.IdentifierLooksLikeAnItemId
            then
                Some x.Identifier
            else
                x.TryIdentifierAsPath ()
                |> Option.map (
                    fun absolutePath ->
                        absolutePath
                        |> Fingerprint.getFingerprint 
                        |> function
                            | Derived fingerprint when x.ForceFullFingerprint.ToBool() = false -> fingerprint
                            | Full fingerprint -> fingerprint
                            | _ -> Fingerprint.fingerprintAsString absolutePath
                    )

    member x.AssertItemIdProvided cmdletName =
        match x.ParameterSetName with
        | "ByItemId" -> ()
        | "ByIdentifier" when x.IdentifierLooksLikeAnItemId -> ()
        | _ -> failwith $"{cmdletName}: expected an item id as first parameter"
        
    member x.AssertPathProvided cmdletName =
        match x.ParameterSetName with
        | "ByPath" -> ()
        | "ByIdentifier" when x.IdentifierLooksLikeAnItemId = false -> ()
        | _ -> failwith $"{cmdletName}: expected a path as first parameter"
        
    member x.AssertIsManagedItem cmdletName maybeItem =
        maybeItem
        |> Option.ifNone (fun _ -> failwith $"{cmdletName}: cannot derive item id from given input")
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
