namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Add, "Fingerprint")>]
[<OutputType(typeof<System.Void>, typeof<PSObject>)>]
type public AddFingerprintCommand () =
    inherit CommandBase ()

    [<Parameter(Position=0, Mandatory=true, ValueFromPipeline=true)>]
    member val InputObject : PSObject = null with get, set

    [<Parameter(Mandatory=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = System.String.Empty with get, set

    [<Parameter>]
    member val Force = SwitchParameter(false) with get, set

    [<Parameter>]
    member val PassThru = SwitchParameter(false) with get, set

    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            x.Path
            |> x.ToAbsolutePath
            |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
            |> Option.map ( fun path -> path |> if x.Force.IsPresent then Fingerprint.getFullFingerprint else Fingerprint.getFingerprint )
            |> Option.map ( fun fp -> x.InputObject.Members.Add(PSNoteProperty("Fingerprint", fp.Value)) )
            |> Option.iter ( fun _ -> if x.PassThru.IsPresent then x.InputObject |> x.WriteObject )
        with
        | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
