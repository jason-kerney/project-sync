module ProjectSync.Types.MaybeList

open ProjectSync.Types
open Utils.Maybe
open Utils.Maybe.Maybe

let reduceErrors (items: Maybe<_> seq) =
    let maybeFailures (items: _ list) =
        if 0 < items.Length then items |> asCombinationFailure
        else Ok ()
        
    items
    |> Seq.toList
    |> List.filter isError
    |> List.map (fun (Error e) -> e)
    |> maybeFailures
    
let exists predicate (items: _ mlist) =
    maybe {
        let! items = items
        return
            items
            |> List.exists predicate
    } |> toBool
    
let find predicate : _ mlist -> _ maybe =
    predicate
    |> List.find
    |> lift

let filter predicate : _ mlist -> _ mlist =
    predicate
    |> List.filter
    |> lift
    
let sort (list: _ mlist) : _ mlist =
    let sort = 
        List.sort
        |> lift
        
    list |> sort

let flatten (items: Maybe<_> list) : _ mlist =
    maybe {
        let errors =
            items
            |> List.filter (fun item -> match item with Error _ -> true | _ -> false)
            |> List.map (fun (Error item) -> item)
        
        if 0 < errors.Length
        then return! errors |> asCombinationFailure
        else
            return
                items
                |> List.map (fun (Ok item) -> item)
    }
    
let map f : _ mlist -> _ mlist = f |> List.map |> lift

let mapM f items : _ mlist =
    maybe {
        let! items =
            items |> map (fun a -> a |> asMaybe |> f)
            
        return! items |> flatten
    }
    
let cons (items: _ mlist) item : _ mlist =
    maybe {
        let! items = items
        return item::items
    }

let consM items item : _ mlist =
    maybe {
        let! item = item
        return! item |> cons items
    }

let head (list: _ mlist) = (List.head |> lift) list

let tail (list: _ mlist) = (List.tail |> lift) list

let toListM item : _ mlist =
    maybe {
        let! item = item
        return item::[]
    }
    
let iter f (items: _ mlist) =
    maybe {
        let! items = items
        
        return
            items
            |> List.iter f
    }

let iterM (f: _ maybe -> unit maybe) (items: _ mlist) : unit maybe =
    let result = items |> mapM f
    
    match result with
    | Ok _ -> Ok ()
    | Error e -> Error e

let iter_M (f: _ -> unit maybe) (items: _ mlist) : unit maybe =
    maybe {
        let! items = items
        
        return!
            items
            |> List.map f
            |> reduceErrors
    }
    
let append (itemsA: _ mlist) (itemsB: _ mlist) : _ mlist =
    maybe {
        let! itemsA = itemsA
        let! itemsB = itemsB
        
        return
            itemsB
            |> List.append itemsA
    }

let except (itemsA: _ mlist) (itemsB: _ mlist) : _ mlist =
    maybe {
        let! itemsA = itemsA
        let! itemsB = itemsB
        
        let result =
            itemsB
            |> List.except itemsA
        
        return result
    }
    
let contains item (items: _ mlist) =
    maybe {
        let! items = items
        return
            items
            |> List.contains item
    } |> toBool
    
let simplify (items : _ maybe mlist) : _ mlist =
    maybe {
        let! items = items
        return! items |> flatten
    }
    
let join (items : _ mlist seq) : _ mlist =
    items
    |> Seq.reduce (fun acc current ->
            maybe {
                let! acc_actual = acc
                let! current_actual = current
                
                return
                    acc_actual
                    |> List.append current_actual
            }
        )