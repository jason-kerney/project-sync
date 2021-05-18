namespace ProjectSync.App

module ConfigValues =
    open ProjectSync.Lib
    open ProjectSync.Types
    open Utils.Maybe
    open Utils.Maybe.Maybe
    open Utils.FileSystem
    open Utils.FileSystem.Helpers.FileSystem

    module Helper =
        open ProjectSync.App

        let getCheckedValue predicate query value =
            if value |> predicate then value
            else query ()
                
        let getValue query value =
            getCheckedValue isOk query value
            
        type private ConfigQuery (printer: IPrinter, fs: IFileSystemAccessor) =
            let inputQuery = Input.Query (printer)
            let queryUnknownSafeString prompt =
                let defaultValue = System.String.Empty
                let predicate = isOk
                let generateFailurePrompt _result = $"There was a problem. {prompt}"
                
                prompt
                |> inputQuery.QuerySafeString defaultValue predicate generateFailurePrompt
                
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

            let generateDirectoryFailurePrompt result =
                match result with
                | Ok v ->
                    v
                    |> fs.Directory
                    |> getFullName
                    |> sprintf "%A does not have the ident file. What is the location of your azure identity file?"
                | Error e -> e |> sprintf "There was an error (%A). What is the location of your azure identity file?"
                
            interface IConfigQuery with
                member __.QueryIdLocation defaultLocation =
                    let defaultLocation =
                        let t = defaultLocation |> fs.MDirectory |> maybeGetFullName
                        match t with
                        | Ok v -> v
                        | _ -> "." |> fs.Directory |> getFullName
                        
                    if defaultLocation |> Ok |> EnvironmentBuilder.idFileExists fs then defaultLocation |> Ok
                    else
                        let predicate result = result |> EnvironmentBuilder.idFileExists fs
                          
                        "What is the location of your azure identity file?"
                        |> inputQuery.QuerySafeString defaultLocation predicate generateDirectoryFailurePrompt
                    
                member __.QuerySyncLocation defaultLocation =
                    let defaultLocation =
                        let t = defaultLocation |> orDefault  "."
                        t |> fs.Directory |> getFullName
                        
                    if defaultLocation |> Ok |> RepositoryConfiguration.repoConfigExists fs then defaultLocation |> Ok
                    else
                        let predicate result = result |> RepositoryConfiguration.repoConfigExists fs
                            
                        "What is the location of the repositories"
                        |> inputQuery.QuerySafeString defaultLocation predicate generateDirectoryFailurePrompt
                    
                member __.QueryTokenName () =
                    "What is the name of the access token to use? (empty to add id file path)"
                    |> queryUnknownSafeString
                    
                member __.QueryTokenValue () =
                    "What is the value of the generated auth token? (empty to add id file path)"
                    |> queryUnknownSafeString
                    
                member __.QueryCompany () =
                    "What is the company name used in azure?"
                    |> queryUnknownSafeString
                    
                member __.QueryProject () =
                    "What is the name of azure project that houses your repositories?"
                    |> queryUnknownSafeString
                    
                member __.QueryNewProjects repoNames =
                    let confirmQuery =
                        if 1 < repoNames.Length then "Synchronize these repositories?"
                        else "Synchronize this repository?"
                        
                    "Which Repositories to Add?"
                        |> querySafeList repoNames confirmQuery
                    
                member __.QueryRemoveProjects repoNames =
                    let confirmQuery =
                        if 1 < repoNames.Length then "Delete these repositories?"
                        else "Delete this repository?"
                        
                    "Which Repositories to Remove?"
                    |> querySafeList repoNames confirmQuery
                
                member __.QueryAddFilter filter =
                    "How do you want to limit repository selection for addition (regex)?"
                    |> inputQuery.QueriedAsString filter 
                    
                member __.QueryRemoveFilter filter =
                    "How do you want to limit repository selection for removal (regex)?"
                    |> inputQuery.QueriedAsString filter
                    
                member __.QueryInitRepositories () =
                    "Do you wish to create a new repository config?"
                    |> inputQuery.Confirm

            member this.AsIConfigQuery with get () = this :> IConfigQuery
            
            
        let getConfigQuery printer fs =
            (printer, fs) |> ConfigQuery :> IConfigQuery
        
        let getToken (configQuery: #IConfigQuery) authToken =
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
            
    open Helper
    
    type ConfigurationHelper (printer: #IPrinter, fs: #IFileSystemAccessor, query: #IConfigQuery) =
        let getValueSafely query predicate value =
            if value |> predicate then
                value
            else query ()
        
        member __.GetSyncLocation add syncLocation : _ maybe =
            let defaultLocation = "." |> Ok |> prefer syncLocation
            let getLocation () =
                let query () = defaultLocation |> query.QuerySyncLocation
                let predicate value = value |> RepositoryConfiguration.repoConfigExists fs
                syncLocation |> getValueSafely query predicate
                
            if defaultLocation |> RepositoryConfiguration.repoConfigExists fs then defaultLocation
            else
                if add then
                    let init = query.QueryInitRepositories ()
                    if init then defaultLocation
                    else getLocation ()
                else  getLocation()
            
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
                        AuthToken = baseResult.AuthToken |> prefer authToken |> getToken query
                        Company = baseResult.Company |> prefer company |> getValue query.QueryCompany
                        Project = baseResult.Project |> prefer project |> getValue query.QueryProject
                        SyncLocation = baseResult.SyncLocation |> prefer syncLocation
                    }
                
                let checks =
                    [result.AuthToken; result.Company; result.Project; result.SyncLocation]
                    |> List.map (hasValue)
                    |> List.fold (&&) true
                    
                if checks then result
                else
                    let idLocation = query.QueryIdLocation idLocation
                    getEnvironment idLocation
                    
            getEnvironment idLocation