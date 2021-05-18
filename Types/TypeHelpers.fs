[<AutoOpen>]
module ProjectSync.Types.TypeHelpers

open Utils.Maybe.MaybeList
    
let inline (|+|) item items = item |> cons items
let inline (|?|) item items = item |> consM items
