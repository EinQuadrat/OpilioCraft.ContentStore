namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.IO
open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "ItemStatus", DefaultParameterSetName="ByPath")>]
[<OutputType(typeof<StatusByPath>, typeof<StatusById>)>]
type public GetItemStatusCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet params
    [<Parameter(ParameterSetName="ByPath", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    [<Parameter(ParameterSetName="ById", Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true)>]
    member val Id = String.Empty with get, set

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            if x.Id |> String.IsNullOrEmpty // check for default parameter set
            then
                x.Path
                |> x.ToAbsolutePath
                |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
                |> Option.map
                    ( fun path ->
                        let fident = path |> FileInfo |> Utils.identifyFile in
                        {
                            Path = fident.FileInfo.FullName
                            AsOf = fident.AsOf
                            Id = fident.Fingerprint
                            IsManaged = fident.Fingerprint |> x.ActiveRepository.IsManagedId
                        }
                    )
                |> Option.iter x.WriteObject
            else
                {
                    Id = x.Id
                    IsManaged = x.Id |> x.ActiveRepository.IsManagedId
                }
                |> x.WriteObject
        with
        | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified

and StatusByPath =
    {
        Path : string
        AsOf : System.DateTime // as UTC timestamp
        Id : string
        IsManaged : bool
    }

and StatusById =
    {
        Id : string
        IsManaged : bool
    }
