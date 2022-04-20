namespace OpilioCraft.ContentStore.Cmdlets

open OpilioCraft.ContentStore.Core

[<AbstractClass>]
type public ContentStoreCommand () =
    inherit CommandBase ()

    member val ContentStoreManager = ContentStoreManager.CreateInstance()

    // basic functionality provided for all content store framework commands
    override x.EndProcessing () =
        x.ContentStoreManager.Dispose ()
        base.EndProcessing ()
