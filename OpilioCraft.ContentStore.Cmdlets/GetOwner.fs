namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation

open OpilioCraft.FSharp.Prelude


[<Cmdlet(VerbsCommon.Get, "Owner", DefaultParameterSetName = "ByPath")>]
[<OutputType(typeof<string>)>]
type public GetOwnerCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet params
    [<Parameter(ParameterSetName="ByPath", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    [<Parameter(ParameterSetName="ById", Position=0, Mandatory=true)>]
    member val Id = String.Empty with get, set
    
    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            if x.Id |> String.IsNullOrEmpty // parameter set ByPath?
            then
                x.Path
                |> x.ToAbsolutePath
                |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
                |> Option.map Fingerprint.fingerprintAsString
            else
                x.Id
                |> Some

            |> x.Assert x.ActiveRepository.IsManagedId $"given id is unknown: {x.Id}"

            |> Option.map x.ActiveRepository.GetItem
            |> Option.bind (x.ContentStoreManager.RulesProvider.TryApplyRule "GuessOwner")
            |> Option.defaultValue "#UNKNOWN"

            |> x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
