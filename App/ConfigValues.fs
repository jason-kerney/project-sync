module ProjectSync.App.ConfigValues

open ProjectSync.Lib
open ProjectSync.Types
open Utils.Maybe
open Utils.Maybe.Maybe
open Utils.FileSystem
open Utils.FileSystem.Helpers.FileSystem

let private getCheckedValue predicate query value =
    if value |> predicate then value
    else query ()
        
let private getValue query value =
    getCheckedValue isOk query value

let private getToken (configQuery: #IAzureConfigQuery) authToken =
    if authToken |> isOk then authToken
    else
        let tokenName = configQuery.QueryTokenName ()
        let tokenValue =
            if tokenName |> hasValue then
                configQuery.QueryTokenValue ()
            else "" |> Ok
            
        if tokenName |> hasValue && tokenValue |> hasValue then
            tokenValue |> EnvironmentBuilder.getAuthValue tokenName
        else "" |> Ok
        
let private generateDirectoryFailurePrompt (fs: IFileSystemAccessor) result =
    match result with
    | Ok v ->
        v
        |> fs.Directory
        |> getFullName
        |> sprintf "%A does not have the ident file. What is the location of your azure identity file?"
    | Error e -> e |> sprintf "There was an error (%A). What is the location of your azure identity file?"

type private ServiceIdConfigQuery (printer: IPrinter, fs: IFileSystemAccessor) =
    let inputQuery = Input.Query printer
    interface IServiceIdConfigQuery with
        member _.QueryIdLocation defaultLocation =
            let defaultLocation =
                let t = defaultLocation |> fs.MDirectory |> maybeGetFullName
                match t with
                | Ok v -> v
                | _ -> "." |> fs.Directory |> getFullName
                
            if defaultLocation |> Ok |> EnvironmentBuilder.idFileExists fs then defaultLocation |> Ok
            else
                let predicate result = result |> EnvironmentBuilder.idFileExists fs
                  
                "What is the location of your azure identity file?"
                |> inputQuery.QuerySafeString defaultLocation predicate (generateDirectoryFailurePrompt fs)

type private AzureConfigQuery (printer: IPrinter) =
    let inputQuery = Input.Query printer
    let queryUnknownSafeString prompt =
        let defaultValue = System.String.Empty
        let predicate = isOk
        let generateFailurePrompt _result = $"There was a problem. {prompt}"
        
        prompt
        |> inputQuery.QuerySafeString defaultValue predicate generateFailurePrompt
        
    interface IAzureConfigQuery with
        member _.QueryTokenName () =
            "What is the name of the access token to use? (empty to add id file path)"
            |> queryUnknownSafeString
            
        member _.QueryTokenValue () =
            "What is the value of the generated auth token? (empty to add id file path)"
            |> queryUnknownSafeString
            
        member _.QueryCompany () =
            "What is the company name used in azure?"
            |> queryUnknownSafeString
            
        member _.QueryProject () =
            "What is the name of azure project that houses your repositories?"
            |> queryUnknownSafeString
            
type private RepositoryConfigQuery (printer: IPrinter, fs: IFileSystemAccessor) =
    let inputQuery = Input.Query printer
        
    let querySafeList things confirmationQuery queryPrompt =
        let rec choose () =
            let chosen =
                queryPrompt
                |> inputQuery.QueriedAsSelectList things
                
            maybe {
                let! chosen = chosen
                let confirmed =
                    confirmationQuery
                    |> inputQuery.ConfirmList chosen
                    
                if confirmed then return chosen
                else return! choose ()
            }
            
        choose ()
    
    interface IRepositoryConfigQuery with                    
        member _.QuerySyncLocation defaultLocation =
            let defaultLocation =
                let t = defaultLocation |> orDefault  "."
                t |> fs.Directory |> getFullName
                
            if defaultLocation |> Ok |> RepositoryConfiguration.repoConfigExists fs then defaultLocation |> Ok
            else
                let predicate result = result |> RepositoryConfiguration.repoConfigExists fs
                    
                "What is the location of the repositories"
                |> inputQuery.QuerySafeString defaultLocation predicate (generateDirectoryFailurePrompt fs)

        member _.QueryNewRepositories repoNames =
            let confirmQuery =
                if 1 < repoNames.Length then "Synchronize these repositories?"
                else "Synchronize this repository?"
                
            "Which Repositories to Add?"
                |> querySafeList repoNames confirmQuery
            
        member _.QueryRemoveRepositories repoNames =
            let confirmQuery =
                if 1 < repoNames.Length then "Delete these repositories?"
                else "Delete this repository?"
                
            "Which Repositories to Remove?"
            |> querySafeList repoNames confirmQuery
        
        member _.QueryAddFilter filter =
            "How do you want to limit repository selection for addition (regex)?"
            |> inputQuery.QueriedAsString filter 
            
        member _.QueryRemoveFilter filter =
            "How do you want to limit repository selection for removal (regex)?"
            |> inputQuery.QueriedAsString filter
            
        member _.QueryInitRepositories () =
            "Do you wish to create a new repository config?"
            |> inputQuery.Confirm
            
type ConfigQuery (printer: IPrinter, fs: IFileSystemAccessor) =
    let idLocationConfigQuery = (printer, fs) |> ServiceIdConfigQuery :> IServiceIdConfigQuery
    let azureConfigQuery = printer |> AzureConfigQuery :> IAzureConfigQuery
    let repositoryConfigQuery = (printer, fs) |> RepositoryConfigQuery :> IRepositoryConfigQuery

    member this.ServiceIdConfigQuery with get () = idLocationConfigQuery
        
    member this.AzureConfigQuery with get() = azureConfigQuery
            
    member this.RepositoryConfigQuery with get () = repositoryConfigQuery

type ConfigurationHelper (printer: IPrinter, fs: IFileSystemAccessor, idLocationQuery: IServiceIdConfigQuery, azureQuery: IAzureConfigQuery, repositoryQuery: IRepositoryConfigQuery) =
    let getValueSafely query predicate value =
        if value |> predicate then
            value
        else query ()
    
    // repository
    member _.GetSyncLocation add syncLocation : _ maybe =
        let defaultLocation = "." |> Ok |> prefer syncLocation
        let getLocation () =
            let query () = defaultLocation |> repositoryQuery.QuerySyncLocation
            let predicate value = value |> RepositoryConfiguration.repoConfigExists fs
            syncLocation |> getValueSafely query predicate
            
        if defaultLocation |> RepositoryConfiguration.repoConfigExists fs then defaultLocation
        else
            if add then
                let init = repositoryQuery.QueryInitRepositories ()
                if init then defaultLocation
                else getLocation ()
            else  getLocation()
        
    // Azure
    member this.GetEnvironment idLocation syncLocation company project tokenName token =
        let idLocation = "." |> Ok |> prefer idLocation
        let rec getEnvironment idLocation =
            let authToken = token |> EnvironmentBuilder.getAuthValue tokenName
            let baseResult =
                if idLocation |> EnvironmentBuilder.idFileExists fs then
                    EnvironmentBuilder.getEnvironment printer fs idLocation syncLocation
                else
                    {
                        Printer = printer
                        FileSystem = fs
                        SyncLocation = syncLocation
                        AuthToken = authToken
                        Company = company
                        Project = project
                    }
                    
            let result =
                { baseResult with
                    AuthToken = baseResult.AuthToken |> prefer authToken |> getToken azureQuery
                    Company = baseResult.Company |> prefer company |> getValue azureQuery.QueryCompany
                    Project = baseResult.Project |> prefer project |> getValue azureQuery.QueryProject
                    SyncLocation = baseResult.SyncLocation |> prefer syncLocation
                }
            
            let checks =
                [result.AuthToken; result.Company; result.Project; result.SyncLocation]
                |> List.map hasValue
                |> List.fold (&&) true
                
            if checks then result
            else
                let idLocation = idLocationQuery.QueryIdLocation idLocation
                getEnvironment idLocation
                
        getEnvironment idLocation
        
let getConfigQuery printer fs =
    (printer, fs) |> ConfigQuery