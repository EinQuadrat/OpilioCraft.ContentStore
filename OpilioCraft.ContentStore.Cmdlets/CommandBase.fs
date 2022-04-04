namespace OpilioCraft.ContentStore.Cmdlets

open System.IO
open System.Management.Automation
open System.Runtime.CompilerServices

open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

// simplify exception handling
[<Extension>]
type ExceptionExtension =
    [<Extension>]
    static member ToError(exn, errorCategory, targetObject) = ErrorRecord(exn, null, errorCategory, targetObject)

// base class for all content store framework related commands
[<AbstractClass>]
type public CommandBase () =
    inherit PSCmdlet ()

    member val ContentStoreManager = lazy ( ContentStoreManager.CreateInstance() ) with get

    member x.ToAbsolutePath (path : string) =
        if Path.IsPathRooted(path)
        then
            path
        else
            Path.Combine(x.SessionState.Path.CurrentFileSystemLocation.ToString(), path)

    member inline x.WarningIfNone warning maybe =
        maybe |> Option.ifNone ( fun _ -> x.WriteWarning warning )

    member inline x.Assert condition failMessage maybe =
        maybe |> Option.filter condition |> x.WarningIfNone failMessage

    member x.TryFileExists errorMessage path =
        Some path |> x.Assert File.Exists errorMessage

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
    override x.BeginProcessing () =
        base.BeginProcessing ()

        try
            x.ContentStoreManager.Force () |> ignore
        with
            | exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.ResourceUnavailable

    override x.EndProcessing () =
        if x.ContentStoreManager.IsValueCreated then x.ContentStoreManager.Value.Dispose ()
        base.EndProcessing ()
