namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Set, "Relation", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<Void>)>]
type public SetRelationCommand () =
    inherit RepositoryCommandExtended ()

    [<Parameter(Mandatory=true)>]
    member val Target = String.Empty with get, set

    [<Parameter>]
    member val RelationType = RelationType.Related.ToString() with get, set
    
    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            let fromId =
                x.RetrieveItemId()
                |> x.AssertIsManagedItem
                |> Option.get

            let toId =
                if x.Target |> x.IsValidItemId
                then
                    x.Target
                else
                    x.Target
                    |> x.ToAbsolutePath
                    |> x.AssertFileExists $"given file does not exist or is not accessible: {x.Target}"

                    |> fun absolutePath ->
                            absolutePath
                            |> Fingerprint.getFingerprint 
                            |> function
                                | Derived fingerprint when x.ForceFullFingerprint.ToBool() = false -> fingerprint
                                | Full fingerprint -> fingerprint
                                | _ -> Fingerprint.fingerprintAsString absolutePath

                    |> Some
                    |> x.AssertIsManagedItem
                    |> Option.get

            let relType =
                x.RelationType
                |> Utils.tryParseRelationType
                |> Option.ifNone (fun _ -> failwith $"invalid relation type: {x.RelationType}")
                |> Option.get

            x.ActiveRepository.AddRelation fromId { Target = toId; IsA = relType }
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
