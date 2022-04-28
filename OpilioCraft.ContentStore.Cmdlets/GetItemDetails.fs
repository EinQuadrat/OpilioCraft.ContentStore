namespace OpilioCraft.ContentStore.Cmdlets

open System.Management.Automation
open System.Text.RegularExpressions
open OpilioCraft.FSharp.Prelude
open OpilioCraft.ContentStore.Core

[<Cmdlet(VerbsCommon.Get, "ItemDetails", DefaultParameterSetName="ByIdentifier")>]
[<OutputType(typeof<ItemDetails>)>]
type public GetItemDetailsCommand () =
    inherit RepositoryCommandBase ()

    // cmdlet params
    [<Parameter(Position=1)>]
    member val Select = ".*" with get, set

    // cmdlet funtionality
    override x.ProcessRecord() =
        base.ProcessRecord()

        try
            x.RetrieveItemId()
            |> x.AssertIsManagedItem

            |> Option.map x.ActiveRepository.GetDetails

            |> Option.iter (
                fun itemDetails ->
                    let result = new ItemDetails()
                    let regex = Regex(x.Select, RegexOptions.Compiled)

                    itemDetails.Keys
                    |> Seq.filter regex.IsMatch
                    |> Seq.iter ( fun key -> result.Add(key, itemDetails.[key]) )

                    x.WriteObject result
                )
        with
            | exn -> exn |> x.WriteAsError ErrorCategory.NotSpecified
