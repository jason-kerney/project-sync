[<AutoOpen>]
module ProjectSync.Types.GeneralTypes

open ProjectSync.Types
open Utils.Maybe
open Utils.FileSystem

type Configuration =
    | Configured
    | NotConfigured
    | Unknown
    
type Existence =
    | ExistsOnDisk
    | NewOnDisk
    
type RepositoryInformation =
    {
        Name: string
        Uri : string
        Configuration: Configuration
        Existence: Existence
    }
    
type IPrinter =
    abstract member PrintF: Printf.TextWriterFormat<('a)> -> 'a
    abstract member PrintFn: Printf.TextWriterFormat<('a)> -> 'a
    
type SyncEnvironment =
    {
        Printer: IPrinter
        FileSystem: IFileSystemAccessor
        SyncLocation: Maybe<string>
        AuthToken: Maybe<string>
        Company: Maybe<string>
        Project: Maybe<string>
    }
    interface IFileSystemAccessor with
        member this.FullFilePath path = this.FileSystem.FullFilePath path
        member this.File path = this.FileSystem.File path
        member this.MFile path = this.FileSystem.MFile path
        
        member this.FullDirectoryPath path = this.FileSystem.FullDirectoryPath path
        member this.Directory path = this.FileSystem.Directory path
        member this.MDirectory path = this.FileSystem.MDirectory path
        
        member this.JoinFilePath path fileName = this.FileSystem.JoinFilePath path fileName
        member this.MJoinFilePath path fileName = this.FileSystem.MJoinFilePath path fileName
        
        member this.JoinF path fileName = this.FileSystem.JoinF path fileName
        member this.MJoinF path fileName = this.FileSystem.MJoinF path fileName
                
        member this.JoinFD directory fileName = this.FileSystem.JoinFD directory fileName
        member this.MJoinFD directory fileName = this.FileSystem.MJoinFD directory fileName
        
        member this.JoinDirectoryPath root childFolder = this.FileSystem.JoinDirectoryPath root childFolder
        member this.MJoinDirectoryPath root childFolder = this.FileSystem.MJoinDirectoryPath root childFolder
        
        member this.JoinD root childFolder = this.FileSystem.JoinD root childFolder
        member this.MJoinD root childFolder = this.FileSystem.MJoinD root childFolder
            
    interface IPrinter with
        member this.PrintF format = this.Printer.PrintF format
        member this.PrintFn format = this.Printer.PrintFn format
    
    (*File System*)
    member this.FullFilePath path = this.FileSystem.FullFilePath path
    member this.File path = this.FileSystem.File path
    member this.MFile path = this.FileSystem.MFile path
    
    member this.FullDirectoryPath path = this.FileSystem.FullDirectoryPath path
    member this.Directory path = this.FileSystem.Directory path
    member this.MDirectory path = this.FileSystem.MDirectory path
    
    member this.JoinFilePath path fileName = this.FileSystem.JoinFilePath path fileName
    member this.MJoinFilePath path fileName = this.FileSystem.MJoinFilePath path fileName
    
    member this.JoinF path fileName = this.FileSystem.JoinF path fileName
    member this.MJoinF path fileName = this.FileSystem.MJoinF path fileName
            
    member this.JoinFD directory fileName = this.FileSystem.JoinFD directory fileName
    member this.MJoinFD directory fileName = this.FileSystem.MJoinFD directory fileName
    
    member this.JoinDirectoryPath root childFolder = this.FileSystem.JoinDirectoryPath root childFolder
    member this.MJoinDirectoryPath root childFolder = this.FileSystem.MJoinDirectoryPath root childFolder
    
    member this.JoinD root childFolder = this.FileSystem.JoinD root childFolder
    member this.MJoinD root childFolder = this.FileSystem.MJoinD root childFolder

    (*Printer*)
    member this.PrintF format = this.Printer.PrintF format
    member this.PrintFn format = this.Printer.PrintFn format
    
    (*Original*)
    member this.SyncDirectory
        with get () : Maybe<_> =
            match this.SyncLocation with
            | Ok path ->
                path |> this.Directory |> Ok
            | Error e -> Error e
            
    member this.ToSimplifiedString () =
        let toString value = sprintf "%A" value
        [
            $"{nameof this.SyncLocation}: {this.SyncLocation |> toString}"
            $"{nameof this.Company}: {this.Company |> toString}"
            $"{nameof this.Project}: {this.Project |> toString}"
            $"{nameof this.AuthToken}: {this.AuthToken |> toString}"
        ]
        |> joinBys "; "
        |> sprintf "{%s}"
