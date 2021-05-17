module ProjectSync.Types.Possibly
open Utils.Maybe
open Utils.Maybe.Maybe

let map f (possibility: _ maybe) =
    maybe {
        let! item = possibility
        
        return
            f item
    }
    
let check f possibility =
    possibility
    |> map f
    |> toBool