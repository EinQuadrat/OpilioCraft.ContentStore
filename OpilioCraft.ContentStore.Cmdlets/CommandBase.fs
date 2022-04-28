namespace OpilioCraft.ContentStore.Cmdlets

open System.IO
open System.Management.Automation
open System.Runtime.CompilerServices
open OpilioCraft.FSharp.Prelude

// simplify exception handling
[<Extension>]
type ExceptionExtension =
    [<Extension>]
    static member ToError(exn, errorCategory, targetObject) = ErrorRecord(exn, null, errorCategory, targetObject)

// simplify conditions
[<RequireQualifiedAccess>]
module Ensure =
    let test condition input =
        if condition input
        then
            Result.Ok input
        else
            Result.Error input

    let fileExists path = File.Exists path

// base class for all content store framework related commands
[<AbstractClass>]
type public CommandBase () =
    inherit PSCmdlet ()

    member x.ToAbsolutePath (path : string) =
        if Path.IsPathRooted(path)
        then
            path
        else
            Path.Combine(x.SessionState.Path.CurrentFileSystemLocation.ToString(), path)

    member inline x.WarningIfNone warning maybe =
        maybe |> Option.ifNone ( fun _ -> x.WriteWarning warning )

    member inline x.WarnIfFalse warning input =
        if not input then x.WriteWarning warning
        input

    member _.AssertFileExists errorMessage path =
        let testResult = path |> Ensure.fileExists in
        if not testResult then failwith $"{errorMessage}: {path}"
        path

    // simplify error handling
    member x.WriteAsError errorCategory (exn : #System.Exception) =
        exn.ToError(errorCategory, x)
        |> x.WriteError

    member x.ThrowAsTerminatingError errorCategory (exn : #System.Exception) =
        exn.ToError(errorCategory, x)
        |> x.ThrowTerminatingError

    member x.ThrowValidationError errorMessage errorCategory =
        errorMessage
        |> ParameterBindingException
        |> x.ThrowAsTerminatingError errorCategory

    // basic functionality provided for all content store framework commands
    override x.EndProcessing () =
        base.EndProcessing ()
