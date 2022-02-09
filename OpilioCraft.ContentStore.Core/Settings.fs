namespace OpilioCraft.ContentStore.Core

open System
open System.IO
open System.Reflection
open System.Text.Json

open OpilioCraft.FSharp.Prelude

exception IncompatibleVersionException of Expected : int * Found : int
    with override x.Message = $"[ContentStore] config file version is not matching framework version: expected {x.Expected}, found {x.Found}"
exception UnknownRepositoryException of Name : string
    with override x.Message = $"[ContentStore] no repository with name \"{x.Name}\" found; please check framework configuration"

type UserSettings = {
    Version : int
    Repositories : Map<string,string>
}
   
[<RequireQualifiedAccess>]
module internal Settings =
    // framework version
    let FrameworkVersion = 2

    // location of runtime; e.g. for side-by-side apps
    let AssemblyLocation = Uri(Assembly.GetExecutingAssembly().Location).LocalPath
    let RuntimeBase = Path.GetDirectoryName(AssemblyLocation)

    // location of app specific data
    let AppDataLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ContentStoreFramework")

    // configuration files
    let ConfigFilename = Path.Combine(AppDataLocation, "config.json")
    let OwnerRuleSetFilename = Path.Combine(AppDataLocation, "ruleset-owner.json")

    // user settings
    let private configJsonOptions = JsonSerializerOptions() in
    let private loadUserSettings = UserSettingsHelper.lazyLoad<UserSettings> ConfigFilename configJsonOptions

    let userSettings () = loadUserSettings.Value

    let verifyUserSettings () =
        userSettings ()
        |> (
            fun settings ->
                if settings.Version <> FrameworkVersion
                then
                    raise <| IncompatibleVersionException(FrameworkVersion, settings.Version)
            )

    // accessors
    let ConfigVersion () = userSettings().Version

    let RepositoryPath name =
        let repositories = userSettings().Repositories in

        if repositories.ContainsKey name
        then
            repositories.[name]
        else
            raise <| UnknownRepositoryException name
