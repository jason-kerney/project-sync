module ProjectSync.App.Input

open ProjectSync.Types

open Utils.Maybe
open Utils.Maybe.Maybe
open Spectre.Console

let private maxItemsPerPage = 35

let private askWithDefault defaultValue prompt : 'T =
    AnsiConsole.Prompt(
            TextPrompt<'T>(prompt).DefaultValue(defaultValue)
        )
    
let private ask prompt : 'T =
    AnsiConsole.Ask<'T>(prompt)
    
let private askWithMultipleChoices prompt choices =
    let t = MultiSelectionPrompt<string>()
    t.Title <- prompt
    t.NotRequired () |> ignore
    t.PageSize <- 10
    t.MoreChoicesText <- "[grey](Move [green]up[/] and [green]down[/] to reveal more items)[/]"
    t.InstructionsText <- "[grey](Press [blue]<space>[/] to toggle an item, [green]<enter>[/] to accept)[/]"
    t.AddChoices(choices |> Seq.toArray) |> ignore
    
    AnsiConsole.Prompt(t)
    |> Seq.toList
    
let private queriedAsString defaultValue queryPrompt : string maybe =
    maybe {
        return 
            queryPrompt
            |> askWithDefault defaultValue
    }
    
let private queriedAsSelectList (items: _ seq) query =
    maybe {
        return askWithMultipleChoices query items
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
            AnsiConsole.Confirm query
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
            queryPrompt
            |> AnsiConsole.Confirm
    }
    |> toBool

    
type Query (printer: IPrinter) =
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