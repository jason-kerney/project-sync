[<AutoOpen>]
module ProjectSync.App.Types

open Utils.Maybe
    
type GitAttention =
    | NoAction
    | UncommittedChanges of string
    | Pull of string
    | Push of string
    | Merge of string
    
type GitHubUserType =
    | Org
    | User
    
type IServiceIdConfigQuery =
    abstract member QueryIdLocation : defaultLocation:string maybe -> string maybe    
    
type IAzureConfigQuery = 
    abstract member QueryTokenName : unit -> string maybe
    abstract member QueryTokenValue : unit -> string maybe
    abstract member QueryCompany : unit -> string maybe
    abstract member QueryProject : unit -> string maybe

type IRepositoryConfigQuery = 
    abstract member QuerySyncLocation : defaultLocation:string maybe -> string maybe
    abstract member QueryNewRepositories : string list -> string mlist
    abstract member QueryRemoveRepositories : string list -> string mlist
    abstract member QueryAddFilter : string -> string maybe
    abstract member QueryRemoveFilter : string -> string maybe
    abstract member QueryInitRepositories : unit -> bool

let isOk (thing: _ maybe) =
    match thing with
    | Ok _ -> true
    | _ -> false
    
let hasValue (thing: string maybe) =
    match thing with
    | Ok v -> 0 < v.Length
    | _ -> false

let prefer (preferred: _ maybe) (other: _ maybe) : _ maybe =
    match preferred, other with
    | Ok value, _
    | _, Ok value -> Ok value
    | error, _ -> error
