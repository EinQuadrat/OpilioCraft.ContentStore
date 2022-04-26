namespace OpilioCraft.ContentStore.Core

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

open OpilioCraft.FSharp.Prelude
open Utils // load FileIdentifier extensions

// Repository main class
type Repository internal (root : string, config : RepositoryConfig, forcePrefetch) as this =

    static let ImplementationVersion = Version(2, 0)
    
    // ------------------------------------------------------------------------
    // json settings

    static let jsonOptions =
        JsonSerializerOptions()
        |> fun jsonOpts ->
            jsonOpts.WriteIndented <- true
            jsonOpts.Converters.Add(JsonStringEnumConverter(JsonNamingPolicy.CamelCase))
            jsonOpts.Converters.Add(RelationListConverter())
            jsonOpts.Converters.Add(DetailsConverter())
            jsonOpts

    // ------------------------------------------------------------------------
    // repository filesystem layout

    static let (>+>) basePath part = (basePath, part) |> Path.Combine
    let itemsSection = root >+> config.Layout.Items
    let storageSection = if Path.IsPathRooted(config.Layout.Storage) then config.Layout.Storage else root >+> config.Layout.Storage

    let itemFile (id : ItemId) = itemsSection >+> $"{id}.json"
    let itemIdFromFilename (filename : string) = filename.Substring(0, filename.Length - ".json".Length)
    let itemIdFromFileInfo (fi : FileInfo) = fi.Name |> itemIdFromFilename
    let contentFile (id : ItemId) ext = storageSection >+> $"{id}{ext}"

    // ------------------------------------------------------------------------

    let cache = new Dictionary<ItemId,RepositoryItem>()

    do
        if (config.Prefetch || forcePrefetch)
        then
            this.PopulateCache ()

    // ------------------------------------------------------------------------

    let mutable rulesProvider = RulesProvider () // initialize with empty rules table

    // ------------------------------------------------------------------------

    static member Version = ImplementationVersion

    // ------------------------------------------------------------------------
    // cache management

    member x.PopulateCache () =
        Directory.EnumerateFiles(itemsSection, "*.json")
        |> Seq.map FileInfo
        |> Seq.map ( fun fi -> fi |> itemIdFromFileInfo )
            // filename without extension = repository item id
        |> Seq.iter ( fun id -> if not <| cache.ContainsKey(id) then cache.Add(id, x.FetchItem id) )

    // ------------------------------------------------------------------------

    member x.InjectRules (provider : RulesProvider) = rulesProvider <- provider

    // ------------------------------------------------------------------------

    // low-level item access; use GetItem() or GetItemWithoutCaching instead
    member private _.FetchItem (id : ItemId) : RepositoryItem =
        try
            let itemAsJson = id |> itemFile |> File.ReadAllText in
            JsonSerializer.Deserialize<RepositoryItem>(itemAsJson, jsonOptions)
        with
            | exn -> failwith $"[{nameof Repository}] cannot read item data for id {id}: {exn.Message}"

    member private _.StoreItem (item : RepositoryItem) =
        try
            IO.saveGuard (item.Id |> itemFile)
            <| fun uri -> let itemAsJson = JsonSerializer.Serialize(item, jsonOptions) in File.WriteAllText(uri, itemAsJson)
            cache.[item.Id] <- item // update cache
        with
            | _ -> item.Id |> cache.Remove |> ignore; reraise() // cleanup cache

    // existence
    member _.IsManagedId =
        itemFile >> File.Exists

    // item access
    member x.GetItem (id : ItemId) : RepositoryItem =
        if not <| cache.ContainsKey(id) then cache.[id] <- x.FetchItem id // populate cache on demand
        cache.[id]        

    member x.GetItemWithoutCaching (id : ItemId) : RepositoryItem =
        x.FetchItem id
        
    // relations
    member x.HasRelations (id : ItemId) : bool =
        id |> x.GetItem |> (fun item -> item.Relations.Length > 0)
           
    member x.GetRelations id =
        id |> x.GetItem |> (fun item -> item.Relations)
    
    member x.IsRelatedTo (id : ItemId) (targetId : ItemId) : bool =
        id |> x.GetRelations |> List.exists (fun rel -> rel.Target = targetId)

    member x.HasRelationTo (id : ItemId) (targetId : ItemId) (relType : RelationType) : bool =
        id |> x.GetRelations |> List.exists (fun rel -> (rel.Target = targetId) && (rel.IsA = relType))

    member x.AddRelation id (rel : Relation) =
        let item = id |> x.GetItem in
        if not(x.HasRelationTo id rel.Target rel.IsA) then { item with Relations = [ rel ] |> List.append item.Relations } |> x.StoreItem

    member x.ForgetRelationTo id specificRel =
        let item = id |> x.GetItem in
        { item with Relations = item.Relations |> List.filter ( fun rel -> rel <> specificRel ) } |> x.StoreItem

    member x.ForgetRelationsTo id targetId =
        let item = id |> x.GetItem in
        { item with Relations = item.Relations |> List.filter (fun rel -> rel.Target <> targetId) } |> x.StoreItem

    member x.ForgetRelations id =
        let item = id |> x.GetItem in
        { item with Relations = [] } |> x.StoreItem

    // details
    member x.HasDetail id name =
        let item = id |> x.GetItem in item.Details.ContainsKey(name)

    member x.GetDetail id name defaultValue =
        let item = id |> x.GetItem in
        if item.Details.ContainsKey(name) then item.Details.[name] else defaultValue

    member x.SetDetail id name value =
        let item = id |> x.GetItem in
        item.Details.[name] <- value
        item |> x.StoreItem

    member x.GetDetails =
        x.GetItem >> fun item -> item.Details

    member x.SetDetails id modifications =
        let item = id |> x.GetItem
        modifications |> Map.iter ( fun key value -> item.Details.[key] <- value )        
        item |> x.StoreItem

    member x.SetDetailsTo id itemDetails =
        let item = id |> x.GetItem in
        { item with Details = itemDetails } |> x.StoreItem

    member x.UnsetDetail id name =
        let item = id |> x.GetItem in
        
        name
        |> item.Details.Remove
        |> fun needsUpdate -> if needsUpdate then item |> x.StoreItem

    member x.UnsetDetails id (names : string list) =
        let item = id |> x.GetItem in
        
        names
        |> List.map item.Details.Remove
        |> List.contains true
        |> fun needsUpdate -> if needsUpdate then item |> x.StoreItem

    // data files
    member x.HasFile id =
        let item = id |> x.GetItem in
        (id, item.ContentType.FileExtension) ||> contentFile |> File.Exists

    member private x.ImportFile id (fileInfo : FileInfo) =
        let dataFile = (id, fileInfo.Extension) ||> contentFile

        try
            File.Copy(fileInfo.FullName, dataFile, false) // prevent accidentially overwrite of existing files
        with
            | exn -> failwith $"[{nameof Repository}] cannot import file for id {id}: {exn.Message}"

    member x.ExportFile id targetPath overwrite =
        let item = id |> x.GetItem
        let reposFile = (id, item.ContentType.FileExtension) ||> contentFile

        try
            File.Copy(reposFile, targetPath, overwrite)
        with
            | exn -> failwith $"[{nameof Repository}] cannot export file for id {id}: {exn.Message}"

    // forget data
    member x.Forget (id : ItemId) : unit =
        if id |> x.IsManagedId
        then
            try
                id |> cache.Remove |> ignore
                id |> itemFile |> File.Delete
                
                let item = id |> x.GetItem in
                (id, item.ContentType.FileExtension) ||> contentFile |> File.Delete
            with
                | exn -> failwith $"[{nameof Repository}] cannot cleanup resources related to id {id}: {exn.Message}"

    // high-level api
    member x.AddToRepository(fident : FileIdentificator, ?contentCategoryOverwrite) : ItemId =
        let itemId = fident.Fingerprint

        if not (itemId |> x.IsManagedId)
        then
            let contentType =
                contentCategoryOverwrite
                |> Option.map (fun category -> { Category = category; FileExtension = fident.FileInfo.Extension })
                |> Option.defaultValue (fident.FileInfo |> getContentType)

            let mutable item =
                {
                    Id = itemId
                    AsOf = fident.AsOf
                    ContentType = contentType
                    Relations = List.empty<Relation>
                    Details = Utils.getCategorySpecificDetails fident.FileInfo contentType.Category rulesProvider
                }

            // reset AsOf to DateTaken if newer
            if item.Details.ContainsKey(Slot.DateTaken)
            then
                let dateTaken = item.Details.[Slot.DateTaken].AsDateTime.ToUniversalTime()
                item <- { item with AsOf = dateTaken }

            item |> x.StoreItem
            fident.FileInfo |> x.ImportFile itemId

        itemId // facilitate chaining

    member x.GetItemIds () : seq<ItemId> =
        itemsSection
        |> Directory.EnumerateFiles
        |> Seq.map (fun filename -> filename |> FileInfo |> (fun fi -> fi.Name |> itemIdFromFilename))

    // details maintenance
    member x.ReadDetailsFromFile id =
        let item = id |> x.GetItem
        let contentFileInfo = (id, item.ContentType.FileExtension) ||> contentFile |> FileInfo
        Utils.getCategorySpecificDetails contentFileInfo item.ContentType.Category rulesProvider

    member x.ResetDetails id =
        id
        |> x.ReadDetailsFromFile
        |> x.SetDetailsTo id

