namespace OpilioCraft.ContentStore.Core

open System
open System.Text.Json
open OpilioCraft.FSharp.Prelude

exception RepositoryNotFoundException of Root : string
    with override x.ToString () = $"no repository found at location {x.Root}"

[<RequireQualifiedAccess>]
module ContentStoreManager =
    // defaults
    [<Literal>]
    let RepositoryConfigFilename = "repository.json"
    
    [<Literal>]
    let NameOfDefaultRepository  = "DEFAULT"

    [<Literal>]
    let ResourceExifTool         = "ExifTool"

    // managed resources
    let mutable private repositoryCache : Map<string, Repository> = Map.empty
    let mutable private rulesProvider : RulesProvider = RulesProvider()
    let mutable private usedResources : Map<string, IDisposable> = Map.empty

    // repository access
    let private loadRepository name =
        let pathToRepository = UserSettings.RepositoryPath name
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
        let repos = Repository (pathToRepository, repositoryConfig, false)
        repos.InjectRules rulesProvider

    let getRepository name =
        if (not <| Map.containsKey name repositoryCache)
        then
            repositoryCache <- repositoryCache |> Map.add name (loadRepository name)

        repositoryCache.[name]

    let reloadRepository name =
        repositoryCache <- repositoryCache |> Map.remove name // remove from cache to force reload
        getRepository name

    let getDefaultRepository () = getRepository NameOfDefaultRepository

    // rules management
    let updateRules () =
        rulesProvider <- RulesProvider (UserSettings.frameworkConfig())
        repositoryCache |> Map.iter (fun _ repo -> repo.InjectRules rulesProvider |> ignore)

    let tryGetRule = rulesProvider.TryGetRule
    let tryApplyRule = rulesProvider.TryApplyRule

    let getRulesProvider () = rulesProvider

    // resource management
    let preloadExifTool () =
        if not <| usedResources.ContainsKey(ResourceExifTool)
        then
            usedResources <- usedResources |> Map.add ResourceExifTool (ExifTool.Proxy ())

    let freeResources () =
        usedResources |> Map.iter (fun _ disposable -> disposable.Dispose())
        usedResources <- Map.empty

    // overall initialization
    let initialize () =
        // check config
        UserSettings.verifyFrameworkConfig ()
            // will throw IncompleteSetupException if missing
            // will throw InvalidUserSettingsException if config file is of wrong format
            // will throw IncompatibleVersionException if version in config file is not framework version

        // reset caches
        freeResources()
        repositoryCache <- Map.empty
        updateRules()
