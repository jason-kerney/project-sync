module ProjectSync.Tests.App.``Program Runner should``

open System
open ApprovalTests
open ProjectSync.App.Program
open ProjectSync.App.Types
open ProjectSync.App.Arguments
open ProjectSync.Lib
open ProjectSync.Lib.Printer
open ProjectSync.Tests
open ProjectSync.Types
open WhatsYourVersion
open NUnit.Framework
open Utils.FileSystem
open Utils.Maybe
open Utils.Maybe.Maybe

type FakeFile (path: string) =
    interface IFileWrapper with
        member _.FullName with get () = $"file.full({path})"
        member _.Exists with get () = true
        member _.Delete () = "FakeFile.Delete" |> NotImplementedException |> raise
        member _.Path with get () = "FakeFile.Path" |> NotImplementedException |> raise
        member _.ReadAllText () =
            if path.Contains (RepositoryConfiguration.repoConfigNameRaw) then
                [
                    $"{path}_Repository_1"
                    $"{path}_Repository_2"
                    $"{path}_Repository_3"
                ]
                |> join
                |> Ok
            elif path.Contains(EnvironmentBuilder.idFilenameRaw) then
                [
                    "# This file is used to configure application connection to Azure"
                    $"company:{path}_Fake_Company_Yah"
                    $"project:{path}_A_Really_Cool_ProjectName"
                    $"tokenName:{path}_ApplicationUser"
                    "# Expires 2999-12-25 @ 2PM EST"
                    $"token:{path}_ApplicationPassword"
                ]
                |> join
                |> Ok
            else "File doesn't exist ha ha ha" |> asGeneralFailure
        member _.WriteAllText text =
            match text with
            | Ok _ -> Ok ()
            | Error e -> Error e
            
let getFakeFile path = path |> FakeFile :> IFileWrapper

type FakeDir (path: string) =
    interface IDirectoryWrapper with
        member _.FullName with get () = $"dir.full({path})"
        member _.Exists with get () = true
        member _.Delete () = "FakeDir.Delete" |> NotImplementedException |> raise
        member _.Name with get () = path
        member _.GetFiles filter = $"FakeDir.GetFiles {filter}" |> NotImplementedException |> raise
        member _.GetFiles () = "FakeDir.GetFiles ()" |> NotImplementedException |> raise
        
        member _.GetDirectories () = "FakeDir.GetDirectories" |> NotImplementedException |> raise
        member _.Parent with get () = "FakeDir.Parent" |> NotImplementedException |> raise
        
let getFakeDir path = (path) |> FakeDir :> IDirectoryWrapper

type FakeFileSystem () =
    interface IFileSystemAccessor with
        member _.FullFilePath path = path
        member _.File path = path |> getFakeFile
        member _.MFile path = path |> lift getFakeFile
        
        member _.FullDirectoryPath path = path
        member _.Directory path = path |> getFakeDir
        member _.MDirectory path =
            match path with
            | Ok p -> p |> getFakeDir |> Ok
            | Error e -> Error e
            
        member _.JoinFilePath path fileName = $"%s{path}/%s{fileName}"
        member this.MJoinFilePath path fileName =
            let accessor = this :> IFileSystemAccessor
            fileName
            /-> (lift accessor.JoinFilePath) path
            
        member _.JoinF path fileName =
            $"%s{path}/%s{fileName}" |> getFakeFile
            
        member this.MJoinF path fileName =
            let accessor = this :> IFileSystemAccessor
            match path, fileName with
            | Ok path, Ok fileName -> fileName |> accessor.JoinF path |> Ok
            | Error e, Ok _
            | Ok _, Error e -> Error e
            | Error e1, Error e2 -> e1 |> combineWith e2
            
        member _.JoinFD directory fileName =
            $"%s{directory.FullName}/%s{fileName}" |> getFakeFile
            
        member this.MJoinFD (directory: maybe<IDirectoryWrapper>) fileName =
            let accessor = this :> IFileSystemAccessor
            match directory, fileName with
            | Ok directory, Ok fileName -> fileName |> accessor.JoinFD directory |> Ok
            | Error e, Ok _
            | Ok _, Error e -> Error e
            | Error e1, Error e2 -> e1 |> combineWith e2
            
        member _.JoinD root childFolder =
            childFolder |> sprintf "%s/%s" root |> getFakeDir
            
        member _.JoinDirectoryPath root childFolder =
            sprintf $"%s{root}/%s{childFolder}"
            
        member this.MJoinDirectoryPath root childFolder =
            let accessor = this :> IFileSystemAccessor
            match root, childFolder with
            | Ok root, Ok childFolder -> childFolder |> accessor.JoinDirectoryPath root |> Ok
            | Error e, Ok _
            | Ok _, Error e -> Error e
            | Error e1, Error e2 -> e1 |> combineWith e2
            
        member this.MJoinD root childFolder =
            match root, childFolder with
            | Ok p, Ok folder -> folder |> (this :> IFileSystemAccessor).JoinD p |> Ok
            | Error e, Ok _
            | Ok _, Error e -> Error e
            | Error e1, Error e2 -> e1 |> combineWith e2
            
let getFakeFileSystem () = FakeFileSystem () :> IFileSystemAccessor

type FakeQuery () =
    interface IServiceIdConfigQuery with
        member _.QueryIdLocation defaultLocation = "home/idFile" |> Ok
    
    interface IAzureConfigQuery with
        member _.QueryTokenName () = "ATokenName" |> Ok
        member _.QueryTokenValue () = "APassword" |> Ok
        member _.QueryCompany () = "ACompany" |> Ok
        member _.QueryProject () = "AProject" |> Ok
        
    interface IRepositoryConfigQuery with
        member _.QuerySyncLocation defaultLocation = "home/repoFile" |> Ok
        member _.QueryNewRepositories values = values |> Ok
        member _.QueryRemoveRepositories values = values |> Ok
        member _.QueryAddFilter value = value |> Ok
        member _.QueryRemoveFilter value = value |> Ok
        member _.QueryInitRepositories () = false

    member this.ServiceIdConfigQuery
        with get () = this :> IServiceIdConfigQuery
    member this.AzureConfigQuery
         with get () = this :> IAzureConfigQuery
         
    member this.RepositoryConfigQuery
        with get () = this :> IRepositoryConfigQuery
        
let getFakeQuery () = FakeQuery ()

type FakeVersionGetter () =
    interface IVersionRetriever with
        member _.GetVersion () = VersionInfo ()
        
let getFakeVersion () = FakeVersionGetter ()  :> IVersionRetriever

[<Test>][<Ignore("Runs to long")>]
let ``Build all possible environments`` () =
    let fakeQuery = getFakeQuery ()
    let runner = Runner (getPrinter (), getFakeFileSystem (), fakeQuery.ServiceIdConfigQuery,fakeQuery.AzureConfigQuery, fakeQuery.RepositoryConfigQuery, getFakeVersion ())
    let results =
        SampleData.allRuntimeArgs
        |> List.filter (fun (_, r) -> match r with | Run _ -> true | _ -> false)
        |> List.map (snd >> (fun (Run args) -> $"{args.ToSimplifiedString ()} {(runner.GetEnvironment args).ToSimplifiedString ()}"))
        
    Approvals.VerifyAll(results, "Args")