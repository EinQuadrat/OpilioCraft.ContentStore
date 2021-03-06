namespace OpilioCraft.ContentStore.Core

open System
open System.Text.Json.Serialization
open OpilioCraft.FSharp.Prelude

// exceptions
exception UnknownRepositoryException of Name : string
    with override x.Message = $"[ContentStore] no repository with name \"{x.Name}\" found; please check framework configuration"

// framework config
type FrameworkConfig =
    {
        Version      : Version
        Repositories : Map<string,string>
        Rules        : Map<string,string>
    }

// repository config
type RepositoryConfig =
    {
        Version     : Version
        Layout      : RepositoryLayout
        Prefetch    : bool
    }

and RepositoryLayout =
    {
        Items       : string
        Storage     : string
    }

// ----------------------------------------------------------------------------

type ContentCategory =
    | Unknown = 0
    | Image = 1
    | Movie = 2
    | Digitalized = 3 // digitalized former analogue stuff

type ContentType =
    {
        Category      : ContentCategory
        FileExtension : string
    }

// ----------------------------------------------------------------------------

type FileIdentificator =
    {
        FileInfo    : IO.FileInfo
        AsOf        : DateTime
        Fingerprint : Fingerprint
    }

// ----------------------------------------------------------------------------

type RelationType =
    | Related = 0
    | Sibling = 1
    | Derived = 2

type Relation = {
    Target : Fingerprint
    IsA    : RelationType
}

// ----------------------------------------------------------------------------

type ItemDetail =
    | Boolean of bool
    | Float of float
    | Number of decimal
    | DateTime of DateTime
    | TimeSpan of TimeSpan
    | String of string

    member x.Unwrap : obj =
        match x with
        | Boolean plainValue  -> plainValue :> obj
        | Float plainValue    -> plainValue :> obj
        | Number plainValue   -> plainValue :> obj
        | DateTime plainValue -> plainValue :> obj
        | TimeSpan plainValue -> plainValue :> obj
        | String plainValue   -> plainValue :> obj

    member x.AsBoolean : bool =
        match x with
        | Boolean value -> value
        | _ -> failwith $"cannot cast {x.GetType().FullName} to System.Boolean"

    member x.AsDateTime : System.DateTime =
        match x with
        | DateTime value -> value
        | _ -> failwith $"cannot cast {x.GetType().FullName} to System.DateTime"

    member x.AsString : string =
        match x with
        | String value -> value
        | _ -> failwith $"cannot cast {x.GetType().FullName} to System.String"

    static member ofFlexibleValue fval =
        match fval with
        | FlexibleValue.Boolean x  -> Boolean x
        | FlexibleValue.Numeral x  -> decimal x |> Number
        | FlexibleValue.Float x    -> Float x
        | FlexibleValue.Decimal x  -> Number x
        | FlexibleValue.DateTime x -> DateTime x
        | FlexibleValue.TimeSpan x -> TimeSpan x
        | FlexibleValue.String x   -> String x
        // value changing operations
        | FlexibleValue.Date x     -> x.ToDateTime(System.TimeOnly.MinValue) |> DateTime
        // unsupported cases
        | _ -> failwith "cannot convert given FlexibleValue to ItemDetail"

type ItemDetails = System.Collections.Generic.Dictionary<string,ItemDetail>

// ----------------------------------------------------------------------------

type ItemId = Fingerprint

type RepositoryItem = // data structure used by Repository
    {
        Id          : ItemId // use fingerprint (SHA256) as id
        AsOf        : System.DateTime // as UTC timestamp
        ContentType : ContentType
        Relations   : Relation list
        Details     : ItemDetails
    }

    [<JsonIgnore>]
    member x.AsOfLocal = x.AsOf.ToLocalTime()
