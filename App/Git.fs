module ProjectSync.App.Git

open ProjectSync.Types
open System.Diagnostics
open System.IO
open Utils.Maybe

let private start (workingDir: string) command args =
    let getOutPut (curProcess: #StreamReader) acc =
        let rec getOutPut (curProcess: #StreamReader) (acc: string list) =
                if curProcess.EndOfStream then
                    acc |> List.rev
                else
                    (curProcess.ReadLine ()) :: acc
                    |> getOutPut curProcess
            
        getOutPut curProcess acc
    
    let rec getStandardOutput (curProcess: Process) acc =
        getOutPut curProcess.StandardOutput acc
            
    let rec getStandardError (curProcess: Process) acc =
        getOutPut curProcess.StandardError acc
            
    let getDisplay proc =
            []
            |> getStandardOutput proc 
            |> getStandardError proc
     
    async {
        use curProcess = new Process ()
        let processInfo = ProcessStartInfo (command, args)
        processInfo.UseShellExecute <- false
        processInfo.RedirectStandardOutput <- true
        processInfo.RedirectStandardError <- true
        processInfo.CreateNoWindow <- true
        processInfo.WorkingDirectory <- workingDir
        curProcess.StartInfo <- processInfo
        return
             maybe {
                let id = sprintf "%s %s" command args
                curProcess.Start () |> ignore
                
                return
                    id,
                    getDisplay curProcess
                    |> join
            }
    }
        
let private gitNow getArgs rootDir repositoryName =
    let target = repositoryName |> sprintf "%s%c%s" rootDir Path.DirectorySeparatorChar
    
    maybe {
        let! args = repositoryName |> getArgs
        return!
            start target "git" args
            |> Async.RunSynchronously
    }
    
let private gitNowResult getArgs rootDir repository =
    maybe {
        let! (_, result) =
            gitNow getArgs rootDir repository
            
        return result
    }
    
let private fetch (printer: IPrinter) rootDir (repository: RepositoryInformation) =
    let target = repository.Name |> sprintf "%s%c%s" rootDir Path.DirectorySeparatorChar
    
    printer.PrintFn "about to fetch %s @ %s" repository.Name target
    
    start target "git" "fetch"
    
let private clone (printer: IPrinter) rootDir (repository: RepositoryInformation) =
    let operations = sprintf "clone %s" repository.Uri
    
    printer.PrintFn "about to %s @ %s" operations rootDir
    
    start rootDir "git" operations
    
let private printResults (printer: IPrinter) (asyncResults: (string * string) maybe Async list) =
    let divider = "\n-------------------------\n"
    let results =
        asyncResults
        |> Async.Parallel
        |> Async.RunSynchronously
        
    results
    |> Array.map (fun r ->
        match r with
        | Error e -> "", e |> Error |> sprintf "%A"
        | Ok v -> v
    )
    |> Array.filter (fun (_, v) -> 0 < v.Length)
    |> Array.map (fun (id, v) -> v |> sprintf "%s %s" id |> trim)
    |> joinBys divider
        |> printer.PrintFn "%s%s" divider
    
        
let private localBranch rootDir repositoryName =
    gitNowResult (fun _ -> Ok "rev-parse --abbrev-ref HEAD") rootDir repositoryName
    
let getBranchName format rootDir repositoryName =
    maybe {
        let! branchName = localBranch rootDir repositoryName
        return
            sprintf format branchName
    }
    
let private originShaw rootDir repositoryName =
    let getArgs _ = getBranchName "rev-parse origin/%s" rootDir repositoryName
    gitNowResult getArgs rootDir repositoryName
    
let private localShaw rootDir repositoryName =
    let getArgs _ = getBranchName "rev-parse %s" rootDir repositoryName
    gitNowResult getArgs rootDir repositoryName
    
let private baseShaw rootDir repositoryName =
    let getArgs _ = getBranchName "merge-base HEAD origin/%s" rootDir repositoryName
    gitNowResult getArgs rootDir repositoryName
    
let private hasUncommittedChanges rootDir repositoryName =
    let getArgs _ = Ok "status -s"
    
    repositoryName
    |> gitNowResult getArgs rootDir
    |> Possibly.check (fun s -> 0 < s.Length)
    
let needsAttention rootDir repositoryName =
    let result =
        maybe {
            let! originShaw = originShaw rootDir repositoryName
            let! localShaw = localShaw rootDir repositoryName
            let! baseShaw = baseShaw rootDir repositoryName
            let hasChanges = hasUncommittedChanges rootDir repositoryName
            
            return
                if hasChanges then repositoryName |> UncommittedChanges
                elif originShaw = localShaw then NoAction
                elif localShaw = baseShaw then repositoryName |> Pull
                elif originShaw = baseShaw then repositoryName |> Push 
                else repositoryName |> Merge
        }
    
    match result with
    | Ok v -> v
    | _ -> NoAction
        
let hasChanges rootDir repositoryName =
    let need = needsAttention rootDir repositoryName
    not <| (need = NoAction)
    
let updateRepositories (printer: IPrinter) rootDir (repositories: RepositoryInformation mlist) =
    maybe {
        let! repositories = repositories
        let! rootDir = rootDir
        
        repositories
        |> List.map (fun r ->
            if r.Existence = ExistsOnDisk then fetch printer rootDir r
            else clone printer rootDir r
        )
        |> printResults printer
    }