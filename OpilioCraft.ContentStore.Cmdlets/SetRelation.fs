namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Set, "Relation", DefaultParameterSetName="ByPath")>]
[<OutputType(typeof<Void>)>]
type public SetRelationCommand () =
    inherit RepositoryCommandBase ()

    [<Parameter(ParameterSetName="ByPath", Position=0, Mandatory=true)>]
    member val Path = String.Empty with get, set

    [<Parameter(ParameterSetName="ByPath", Position=1, Mandatory=true)>]
    member val TargetPath = String.Empty with get, set

    [<Parameter(ParameterSetName="ById", Position=0, Mandatory=true)>]
    member val Id = String.Empty with get, set

    [<Parameter(ParameterSetName="ById", Position=1, Mandatory=true)>]
    member val TargetId = String.Empty with get, set

    [<Parameter(Position=2)>]
    member val RelationType = RelationType.Related.ToString() with get, set
    
    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            let fromId =
                if x.Id |> String.IsNullOrEmpty // check for default parameter set
                then
                    x.Path
                    |> x.ToAbsolutePath
                    |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
                    |> Option.map Fingerprint.fingerprintAsString
                else
                    x.Id
                    |> Some
                |> x.Assert x.ActiveRepository.IsManagedId $"given origin id is unknown: {x.Id}"

            let toId =
                if x.TargetId |> String.IsNullOrEmpty // check for default parameter set
                then
                    x.TargetPath
                    |> x.ToAbsolutePath
                    |> x.TryFileExists $"given file does not exist or is not accessible: {x.TargetPath}"
                    |> Option.map Fingerprint.fingerprintAsString
                else
                    x.TargetId
                    |> Some
                |> x.Assert x.ActiveRepository.IsManagedId $"given target id is unknown: {x.TargetId}"

            let relType =
                x.RelationType
                |> Utils.tryParseRelationType
                |> x.WarningIfNone $"invalid relation type: {x.RelationType}"

            if fromId.IsSome && toId.IsSome && relType.IsSome
            then
                x.ActiveRepository.AddRelation fromId.Value { Target = toId.Value; IsA = relType.Value }
        with
        | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
