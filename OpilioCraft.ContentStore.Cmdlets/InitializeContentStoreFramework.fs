﻿namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsData.Initialize, "ContentStoreFramework")>]
[<OutputType(typeof<ContentStoreManager>)>]
type public InitializeMMToolkitCommand () =
    inherit CommandBase ()

    [<Parameter>]
    member val WithExifTool = SwitchParameter(false) with get, set

    // cmdlet funtionality
    override x.EndProcessing() =
        if x.WithExifTool.IsPresent
        then
            x.ContentStoreManager.Value.UseExifTool() // MMToolkit is initialized by base class already
        
        x.ContentStoreManager.Value |> x.WriteObject
        // do not call base.EndProcessing() to avoid immediate disposal