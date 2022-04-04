namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open System.Text.RegularExpressions
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "ItemDetails", DefaultParameterSetName = "ByPath")>]
[<OutputType(typeof<ItemDetails>)>]
type public GetItemDetailsCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet params
    [<Parameter(ParameterSetName="ByPath", Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    [<Parameter(ParameterSetName="ById", Position=0, Mandatory=true)>]
    member val Id = String.Empty with get, set

    [<Parameter(Position=1)>]
    member val Select = ".*" with get, set

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            if x.Id |> String.IsNullOrEmpty // parameterset ByPath?
            then
                x.Path
                |> x.ToAbsolutePath
                |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
                |> Option.map Fingerprint.fingerprintAsString
            else
                x.Id
                |> Some

            |> x.Assert x.ActiveRepository.IsManagedId $"given id is unknown: {x.Id}"

            |> Option.map x.ActiveRepository.GetDetails
            |> Option.iter
                ( fun itemDetails ->
                    let result = new ItemDetails()
                    let regex = Regex(x.Select, RegexOptions.Compiled)

                    itemDetails.Keys
                    |> Seq.filter regex.IsMatch
                    |> Seq.iter ( fun key -> result.Add(key, itemDetails.[key]) )

                    result |> x.WriteObject
                )
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
