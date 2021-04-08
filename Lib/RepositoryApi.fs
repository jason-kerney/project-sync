module ProjectSync.Lib.RepositoryApi

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Text.Json
open System.Text.Json.Serialization
open ProjectSync.Types

type ApiRepository = {
    [<JsonPropertyName("name")>]
    Name: string
//    [<JsonPropertyName("remoteUrl")>]
//    RemoteUrl: string
}

type ApiResult = {
    [<JsonPropertyName("value")>]
    Value: ApiRepository seq
}

let private getRepositories (result : ApiResult) =
    result.Value
    |> List.ofSeq
    |> List.map (fun r -> r.Name)
    |> List.sort

type RepositoryConnection (env: SyncEnvironment) =
    member __.GetRepositoryNames () =
        use client = new HttpClient ()
    
        maybe {
            let! company = env.Company
            let! project = env.Project
            let! token = env.AuthToken
            
            let uri =
                sprintf $"https://dev.azure.com/{company}/{project}/_apis/git/"
                |> Uri
                
            client.BaseAddress <- uri
            client.DefaultRequestHeaders.Accept.Clear ()
            client.DefaultRequestHeaders.Accept.Add (MediaTypeWithQualityHeaderValue "application/json")
            client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Basic", token)
            
            return
                "repositories?api-version=6.0"
                |> client.GetStringAsync 
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> JsonSerializer.Deserialize<ApiResult>
                |> getRepositories
        }
        
let getRepositoriesFromApi env =
    let connection = RepositoryConnection (env)
    connection.GetRepositoryNames ()
    
let getRepositoriesFromApiExcept configured env =
    let api = getRepositoriesFromApi env
    api
    |> MaybeList.except configured