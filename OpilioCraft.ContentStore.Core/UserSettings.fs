namespace OpilioCraft.ContentStore.Core

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
    let private loadFrameworkConfig = UserSettingsHelper.lazyLoad<FrameworkConfig> Settings.FrameworkConfigFilename frameworkConfigJsonOptions
    let frameworkConfig () = loadFrameworkConfig.Value

    let verifyFrameworkConfig () =
        frameworkConfig ()
        |> Verify.isVersion Settings.FrameworkVersion
        |> ignore

    // accessors
    let RepositoryPath name =
        let repositories = frameworkConfig().Repositories in

        if repositories.ContainsKey name
        then
            repositories.[name]
        else
            raise <| UnknownRepositoryException name
