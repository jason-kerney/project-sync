module ProjectSync.Lib.RepositoryConfiguration

open ProjectSync.Types
open Utils.Maybe
open Utils.FileSystem
open Utils.FileSystem.Helpers

let repoConfigNameRaw = ".repositories"
let repoConfigName : maybe<_> = repoConfigNameRaw |> Ok

let private getRepoFile (env: SyncEnvironment) =
    let syncDirectory = env.SyncDirectory
    
    repoConfigName
    |> env.MJoinFD syncDirectory

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
    |> fs.MJoinF syncLocation
    |> maybeExists