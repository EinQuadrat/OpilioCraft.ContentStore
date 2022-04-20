namespace OpilioCraft.ContentStore.Core

open System
open System.IO
open System.Reflection

[<RequireQualifiedAccess>]
module internal Settings =
    let FrameworkVersion = Version(2, 1)

    // location of runtime; e.g. for side-by-side apps
    let AssemblyLocation = Uri(Assembly.GetExecutingAssembly().Location).LocalPath
    let RuntimeBase = Path.GetDirectoryName(AssemblyLocation)

    // location of app specific data
    let AppDataLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ContentStoreFramework")
    let RulesLocation = Path.Combine(AppDataLocation, "rules")

    // configuration files
    let FrameworkConfigFilename = Path.Combine(AppDataLocation, "config.json")
