namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.FSharp.Prelude

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
            |> x.AssertFileExists $"given file does not exist or is not accessible: {x.Path}"
            |> fun path -> path |> if x.Force.IsPresent then Fingerprint.getFullFingerprint else Fingerprint.getFingerprint
            |> fun fp -> x.InputObject.Members.Add(PSNoteProperty("Fingerprint", fp.Value))
            
            if x.PassThru.IsPresent
            then
                 x.WriteObject x.InputObject
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
