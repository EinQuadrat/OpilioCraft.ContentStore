namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Management.Automation

[<Cmdlet(VerbsCommon.Get, "ItemFile")>]
[<OutputType(typeof<Void>)>]
type public GetItemFileCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet params
    [<Parameter(Position=0, Mandatory=true)>]
    member val Id = String.Empty with get, set

    [<Parameter(Position=1, Mandatory=true)>]
    member val TargetPath = String.Empty with get, set

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.Id
            |> Some
            |> x.Assert x.ActiveRepository.IsManagedId $"given id is unknown: {x.Id}"
            |> Option.iter ( fun id -> x.ActiveRepository.CloneFile id x.TargetPath )
        with
        | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
