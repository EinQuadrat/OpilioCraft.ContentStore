namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsData.Initialize, "ContentStoreFramework")>]
[<OutputType(typeof<unit>, typeof<string>)>]
type public InitializeMMToolkitCommand () =
    inherit ContentStoreCommand ()

    [<Parameter>]
    member val Version = SwitchParameter(false) with get, set

    [<Parameter>]
    member val PreloadExifTool = SwitchParameter(false) with get, set

    [<Parameter>]
    member val ResetFramework = SwitchParameter(false) with get,set

    [<Parameter>]
    member val ReloadRules = SwitchParameter(false) with get,set

    [<Parameter>]
    member val CleanupResources = SwitchParameter(false) with get,set

    // cmdlet funtionality
    override x.EndProcessing() =
        if x.Version.IsPresent
        then
            x.WriteObject(Settings.FrameworkVersion)
        else if x.ResetFramework.IsPresent
        then
            ContentStoreManager.initialize()
        
        else if x.CleanupResources.IsPresent
        then
            ContentStoreManager.freeResources()
        
        else
            if x.ReloadRules.IsPresent
            then
                ContentStoreManager.reloadRules()

            if x.PreloadExifTool.IsPresent
            then
                ContentStoreManager.preloadExifTool() // multiple calls are harmless
