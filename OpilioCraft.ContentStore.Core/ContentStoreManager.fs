namespace OpilioCraft.ContentStore.Core

open System
open OpilioCraft.FSharp.Prelude

/// <summary>Global entry point for using the Content Store Framework.</summary>
/// <para>
/// Creating an instance of ContentStoreManager before using the members of the library
/// ensures that everything is initialized properly. </para>
/// <para>
/// The UseXXX() methods offer a way to control long-term resources. </para>
[<Sealed>]
type ContentStoreManager private () =
    inherit DisposableBase ()

    [<Literal>]
    let ResourceExifTool = "ExifTool"

    let mutable _isDisposed = false
    let mutable _usedResources : Map<string, DisposeDelegate> = Map.empty

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

    // assure valid framework configuration before instantiating ContentStoreManager
    static member CreateInstance () =
        try
            // check framework
            UserSettings.verifyFrameworkConfig ()
                // will throw IncompleteSetupException if missing
                // will throw InvalidUserSettingsException if config file is of wrong format
                // will throw IncompatibleVersionException if version in config file is not framework version

            // create instance
            new ContentStoreManager ()
        with
            | exn -> Console.WriteLine $"unexpected exception: {exn.Message}"; raise exn
        
and DisposeDelegate = unit -> unit
