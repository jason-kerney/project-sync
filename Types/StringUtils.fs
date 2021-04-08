[<AutoOpen>]
module ProjectSync.Types.StringUtils

open System
open ProjectSync.Types

let splitBy (separators: char[]) (text: string) =
    text.Split (separators, StringSplitOptions.RemoveEmptyEntries)
    |> Array.toList
    
let msplitBy (separators: char []) = splitBy separators |> lift

let split (text: string) =
    text |> splitBy [|'\r';'\n'|]
    
let msplit : _ maybe -> _ mlist = Result.map split

let joinBys (separator: string) (items: string seq) =
    String.Join (separator, items)
    
let joinBy (separator: char) (items: string seq) =
    String.Join (separator, items)
    
let mjoinBy separator : _ mlist -> _ maybe = joinBy separator |> Result.map

let mjoinByM separator items =
    maybe {
        let! separator = separator
        
        return!
            items
            |> mjoinBy separator
    }
    
let joinByString (separator: string) (items: string seq) =
    String.Join (separator, items)
    
let mjoinByString separator : _ maybe -> _ maybe = separator |> joinByString |> Result.map

let mjoinByStringM separator items =
    maybe {
        let! separator = separator
        return!
            items
            |> mjoinByString separator
    }
    
let join (items: string seq) =
    items |> joinBy '\n'
    
let mjoin (items: _ maybe) : _ maybe = items |> Result.map join

let trim (value: string) = value.Trim ()

let mtrim : _ maybe -> _ maybe = trim |> Result.map

let msprintf format = sprintf format |> lift
