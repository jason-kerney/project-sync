[<AutoOpen>]
module ProjectSync.Types.GeneralTypes

open ProjectSync.Types
open Utils.Maybe
open Utils.Maybe.Maybe

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
    
type IFileSystemWrapper =
    abstract member FullName: string with get
    abstract member Exists: bool with get
    abstract member Delete: unit -> Async<Maybe<unit>>

type IFileWrapper =
    inherit IFileSystemWrapper
    abstract member Path: IDirectoryWrapper with get
    abstract member ReadAllText: unit -> Maybe<string>
    abstract member WriteAllText: text:Maybe<string> -> Maybe<unit>

and IDirectoryWrapper =
    inherit IFileSystemWrapper
    abstract member Name: string with get
    abstract member GetFiles: pattern:string -> IFileWrapper mlist
    abstract member GetFiles: unit -> IFileWrapper mlist
    abstract member GetDirectories: unit -> IDirectoryWrapper mlist
    abstract member Parent : IDirectoryWrapper with get
    
type IFileSystemAccessor =
    abstract member File: path:string -> IFileWrapper
    abstract member Directory: path:string -> IDirectoryWrapper
    abstract member MDirectory: path:string maybe -> IDirectoryWrapper maybe
    abstract member JoinF: path:Maybe<string> -> fileName:Maybe<string> -> Maybe<IFileWrapper>
    abstract member JoinFD: directory:Maybe<IDirectoryWrapper> -> fileName:Maybe<string> -> Maybe<IFileWrapper>
    abstract member JoinD: root:string -> childFolder:string -> IDirectoryWrapper
    abstract member MJoinD: root:string maybe -> childFolder:string maybe -> IDirectoryWrapper maybe
    
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
        member this.File path = this.FileSystem.File path
        member this.Directory path = this.FileSystem.Directory path
        member this.MDirectory path = this.FileSystem.Directory |> llift path
        member this.JoinF path fileName = this.FileSystem.JoinF path fileName
        member this.JoinFD path fileName = this.FileSystem.JoinFD path fileName
        member this.JoinD path childFolder = this.FileSystem.JoinD path childFolder
        member this.MJoinD path childFolder =
            let f = this.FileSystem.JoinD |> lift
            childFolder /-> f path
            
    interface IPrinter with
        member this.PrintF format = this.Printer.PrintF format
        member this.PrintFn format = this.Printer.PrintFn format
        
    member this.File path = this.FileSystem.File path
    member this.Directory path = this.FileSystem.Directory path
    member this.JoinF path fileName = this.FileSystem.JoinF path fileName
    member this.JoinFD path fileName = this.FileSystem.JoinFD path fileName
    member this.JoinD childFolder path = this.FileSystem.JoinD childFolder path
    member this.MJoinD childFolder path = (this :> IFileSystemAccessor).MJoinD childFolder path
    member this.PrintF format = this.Printer.PrintF format
    member this.PrintFn format = this.Printer.PrintFn format
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
