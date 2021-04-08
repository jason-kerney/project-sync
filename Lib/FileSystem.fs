module ProjectSync.Lib.FileSystem

open System.IO
open ProjectSync.Types

type private ActualFile (path, printer: IPrinter) =
    interface IFileWrapper with
        member __.FullName
            with get () =
                let info = FileInfo (path)
                info.FullName
        member __.Exists
            with get () =
                let info = FileInfo (path)
                info.Refresh ()
                info.Exists
                
        member __.Delete () =
            async {
                return
                    maybe {
                        let info = FileInfo (path)
                        info.Refresh ()
                        if info.Exists then
                            info.Attributes <- FileAttributes.Normal
                            info.Delete ()
                    }
            }
            
        member this.Path
            with get () =
                let name = this.FullName
                (ActualDirectory (name, printer)) :> IDirectoryWrapper
                
        member this.ReadAllText () =
            let file = FileInfo (this.FullName)
            if file.Exists then
                maybe {
                    file.Refresh ()
                    return
                        file.FullName
                        |> File.ReadAllText
                }
            else
                file.FullName
                |> sprintf "%s does not exist, unable to read."
                |> asGeneralFailure
                
        member this.WriteAllText text =
            let file = this.AsFile ()
            maybe {
                let! text = text
                return 
                    File.WriteAllText (file.FullName, text)
            }
            
    member this.AsFile () = this :> IFileWrapper
    member this.FullName
        with get () =
            let file = this.AsFile ()
            file.FullName
            
    member this.Path
        with get () =
            let file = this.AsFile ()
            file.Path

and private ActualDirectory (path, printer: IPrinter) =
    interface IDirectoryWrapper with
        member __.FullName
            with get () =
                let info = DirectoryInfo (path)
                info.FullName
                
        member __.Exists
            with get () =
                let info = DirectoryInfo (path)
                info.Refresh ()
                info.Exists
                
        member this.Delete () =
            let dir = this.AsDir ()
            
            async {
                return
                    maybe {
                        let directories = dir.GetDirectories ()
                        let files = dir.GetFiles ()
                     
                        let! fileDeletions =        
                            files
                            |> MaybeList.map (fun f -> f.Delete ())
                            
                        let! _success =
                            fileDeletions
                            |> Async.Parallel
                            |> Async.RunSynchronously
                            |> MaybeList.reduceErrors
                            
                        let! directoryDeletions =
                            directories
                            |> MaybeList.map (fun d -> d.Delete ())
                            
                        let! _success =
                            directoryDeletions
                            |> Async.Parallel
                            |> Async.RunSynchronously
                            |> MaybeList.reduceErrors
                            
                        let info = DirectoryInfo (dir.FullName)
                        info.Delete ()
                        let msg = sprintf "Deleted:%s\n" dir.FullName
                        printer.PrintFn "%s" msg
                            
                        return ()
                    }
            }
            
        member __.Name
            with get () =
                let info = DirectoryInfo (path)
                info.Name
                
        member __.GetFiles pattern =
            let info = DirectoryInfo (path)
            if info.Exists then
                maybe {
                    return
                        pattern
                        |> info.GetFiles
                        |> Array.toList
                        |> List.map (fun i -> ActualFile (i.FullName, printer) :> IFileWrapper)
                }
            else
                info.FullName
                |> sprintf "%s does not exist and does not have files to filter"
                |> asGeneralFailure
                
        member __.GetFiles () =
            let info = DirectoryInfo (path)
            if info.Exists then
                maybe {
                    return
                        ()
                        |> info.GetFiles
                        |> Array.toList
                        |> List.map (fun i -> ActualFile (i.FullName, printer) :> IFileWrapper)
                }
            else
                info.FullName
                |> sprintf "%s does not exist and does not have files"
                |> asGeneralFailure
                
        member __.GetDirectories () =
            let info = DirectoryInfo (path)
            if info.Exists then
                maybe {
                    return
                        ()
                        |> info.GetDirectories
                        |> Array.toList
                        |> List.map (fun i -> ActualDirectory (i.FullName, printer) :> IDirectoryWrapper)
                }
            else
                info.FullName
                |> sprintf "%s does not exist and does not have children directories"
                |> asGeneralFailure
                
        member __.Parent
            with get () =
                let info = DirectoryInfo (path)
                
                ActualDirectory (info.Parent.FullName, printer)
                :> IDirectoryWrapper
                
    member this.AsDir () = this :> IDirectoryWrapper
    
    member this.FullName
        with get () =
            let dir = this.AsDir ()
            dir.FullName
            
let getFile printer path =
    ActualFile (path, printer) :> IFileWrapper
    
let getDirectory printer path =
    ActualDirectory (path, printer) :> IDirectoryWrapper

type private ActualFileSystem (printer) =
    interface IFileSystemAccessor with
        member __.File path = path |> getFile printer
        member __.Directory path = path |> getDirectory printer
        member this.MDirectory path =  (this.AsFileSystem ()).Directory |> llift path 
        member this.JoinF path fileName =
            let fs = this.AsFileSystem ()
            maybe {
                let! path = path
                let! fileName = fileName
                
                let fullPath =
                    sprintf "%s%c%s" path Path.DirectorySeparatorChar fileName

                return                     
                    fullPath |> fs.File
            }
            
        member this.JoinFD (directory: Maybe<IDirectoryWrapper>) (fileName: Maybe<string>) =
            let fs = this.AsFileSystem ()
            maybe {
                let! directory = directory
                let path = directory.FullName |> Ok
                return!
                    fs.JoinF path fileName
            }
            
        member this.JoinD root childFolder =
            let fs = this.AsFileSystem ()
            let fullPath =
                sprintf "%s%c%s" root Path.DirectorySeparatorChar childFolder
                
            fullPath |> fs.Directory

        member this.MJoinD root childFolder =
            let fs = this.AsFileSystem ()
            let join = fs.JoinD |> lift
            childFolder
            ^> join root
            
    member this.AsFileSystem () = this :> IFileSystemAccessor
    
let getFileSystem printer =
    ActualFileSystem (printer)
    :> IFileSystemAccessor
    
let maybeGetFullName (fs: #IFileSystemWrapper maybe) =
    maybe {
        let! fs = fs
        return fs.FullName
    }
    
let getFullName (fs: #IFileSystemWrapper) = fs.FullName
    
let exists (fs: #IFileSystemWrapper) = fs.Exists
    
let maybeExists (fs: #IFileSystemWrapper maybe) =
    maybe {
        let! fs = fs
        return fs.Exists
    } |> toBool

let maybePath (file: #IFileWrapper maybe) =
    maybe {
        let! file = file
        return file.Path
    }
    
let maybeReadAllText (file: Maybe<#IFileWrapper>) =
    maybe {
        let! file = file
        return! file.ReadAllText ()
    }
    
let maybeWriteAllText (file: #IFileWrapper maybe) text =
    maybe {
        let! file = file
        return! file.WriteAllText text
    }