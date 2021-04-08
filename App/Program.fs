module ProjectSync.App.Program

open System.Text.RegularExpressions
open ProjectSync.App.Arguments
open ProjectSync.Lib
open ProjectSync.Types
open WhatsYourVersion

type Runner (printer : IPrinter, filesSystem: IFileSystemAccessor, query: IConfigQuery, versionGetter: IVersionRetriever) =
    let configHelper = ConfigValues.ConfigurationHelper (printer, filesSystem, query)
    
    let safeDelete (printer: IPrinter) (fs: IFileSystemAccessor) (root: string) repository =
        let location = repository.Name |> fs.JoinD root
        let hasChanges = repository.Name |> Git.hasChanges root
        
        if hasChanges then
            async {
                return
                    maybe {
                        return printer.PrintFn "Changes exist: %s" location.FullName
                    }
            }
        else location.Delete ()

    
    member this.Run (args: string []) =
        let r = parse args
        match r with
        | ShowHelp help -> this.Run help
        | ShowVersion ->
            let info = versionGetter.GetVersion()
            sprintf "Version %s built at %s" info.Version info.BuildDateUtc
            |> this.Run 
        | Run args -> this.Run args
        
    member this.GetEnvironment (arguments: RunTimeArguments) = 
        let syncLocation = configHelper.GetSyncLocation arguments.Add arguments.SyncLocation
        
        let idLocation = (syncLocation |> prefer arguments.IdLocation)
        
        let env =
            configHelper.GetEnvironment
            <| idLocation
            <| syncLocation
            <| arguments.Company
            <| arguments.Project
            <| arguments.TokenName
            <| arguments.Token
            
        this.WriteIdFile env idLocation 
        
        env
    member __.WriteIdFile (env: SyncEnvironment) idLocation =
        let idFileExists = idLocation |> EnvironmentBuilder.idFileExists env
        
        if idFileExists |> not then 
            EnvironmentBuilder.writeEnvironmentFile env idLocation
            |> ignore
            
    member __.GetAdd (env: SyncEnvironment) configured filter =
        let filter = filter |> maybeOrDefault "."
        let filter = query.QueryAddFilter filter
        let isMatch =
            match filter with
            | Ok value ->
                let r = Regex value
                r.IsMatch
            | _ -> fun _ -> true
            
        let apiRepos =
            env
            |> RepositoryApi.getRepositoriesFromApiExcept configured
            |> MaybeList.filter isMatch
            
        maybe {
            let! apiRepos = apiRepos
            return! query.QueryNewProjects apiRepos
        }
        
    member __.GetDelete configured filter =
        let filter = filter |> maybeOrDefault "."
        let filter = query.QueryRemoveFilter filter
        let isMatch =
            match filter with
            | Ok value ->
                let r = Regex value
                r.IsMatch
            | _ -> fun _ -> true
            
        let removedRepos =
            configured
            |> MaybeList.filter isMatch
            |> maybeOrDefault []
            
        query.QueryRemoveProjects removedRepos
        
    member this.Run (arguments: RunTimeArguments) =
        let env = this.GetEnvironment arguments
        
        let configuredNames = RepositoryConfiguration.getConfiguredRepositoryNames env
        
        let addReposNames =
            if arguments.Add
            then this.GetAdd env configuredNames arguments.InsertFilter
            else [] |> Ok
            
        let deleteReposNames =
            if arguments.Remove
            then this.GetDelete configuredNames arguments.RemoveFilter
            else [] |> Ok
            
        let updateReposNames =
            configuredNames
            |> MaybeList.except deleteReposNames
            
        let addRepos =
            addReposNames
            |> RepositoryBuilder.buildRepositories env NotConfigured
            
        let deleteRepos =
            deleteReposNames
            |> RepositoryBuilder.buildRepositories env Configured
            
        let updateRepos =
            updateReposNames
            |> RepositoryBuilder.buildRepositories env Configured
            |> MaybeList.append addRepos
           
        let removeResult =
            deleteReposNames
            |> RepositoryConfiguration.removeRepositoryNamesFromFile env
            
        let deleteResult =
            maybe {
                let! deleteRepos = deleteRepos
                let! location = env.SyncLocation
                
                return!
                    deleteRepos
                    |> List.map (safeDelete env env location)
                    |> Async.Parallel
                    |> Async.RunSynchronously
                    |> MaybeList.reduceErrors
            }
            
        let addResult =
            addReposNames
            |> RepositoryConfiguration.writeNamesToRepoFile env
            
        let updateResult = 
            updateRepos
            |> Git.updateRepositories printer env.SyncLocation
                    
        let result =
            [
                removeResult
                deleteResult
                addResult
                updateResult
            ]
            |> MaybeList.reduceErrors
            
        maybe {
            let! updateRepos = updateRepos
            let! location = env.SyncLocation
            
            let updateRepos =
                updateRepos
                |> List.map (fun repo ->
                    repo.Name |> Git.needsAttention location
                )
                |> List.filter (fun att -> not <| (att = NoAction))
            
            if 0 < updateRepos.Length then
                printer.PrintFn "\n\n"
                printer.PrintFn "========================================================"
                
            updateRepos
            |> List.iter (printer.PrintFn ">\t%A")
            
            if 0 < updateRepos.Length then
                printer.PrintFn "========================================================"
        } |> ignore
            
        match result with
        | Ok _ ->
            env.PrintFn "Done!"
            0
        | _ ->
            env.PrintFn "\n%A" result
            -1
        
    member __.Run msg =
        printer.PrintFn "%s" msg
        0
        

[<EntryPoint>]
let main argv =
    let printer = Printer.getPrinter ()
    let fileSystem = FileSystem.getFileSystem printer
    let configQuery = ConfigValues.Helper.getConfigQuery printer fileSystem
    
    let assemblyWrapper = AssemblyWrapper.From<IConfigQuery>()
    let versionGetter = VersionRetriever (assemblyWrapper)
    
    let runner = Runner (printer, fileSystem, configQuery, versionGetter)
    
    runner.Run argv
