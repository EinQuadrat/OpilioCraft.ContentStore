namespace OpilioCraft.ContentStore.Cmdlets

open System.IO
open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "ItemStatus", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<StatusByPath>, typeof<StatusById>)>]
type public GetItemStatusCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            if x.IdentifierLooksLikeAnItemId
            then
                {
                    Id = x.Identifier
                    IsManaged = x.Identifier |> x.ActiveRepository.IsManagedId
                }
                |> x.WriteObject
            else
                x.TryIdentifierAsPath ()
                |> Option.map
                    ( fun path ->
                        let fident = path |> FileInfo |> Utils.identifyFile in
                        {
                            Path = fident.FileInfo.FullName
                            IsManaged = fident.Fingerprint |> x.ActiveRepository.IsManagedId
                        }
                    )
                |> Option.iter x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified

and StatusByPath =
    {
        Path : string
        IsManaged : bool
    }

and StatusById =
    {
        Id : string
        IsManaged : bool
    }
