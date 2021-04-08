[<AutoOpen>]
module ProjectSync.Types.TypeHelpers

open ProjectSync.Types
open MaybeList
    
let inline (|+|) item items = item |> cons items
let inline (|?|) item items = item |> consM items
