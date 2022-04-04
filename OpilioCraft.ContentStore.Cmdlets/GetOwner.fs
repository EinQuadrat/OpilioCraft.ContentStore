namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation

open OpilioCraft.FSharp.Prelude
open OpilioCraft.FSharp.Heuristic


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

    [<Parameter(Mandatory=true)>]
    member val Model = String.Empty with get, set

    [<Parameter>]
    member val DefaultOwner = "N/A" with get, set

    member val Heuristic : Heuristic option = None with get, set

    // cmdlet funtionality
    override x.BeginProcessing() =
        base.BeginProcessing()

        try
            x.Heuristic <- Heuristic x.Model |> Some
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified

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
            |> Option.bind x.Heuristic.Value.Apply
            |> Option.map ( fun flexVal -> flexVal.ToString() )
            |> Option.defaultValue x.DefaultOwner

            |> x.WriteObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
