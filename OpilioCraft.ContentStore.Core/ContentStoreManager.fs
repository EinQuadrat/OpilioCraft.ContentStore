namespace OpilioCraft.ContentStore.Core

open System
open System.Text.Json
open OpilioCraft.FSharp.Prelude

exception RepositoryNotFoundException of Root : string
    with override x.ToString () = $"no repository found at location {x.Root}"

/// <summary>Global entry point for using the Content Store Framework.</summary>
/// <para>
/// Creating an instance of ContentStoreManager before using the members of the library
/// ensures that everything is initialized properly. </para>
/// <para>
/// The UseXXX() methods offer a way to control long-term resources. </para>
[<Sealed>]
type ContentStoreManager private ( frameworkConfig : FrameworkConfig ) =
    inherit DisposableBase ()

    // singleton
    static let mutable Instance : ContentStoreManager option = None

    // resource management
    [<Literal>] static let ResourceExifTool = "ExifTool"

    let mutable _isDisposed = false
    let mutable _usedResources : Map<string, DisposeDelegate> = Map.empty

    // repository specific
    [<Literal>] static let RepositoryConfigFilename = "repository.json"
    [<Literal>] static let NameOfDefaultRepository = "DEFAULT"

    let mutable _repositories : Map<string, Repository> = Map.empty

    // rules management
    member val RulesProvider = RulesProvider frameworkConfig

    // repository access
    member private x.LoadRepository(repositoryName : string, ?forcePrefetch : bool) =
        // lookup path to repository
        let pathToRepository = UserSettings.RepositoryPath repositoryName
            // will throw UnknownRepository on a given name not mentioned in configuration file

        if not <| IO.Directory.Exists pathToRepository
        then
            raise <| RepositoryNotFoundException pathToRepository

        // load repository configuration
        let pathToConfigFile = Path.Combine(pathToRepository, RepositoryConfigFilename)

        let repositoryConfig : RepositoryConfig =
            UserSettingsHelper.load<RepositoryConfig> pathToConfigFile (JsonSerializerOptions())
            |> Verify.isVersion Repository.Version

        // load repository
        let repos = Repository (pathToRepository, repositoryConfig, forcePrefetch |> Option.defaultValue false)
        repos.InjectRules x.RulesProvider

        // return it
        repos

    member x.GetRepository(name : string, ?reload : bool) =
        if (not <| Map.containsKey name _repositories) || (reload |> Option.contains true)
        then
            _repositories <- _repositories |> Map.add name (x.LoadRepository(name))

        _repositories.[name]

    member x.GetDefaultRepository(?reload : bool) =
        x.GetRepository(NameOfDefaultRepository, reload |> Option.defaultValue false)

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

    // is a singleton with reload option
    static member GetInstance (?reload) =
        if (reload |> Option.contains true) || Instance.IsNone
        then
            // any existing instance to cleanup?
            if Instance.IsSome
            then
                Instance.Value.Dispose()
                Instance <- None

            // check config
            UserSettings.verifyFrameworkConfig ()
                // will throw IncompleteSetupException if missing
                // will throw InvalidUserSettingsException if config file is of wrong format
                // will throw IncompatibleVersionException if version in config file is not framework version

            // create instance
            Instance <- Some <| new ContentStoreManager (UserSettings.frameworkConfig())

        // return it
        Instance.Value
        
and DisposeDelegate = unit -> unit
