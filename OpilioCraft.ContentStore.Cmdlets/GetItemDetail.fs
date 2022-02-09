namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "ItemDetail", DefaultParameterSetName = "ByPath")>]
[<OutputType(typeof<obj>)>]
type public GetItemDetailCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet params
    [<Parameter(ParameterSetName="ByPath", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    [<Parameter(ParameterSetName="ById", Position=0, Mandatory=true)>]
    member val Id = String.Empty with get, set

    [<Parameter(Position=1, Mandatory=true)>]
    member val Name = String.Empty with get, set

    [<Parameter(Position=2)>]
    member val DefaultValue = String.Empty with get, set

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

            |> Option.map x.ActiveRepository.FetchItem
            |> Option.iter
                ( fun item ->
                    if item.Details.ContainsKey(x.Name)
                    then
                        item.Details.Item(x.Name) |> x.WriteObject
                    else
                        x.DefaultValue |> x.WriteObject
                )
        with
        | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
