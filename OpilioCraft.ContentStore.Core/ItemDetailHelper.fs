module OpilioCraft.ContentStore.Core.ItemDetailHelper

// needed to support ObjectPath expression for ItemDetail objects
let unwrapItemDetail (incoming : 'a) : obj =
    match box incoming with
    | :? ItemDetail as itemDetail -> itemDetail.Unwrap
    | otherwise -> otherwise
