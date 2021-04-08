module ProjectSync.Lib.RepositoryBuilder

open ProjectSync.Types

let buildRepository (env: SyncEnvironment) configuration name =
    maybe {
        let! syncLocation = env.SyncLocation
        let! company = env.Company
        let! project = env.Project
        
        let exists =
            name
            |> env.JoinD syncLocation
            |> FileSystem.exists
            
        let existence =
            if exists
            then Existence.ExistsOnDisk
            else Existence.NewOnDisk
        
        return
            {
                Name = name
                Uri =  $"https://{company}@dev.azure.com/{company}/{project}/_git/{name}"
                Configuration = configuration
                Existence = existence
            }
    }
    
let buildRepositories env configuration names : _ mlist =
    names
    |> MaybeList.map (buildRepository env configuration)
    |> MaybeList.simplify
    