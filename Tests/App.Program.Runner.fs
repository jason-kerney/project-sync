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
        member __.FullName with get () = $"file.full({path})"
        member __.Exists with get () = true
        member __.Delete () = "FakeFile.Delete" |> NotImplementedException |> raise
        member __.Path with get () = "FakeFile.Path" |> NotImplementedException |> raise
        member __.ReadAllText () =
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
        member __.WriteAllText text =
            match text with
            | Ok _ -> Ok ()
            | Error e -> Error e
            
let getFakeFile path = path |> FakeFile :> IFileWrapper

type FakeDir (path: string) =
    interface IDirectoryWrapper with
        member __.FullName with get () = $"dir.full({path})"
        member __.Exists with get () = true
        member __.Delete () = "FakeDir.Delete" |> NotImplementedException |> raise
        member __.Name with get () = path
        member __.GetFiles filter = $"FakeDir.GetFiles {filter}" |> NotImplementedException |> raise
        member __.GetFiles () = "FakeDir.GetFiles ()" |> NotImplementedException |> raise
        
        member __.GetDirectories () = "FakeDir.GetDirectories" |> NotImplementedException |> raise
        member __.Parent with get () = "FakeDir.Parent" |> NotImplementedException |> raise
        
let getFakeDir path = (path) |> FakeDir :> IDirectoryWrapper

type FakeFileSystem () =
    interface IFileSystemAccessor with
        member __.FullFilePath path = path
        member __.File path = path |> getFakeFile
        member __.MFile path = path |> lift getFakeFile
        
        member __.FullDirectoryPath path = path
        member __.Directory path = path |> getFakeDir
        member __.MDirectory path =
            match path with
            | Ok p -> p |> getFakeDir |> Ok
            | Error e -> Error e
            
        member __.JoinFilePath path fileName = $"%s{path}/%s{fileName}"
        member this.MJoinFilePath path fileName =
            let accessor = this :> IFileSystemAccessor
            fileName
            /-> (lift accessor.JoinFilePath) path
            
        member __.JoinF path fileName =
            $"%s{path}/%s{fileName}" |> getFakeFile
            
        member this.MJoinF path fileName =
            let accessor = this :> IFileSystemAccessor
            match path, fileName with
            | Ok path, Ok fileName -> fileName |> accessor.JoinF path |> Ok
            | Error e, Ok _
            | Ok _, Error e -> Error e
            | Error e1, Error e2 -> e1 |> combineWith e2
            
        member __.JoinFD directory fileName =
            $"%s{directory.FullName}/%s{fileName}" |> getFakeFile
            
        member this.MJoinFD (directory: maybe<IDirectoryWrapper>) fileName =
            let accessor = this :> IFileSystemAccessor
            match directory, fileName with
            | Ok directory, Ok fileName -> fileName |> accessor.JoinFD directory |> Ok
            | Error e, Ok _
            | Ok _, Error e -> Error e
            | Error e1, Error e2 -> e1 |> combineWith e2
            
        member __.JoinD root childFolder =
            childFolder |> sprintf "%s/%s" root |> getFakeDir
            
        member __.JoinDirectoryPath root childFolder =
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
    interface IConfigQuery with
        member __.QueryIdLocation defaultLocation = "home/idFile" |> Ok
        member __.QuerySyncLocation defaultLocation = "home/repoFile" |> Ok
        member __.QueryTokenName () = "ATokenName" |> Ok
        member __.QueryTokenValue () = "APassword" |> Ok
        member __.QueryCompany () = "ACompany" |> Ok
        member __.QueryProject () = "AProject" |> Ok
        member __.QueryNewProjects values = values |> Ok
        member __.QueryRemoveProjects values = values |> Ok
        member __.QueryAddFilter value = value |> Ok
        member __.QueryRemoveFilter value = value |> Ok
        member __.QueryInitRepositories () = false
        
let getFakeQuery () = FakeQuery () :> IConfigQuery

type FakeVersionGetter () =
    interface IVersionRetriever with
        member __.GetVersion () = VersionInfo ()
        
let getFakeVersion () = FakeVersionGetter ()  :> IVersionRetriever

[<Test>][<Ignore("Runs to long")>]
let ``Build all possible environments`` () =
    let runner = Runner (getPrinter (), getFakeFileSystem (), getFakeQuery (), getFakeVersion ())
    let results =
        SampleData.allRuntimeArgs
        |> List.filter (fun (_, r) -> match r with | Run _ -> true | _ -> false)
        |> List.map (snd >> (fun (Run args) -> $"{args.ToSimplifiedString ()} {(runner.GetEnvironment args).ToSimplifiedString ()}"))
        
    Approvals.VerifyAll(results, "Args")