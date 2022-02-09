namespace OpilioCraft.ContentStore.Core

open System
open OpilioCraft.FSharp.Prelude

module ContentStore =
    let initialize () =
        // check configuration
        Console.WriteLine "Checking settings"
        Settings.verifyUserSettings ()
            // will throw IncompleteSetupException if missing
            // will throw InvalidUserSettingsException if config file is of wrong format
            // will throw IncompatibleVersionException if version in config file is not framework version

        // check ruleset
        try
            Console.WriteLine "Loading heuristik"
            Heuristik.OwnerRuleSet |> ignore
        with
        | exn -> raise (RuleSetError(Name = Settings.OwnerRuleSetFilename, Exception = exn))


/// <summary>Global entry point for using the Content Store Framework.</summary>
/// <para>
/// Creating an instance of ContentStoreManager before using the members of the library
/// ensures that everything is initialized properly. </para>
/// <para>
/// The UseXXX() methods offer a way to control long-term resources. </para>
[<Sealed>]
type ContentStoreManager () =
    inherit DisposableBase ()

    [<Literal>]
    let ResourceExifTool = "ExifTool"

    let mutable _isDisposed = false
    let mutable _usedResources : Map<string, DisposeDelegate> = Map.empty

    static do
        // initialization does several checks, will throw exceptions on non-compliances
        ContentStore.initialize ()

    // notify resource use
    member _.UseExifTool () =
        if not <| _usedResources.ContainsKey(ResourceExifTool)
        then
            let exifTool = ExifTool.Proxy () in
            _usedResources <- _usedResources |> Map.add ResourceExifTool ( fun () -> (exifTool :> IDisposable).Dispose() )

    // provide cleanup
    override _.DisposeManagedResources () =
        if not _isDisposed
        then
            _usedResources |> Map.iter ( fun _ disposer -> try disposer () with exn -> Console.Error.WriteLine $"[{nameof ContentStoreManager}] cleanup exception: {exn.Message}" )
            _usedResources <- Map.empty
            _isDisposed <- true

        base.DisposeManagedResources ()

and DisposeDelegate = unit -> unit
