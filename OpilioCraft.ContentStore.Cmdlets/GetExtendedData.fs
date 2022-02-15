﻿namespace OpilioCraft.ContentStore.Cmdlets

open System
open System.Collections
open System.Collections.Generic
open System.Management.Automation
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "ExtendedData")>]
[<OutputType(typeof<ExtendedData>, typeof<Hashtable>)>]
type public GetExtendedDataCommand () =
    inherit CommandBase ()

    // cmdlet params
    [<Parameter(Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)>]
    [<Alias("FullName")>] // to be compatible with Get-Item result
    member val Path = String.Empty with get, set

    [<Parameter>]
    member val AsHashtable = SwitchParameter(false) with get, set

    // cmdlet funtionality
    override x.BeginProcessing () =
        base.BeginProcessing () // initialize MMToolkit

        try
            x.ContentStoreManager.Value.UseExifTool()
        with
        | exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.ResourceUnavailable

    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.Path
            |> x.ToAbsolutePath
            |> x.TryFileExists $"given file does not exist or is not accessible: {x.Path}"
            |> Option.map (fun path -> path, path |> System.IO.FileInfo |> Utils.getContentCategory)
            |> Option.map ( fun (path, category) ->
                let extendedData = Utils.getCategorySpecificDetails (System.IO.FileInfo path) category

                if x.AsHashtable.IsPresent
                then
                    extendedData
                    |> Seq.cast<KeyValuePair<string, ItemDetail>>
                    |> Seq.fold (fun (ht : Hashtable) item -> ht.Add(item.Key, item.Value.Unwrap); ht) (Hashtable())
                    :> obj
                else
                    { Path = path; Details = extendedData } :> obj
                )
            |> Option.iter x.WriteObject
        with
        | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified

and ExtendedData =
    {
        Path : string
        Details : Dictionary<string, ItemDetail>
    }