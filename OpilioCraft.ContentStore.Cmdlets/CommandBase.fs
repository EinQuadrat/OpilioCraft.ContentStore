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

// base class for all cmdlets, providing some helper functionality
[<AbstractClass>]
type public CommandBase () =
    inherit PSCmdlet ()

    member x.ToAbsolutePath (path : string) =
        if Path.IsPathRooted(path)
        then
            path
        else
            Path.Combine(x.SessionState.Path.CurrentFileSystemLocation.ToString(), path)

    member x.WarnIfNone warning =
        Option.ifNone ( fun _ -> x.WriteWarning warning )

    member x.WarnIfFalse warning input =
        if not input then x.WriteWarning warning
        input

    member _.AssertFileExists errorMessage path =
        if not <| File.Exists path then failwith $"{errorMessage}: {path}"
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
