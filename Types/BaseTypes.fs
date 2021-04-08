namespace ProjectSync.Types

type SyncFailure =
    | ExceptionFailure of exn
    | GeneralFailure of string
    | CombinationError of SyncFailure list

type Maybe<'Value> = Result<'Value, SyncFailure>
type mlist<'Value> = Maybe<'Value list>
type maybe<'Value> = Maybe<'Value>

[<AutoOpen>]
module BaseHelpers =
    let isValidString (item: Maybe<string>) =
        match item with
        | Ok v ->
            0 < v.Length
        | Error _ -> false
        
    let asGeneralFailure failure: Maybe<_> = failure |> GeneralFailure |> Error
    let asExceptionFailure failure: Maybe<_> = failure |> ExceptionFailure |> Error
    let asCombinationFailure failures: Maybe<_> =
        failures |> CombinationError |> Error
    let asFailureCombinedWith failure2 failure1: Maybe<_> =
        let error =
            match failure1, failure2 with
            | CombinationError failures1, CombinationError failures2 ->
                failures1
                |> List.append failures2
                |> CombinationError
            | CombinationError failures, e ->
                e::failures |> CombinationError
            | e, CombinationError failures ->
                e::failures |> CombinationError
            | e1, e2 ->
                e1::e2::[] |> CombinationError
            
        error |> Error
        
    let combineWith failure1 failure2 =
        match failure1, failure2 with
        | CombinationError f1, CombinationError f2 ->
            List.concat [f1; f2] |> asCombinationFailure
        | CombinationError f1, f2 ->
            f2::f1 |> asCombinationFailure
        | f1, CombinationError f2 ->
            f1::f2 |> asCombinationFailure
        | f1, f2 ->
            [f1; f2] |> asCombinationFailure