// serialization handling
and DetailsConverter() =
    inherit JsonConverter<ItemDetail>()

    [<Literal>] static let DateTimePrefix = "#DateTime#"
    [<Literal>] static let DateTimeOffset = 11 // length of prefix + 1 for trailing whitespace
    [<Literal>] static let TimeSpanPrefix = "#TimeSpan#"
    [<Literal>] static let TimeSpanOffset = 11 // length of prefix + 1 for trailing whitespace
    [<Literal>] static let FloatPrefix = "#Float#"
    [<Literal>] static let FloatOffset = 8 // length of prefix + 1 for trailing whitespace

    override _.Read (reader: byref<Utf8JsonReader>, _: Type, _: JsonSerializerOptions) =
        match reader.TokenType with
        | JsonTokenType.False
        | JsonTokenType.True -> reader.GetBoolean() |> ItemDetail.Boolean

        | JsonTokenType.Number -> reader.GetDecimal() |> ItemDetail.Number

        | JsonTokenType.String ->
            // smart string handling
            reader.GetString()
            |> function
                | s when s.StartsWith(DateTimePrefix) -> s.Substring(DateTimeOffset) |> DateTime.Parse |> ItemDetail.DateTime
                | s when s.StartsWith(TimeSpanPrefix) -> s.Substring(TimeSpanOffset) |> TimeSpan.Parse |> ItemDetail.TimeSpan
                | s when s.StartsWith(FloatPrefix) -> s.Substring(FloatOffset) |> Double.Parse |> ItemDetail.Float
                | s -> s |> ItemDetail.String

        | _ -> failwith $"[{nameof Repository}] unexpected data type while deserializing"
    
    override _.Write (writer: Utf8JsonWriter, value: ItemDetail, options: JsonSerializerOptions) =
        let dateTimeFormat = @"yyyy-MM-ddTHH\:mm\:sszzz"
        
        match value with
        | Boolean x -> writer.WriteBooleanValue x
        | DateTime x -> writer.WriteStringValue $"{DateTimePrefix} {x.ToString(dateTimeFormat)}"
        | Float x -> writer.WriteStringValue $"{FloatPrefix} {x.ToString()}" // preserve precision of number value by string wrapper
        | Number x -> writer.WriteNumberValue x
        | String x -> writer.WriteStringValue x
        | TimeSpan x -> writer.WriteStringValue $"{TimeSpanPrefix} {x.ToString()}"
        
and RelationListConverter() =
    inherit JsonConverter<Relation list>()

    override _.Read (reader: byref<Utf8JsonReader>, _: Type, options: JsonSerializerOptions) =
        JsonSerializer.Deserialize<Relation seq>(&reader, options)
        |> List.ofSeq
    
    override _.Write (writer: Utf8JsonWriter, value: Relation list, options: JsonSerializerOptions) =
        JsonSerializer.Serialize(writer, List.toSeq value, options)
