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
    member val Force : SwitchParameter = SwitchParameter(false) with get, set

    [<Parameter>]
    member val Plain : SwitchParameter = SwitchParameter(false) with get, set

    override x.ProcessRecord () =
        base.ProcessRecord ()

        try
            x.Path
            |> x.ToAbsolutePath
            |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
            |> Option.map
                ( fun path ->
                    path,
                    path |> if x.Force.IsPresent then Fingerprint.getFullFingerprint else Fingerprint.getFingerprint
                )
            |> Option.iter
                ( fun (path, qfp) ->
                    if x.Plain.IsPresent
                    then
                        { Path = path; PlainFingerprint.Fingerprint = qfp.Value } |> x.WriteObject
                    else
                        { Path = path; TypedFingerprint.Fingerprint = qfp } |> x.WriteObject
                )
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
