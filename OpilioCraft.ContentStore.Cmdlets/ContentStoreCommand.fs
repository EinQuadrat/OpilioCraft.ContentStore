namespace OpilioCraft.ContentStore.Cmdlets

open System.Threading
open OpilioCraft.ContentStore.Core

[<AbstractClass>]
type public ContentStoreCommand () =
    inherit CommandBase ()

    // prevent redundant initialization
    static let frameworkInitialized = ref 0

    // basic functionality provided for all content store framework commands
    override _.BeginProcessing () =
        base.BeginProcessing()

        if Interlocked.CompareExchange(frameworkInitialized, 1, 0) = 0
        then
            ContentStoreManager.initialize ()
