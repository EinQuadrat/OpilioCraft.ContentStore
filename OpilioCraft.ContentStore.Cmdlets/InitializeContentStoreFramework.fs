namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsData.Initialize, "ContentStoreFramework")>]
[<OutputType(typeof<unit>)>]
type public InitializeMMToolkitCommand () =
    inherit ContentStoreCommand ()

    [<Parameter>]
    member val PreloadExifTool = SwitchParameter(false) with get, set

    [<Parameter>]
    member val ResetFramework = SwitchParameter(false) with get,set

    [<Parameter>]
    member val ReloadRules = SwitchParameter(false) with get,set

    [<Parameter>]
    member val Cleanup = SwitchParameter(false) with get,set

    // cmdlet funtionality
    override x.EndProcessing() =
        if x.ResetFramework.IsPresent
        then
            ContentStoreManager.initialize()
        
        else if x.Cleanup.IsPresent
        then
            ContentStoreManager.freeResources()
        
        else
            if x.ReloadRules.IsPresent
            then
                ContentStoreManager.updateRules()

            if x.PreloadExifTool.IsPresent
            then
                ContentStoreManager.preloadExifTool() // multiple calls are harmless
