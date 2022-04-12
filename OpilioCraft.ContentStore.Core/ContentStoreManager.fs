namespace OpilioCraft.ContentStore.Core

open System
open System.IO
open System.Text.Json

open OpilioCraft.FSharp.Prelude
open OpilioCraft.Lisp.Runtime

/// <summary>Global entry point for using the Content Store Framework.</summary>
/// <para>
/// Creating an instance of ContentStoreManager before using the members of the library
/// ensures that everything is initialized properly. </para>
/// <para>
/// The UseXXX() methods offer a way to control long-term resources. </para>
[<Sealed>]
type ContentStoreManager private ( frameworkConfig : FrameworkConfig ) =
    inherit DisposableBase ()

    [<Literal>] static let ConfigFilename = "repository.json"
    [<Literal>] static let DefaultRepository = "DEFAULT"
    [<Literal>] static let ResourceExifTool = "ExifTool"

    let mutable _isDisposed = false
    let mutable _usedResources : Map<string, DisposeDelegate> = Map.empty

    // repository access
    member _.LoadRepository(repositoryName : string, ?forcePrefetch : bool) =
        let pathToRepository = UserSettings.RepositoryPath repositoryName
            // will throw UnknownRepository on a given name not mentioned in configuration file

        if not <| Directory.Exists pathToRepository
        then
            raise <| RepositoryNotFoundError pathToRepository

        let pathToConfigFile = Path.Combine(pathToRepository, ConfigFilename)

        let config =
            UserSettingsHelper.load pathToConfigFile (JsonSerializerOptions())
            |> Verify.isVersion Repository.Version

        let repos = Repository (pathToRepository, config, forcePrefetch |> Option.defaultValue false)

        

    member x.LoadDefaultRepository(?forcePrefetch : bool) =
        x.LoadRepository(DefaultRepository, forcePrefetch |> Option.defaultValue false)

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

            // load models
            let models =
                UserSettings.frameworkConfig().Models
                |> Map.map (
                    fun modelName modelPath ->
                        
                    )

            // create instance
            new ContentStoreManager (UserSettings.frameworkConfig ())
        with
            | exn -> Console.WriteLine $"unexpected exception: {exn.Message}"; raise exn
        
and DisposeDelegate = unit -> unit
