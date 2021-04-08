module ProjectSync.Lib.RepositoryConfiguration

open ProjectSync.Types
open ProjectSync.Lib.FileSystem

let repoConfigNameRaw = ".repositories"
let repoConfigName : Maybe<_> = repoConfigNameRaw |> Ok

let private getRepoFile (env: SyncEnvironment) =
    let syncDirectory = env.SyncDirectory
    
    repoConfigName
    |> env.JoinFD syncDirectory

let getConfiguredRepositoryNames env : _ mlist =
    let body =
        getRepoFile env
        |> maybeReadAllText
    match body with
    | Error _ -> Ok []
    | _ ->
        body
        |> msplit
        |> MaybeList.map trim
        |> MaybeList.sort
    
let writeNamesToRepoFile env names =
    let file = getRepoFile env
    let known =
        let known = getConfiguredRepositoryNames env
        match known with
        | Ok v -> Ok v
        | _ -> Ok []
        
    let unknown =
        names
        |> MaybeList.except known
        
    known
    |> MaybeList.append unknown
    |> MaybeList.sort
    |> mjoin
    |> maybeWriteAllText file
    
let removeRepositoryNamesFromFile env names =
    let file = getRepoFile env
    let known = getConfiguredRepositoryNames env
    let good =
        known
        |> MaybeList.except names
        
    good
    |> MaybeList.sort
    |> mjoin
    |> maybeWriteAllText file
    
let repoConfigExists (fs: #IFileSystemAccessor) syncLocation =
    repoConfigName
    |> fs.JoinF syncLocation
    |> maybeExists