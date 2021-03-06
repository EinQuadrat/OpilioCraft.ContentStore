namespace OpilioCraft.ContentStore.Core

open System.IO
open System.Text.Json
open OpilioCraft.FSharp.Prelude
   
[<RequireQualifiedAccess>]
module internal UserSettings =
    // JSON serializer configuration
    let frameworkConfigJsonOptions =
        JsonSerializerOptions()
        |> fun jsonOpts ->
            jsonOpts.WriteIndented <- true
            jsonOpts

    // load user settings on demand
    let private loadFrameworkConfig = UserSettingsHelper.lazyLoad<FrameworkConfig> Settings.FrameworkConfigFile frameworkConfigJsonOptions
    let frameworkConfig () = loadFrameworkConfig.Value

    let private assertRuleFileExists ruleName ruleDefinitionFile =
        if not <| File.Exists ruleDefinitionFile
        then
            raise <| InvalidUserSettingsException(Settings.FrameworkConfigFile, $"definition file of rule {ruleName} does not exist")

    let verifyFrameworkConfig () =
        frameworkConfig ()
        |> Verify.isVersion Settings.FrameworkVersion

        |> UserSettingsHelper.tryGetProperty "Rules"
        |> Option.iter (Map.iter assertRuleFileExists)

    // accessors
    let RepositoryPath name =
        let repositories = frameworkConfig().Repositories in

        if repositories.ContainsKey name
        then
            repositories.[name]
        else
            raise <| UnknownRepositoryException name
