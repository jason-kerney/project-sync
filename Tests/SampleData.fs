module ProjectSync.Tests.SampleData

open ProjectSync.App
open FSharp.Collections.ParallelSeq
    
let getStringArguments (value: (string * string) option) =
    match value with
    | Some (switch, v) -> [|switch; v|]
    | _ -> [||]
    
let getBoolArguments (value: (string * bool) option) =
    match value with
    | Some (switch, _) -> [|switch|]
    | _ -> [||]
    
let combineWith values switches  =
    let combined = 
        seq {
            for switch in switches do
                for value in values do
                    switch, value
        }
        |> Seq.map Some
        |> Seq.toList
    
    None::combined
    
let allArguments =
    let syncPathArgs = ["--target-path"; "--sync-location"; "-tp"] |> combineWith ["my_repo_path"]
    let idPathArgs = ["--azure-id-config-path"; "-idp"] |> combineWith ["my_id_path"]
    let companyArgs = ["--company-name"; "-c"] |> combineWith ["my_company_here"]
    let projectArgs = ["--project-name"; "-p"] |> combineWith ["a_project"]
    let tokenNameArgs = ["--token-name"; "-u"] |> combineWith ["user"]
    let tokenValueArgs = ["--token-value"; "-pwd"] |> combineWith ["password"]
    let shouldAddArgs = ["--should-add-new"; "-add"] |> combineWith [true]
    let insertArgs = ["--insert-matching"; "-ins"] |> combineWith ["insert_filter"]
    let shouldDelArgs = ["--should-delete"; "-del"] |> combineWith [true]
    let removeArgs = ["--remove-matching"; "-rem"] |> combineWith ["remove_filter"]
    let versionArgs = ["--version"; "-v"] |> combineWith [true]
    
    seq {
        for syncPathArg in syncPathArgs do
            for idPathArg in idPathArgs do
                for companyArg in companyArgs do
                    for projectArg in projectArgs do
                        for tokenNameArg in tokenNameArgs do
                            for tokenValueArg in tokenValueArgs do
                                for shouldAddArg in shouldAddArgs do
                                    for insertArg in insertArgs do
                                        for shouldDelArg in shouldDelArgs do
                                            for removeArg in removeArgs do
                                                for versionArg in versionArgs do
                                                    yield
                                                        [|
                                                            syncPathArg |> getStringArguments
                                                            idPathArg |> getStringArguments
                                                            companyArg |> getStringArguments
                                                            projectArg |> getStringArguments
                                                            tokenNameArg |> getStringArguments
                                                            tokenValueArg |> getStringArguments
                                                            shouldAddArg |> getBoolArguments
                                                            insertArg |> getStringArguments
                                                            shouldDelArg |> getBoolArguments
                                                            removeArg |> getStringArguments
                                                            versionArg |> getBoolArguments
                                                        |]
                                                        |> Array.concat
    }
    |> List.ofSeq
    
let processAllArguments processor =
    allArguments
    |> PSeq.map processor
    |> PSeq.toList
    
let allRuntimeArgs =
    processAllArguments (fun args -> args, Arguments.parse args)
    |> List.sortBy fst

