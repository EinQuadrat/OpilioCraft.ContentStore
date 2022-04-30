namespace OpilioCraft.ContentStore.Cmdlets

open System.IO
open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "ItemStatus", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<StatusByPath>, typeof<StatusById>)>]
type public GetItemStatusCommand () =
    inherit RepositoryCommandExtended ()

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.TryInputAsId()
            |> Option.map (
                fun itemId ->
                    {
                        Id = itemId
                        IsManaged = itemId |> x.ActiveRepository.IsManagedId
                    } |> x.WriteObject
                )
            |> Option.orElse (
                x.TryInputAsPath()
                |> Option.map (
                    fun path ->
                        let fident = path |> FileInfo |> Utils.identifyFile in
                        {
                            Path = fident.FileInfo.FullName
                            IsManaged = fident.Fingerprint |> x.ActiveRepository.IsManagedId
                        } |> x.WriteObject
                    )
                )
            |> ignore

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
