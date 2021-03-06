namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.FSharp.Prelude

[<Cmdlet(VerbsCommon.Get, "Fingerprint")>]
[<OutputType(typeof<TypedFingerprint>, typeof<PlainFingerprint>)>]
type public GetFingerprintCommand () =
    inherit CommandBase ()

    [<Parameter(Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = System.String.Empty with get, set

    [<Parameter>]
    member val ForceFullFingerprint : SwitchParameter = SwitchParameter(false) with get, set

    [<Parameter>]
    member val Plain : SwitchParameter = SwitchParameter(false) with get, set

    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            x.Path
            |> x.ToAbsolutePath
            |> x.AssertFileExists $"given file does not exist or is not accessible: {x.Path}"

            |> fun path ->
                let fpStrategy =
                    if x.ForceFullFingerprint.IsPresent
                    then
                        Fingerprint.getFullFingerprint
                    else
                        Fingerprint.getFingerprint

                path, ( fpStrategy path )
            
            |> fun (path, qfp) ->
                if x.Plain.IsPresent
                then
                     x.WriteObject <| { Path = path; PlainFingerprint.Fingerprint = qfp.Value }
                else
                     x.WriteObject <| { Path = path; TypedFingerprint.Fingerprint = qfp }
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified

and TypedFingerprint =
    {
        Path : string
        Fingerprint : QualifiedFingerprint
    }

and PlainFingerprint =
    {
        Path : string
        Fingerprint : string
    }
