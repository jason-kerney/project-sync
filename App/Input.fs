module ProjectSync.App.Input

open ProjectSync.Types
open InquirerCS
open InquirerCS.Builders
open InquirerCS.Interfaces
open InquirerCS.Questions

let private prompt (builder: IBuilder<'Question, 'Result>) =
    builder.Prompt ()
    
let private withDefault defaultValue (builder: InputBuilder<_ Input, _, _>) =
    builder.WithDefaultValue(defaultValue)
    
let private queriedAsString defaultValue queryPrompt =
    maybe {
        return 
            queryPrompt
            |> Question.Input
            |> withDefault defaultValue
            |> prompt
    }
    
let private queriedAsSelectList (items: _ seq) query =
    maybe {
        if 0 < (items |> Seq.length) then
            let max = items |> Seq.length
            let cnt = max / 50 + 1
            let itms = items |> Seq.sort |> Seq.splitInto cnt
            return
                itms
                |> Seq.map (fun parts ->
                    (query, parts)
                    |> Question.Checkbox
                    |> prompt
                )
                |> Seq.concat
                |> List.ofSeq
            
        else
            ("No items Available!?!")
            |> Question.Input
            |> withDefault "Ok"
            |> prompt
            |> ignore
            
            return []
    }
    
let private confirmList items query =
    let itemsString =
        items
        |> List.map (sprintf "\t%s")
        |> join
        |> sprintf "[\n%s\n]"
    
    let query = sprintf "%s %s" itemsString query
    
    maybe {
        return 
            Question.Confirm query
            |> prompt
    }
    |> toBool
    
let private querySafeString defaultValue predicate generateFailurePrompt queryPrompt =
    let rec querySafeString queryPrompt =
        let result =
            queryPrompt
            |> queriedAsString defaultValue
            
        let isGood = result |> predicate
        
        if isGood then result
        else
            let msg = result |> generateFailurePrompt
            querySafeString  msg
            
    queryPrompt |> querySafeString
    
let private confirm queryPrompt =
    maybe {
        return
            (queryPrompt)
            |> Question.Confirm
            |> prompt
    }
    |> toBool

    
type Query (printer: #IPrinter) =
    let printEmptyLine () = printer.PrintFn ""
    let handle result =
        printEmptyLine ()
        result
    
    member __.QueriedAsString defaultValue queryPrompt =
        queriedAsString defaultValue queryPrompt
        |> handle
        
    member __.QueriedAsSelectList items query =
        queriedAsSelectList items query
        |> handle
        
    member __.ConfirmList items query =
        confirmList items query
        |> handle
        
    member __.QuerySafeString defaultValue predicate generateFailurePrompt queryPrompt =
        querySafeString defaultValue predicate generateFailurePrompt queryPrompt
        |> handle
        
    member __.Confirm queryPrompt =
        confirm queryPrompt