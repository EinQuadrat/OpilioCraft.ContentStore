namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<AbstractClass>]
type public ContentStoreCommand () =
    inherit CommandBase ()

    [<Parameter>]
    member val ReloadFramework = SwitchParameter(false) with get,set

    // support framework reload to re-read configuration
    member x.ContentStoreManager = ContentStoreManager.GetInstance(x.ReloadFramework.IsPresent)

    // basic functionality provided for all content store framework commands
    override x.EndProcessing () =
        x.ContentStoreManager.Dispose()
        base.EndProcessing ()
