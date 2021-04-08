module ProjectSync.Lib.EnvironmentBuilder

open System
open ProjectSync.Types

let idFilenameRaw = ".azureIdentity"
let idFilename : Maybe<_> = idFilenameRaw |> Ok
let companyNameKey = "company"
let projectNameKey = "project"
let userNameKey = "tokenName"
let passwordKey = "token"

type private ConfigurationItem =
    {
        Key: string
        Value: string
    }
    
let private buildItem (parts: string list) =
    let key = parts.Head
    let value = parts.Tail |> List.head
    
    {
        Key = key
        Value = value
    }

let private decodeBase64 value =
    let decode = Convert.FromBase64String |> lift
    let toString = (fun (value: byte []) -> System.Text.Encoding.UTF8.GetString value) |> lift 
    value
    |> decode
    |> toString
    
let private parseConfigFile configLocation (fs: #IFileSystemAccessor) =
    maybe {
        let! file =
            idFilename
            |> fs.JoinF configLocation
            
        let! body = file.ReadAllText ()
        let items =
            body
            |> split
        let filtered =
            items
            |> List.filter (fun line -> line.Contains ':' && line.StartsWith '#' |> not)
            
        let r =
            filtered
            |> List.map (fun line -> line.Trim () |> splitBy [|':'|] |> buildItem )
            
        return r
    }
    
let private getValue (item: ConfigurationItem Maybe) =
    maybe {
        let! item = item
        return item.Value
    }
    
let private findValue key (items : ConfigurationItem mlist) =
    if items |> MaybeList.exists (fun i -> i.Key = key) then
        let item = 
            items
            |> MaybeList.filter (fun item -> item.Key = key)
            |> MaybeList.head

        item |> getValue
        
    else
        key
        |> sprintf "%A key does not exist"
        |> asGeneralFailure
        
let getAuthValue userName password =
    maybe {
        let! userName = userName
        let! password = password
        
        return
            password
            |> sprintf "%s:%s" userName
            |> System.Text.Encoding.UTF8.GetBytes 
            |> Convert.ToBase64String
    }
    
let getEnvironment (printer: #IPrinter) (fileSystem: #IFileSystemAccessor) idRootPath syncLocation : SyncEnvironment =
    let configs = parseConfigFile idRootPath fileSystem
    let authName = configs |> findValue userNameKey
    let authToken = configs |> findValue passwordKey
    let company = configs |> findValue companyNameKey
    let project = configs |> findValue projectNameKey
    
    {
        Printer = printer
        FileSystem = fileSystem
        SyncLocation = syncLocation
        AuthToken = authToken |> getAuthValue authName
        Company = company
        Project = project
    }
    
let idFileExists (fileSystem: #IFileSystemAccessor) idLocation =
    idFilename
    |> fileSystem.JoinF idLocation
    |> FileSystem.maybeExists
    
let writeEnvironmentFile (env: SyncEnvironment) idLocation =
    let asIdPart key = sprintf "%s:%s" key |> lift
    let tokenParts =
        env.AuthToken
        |> decodeBase64
        |> msplitBy [|':'|]
        
    let tokenName = tokenParts |> MaybeList.head |> asIdPart userNameKey
    let tokenValue = tokenParts |> MaybeList.tail |> MaybeList.head |> asIdPart passwordKey
    let company = env.Company |> asIdPart companyNameKey
    let project = env.Project |> asIdPart projectNameKey
    
    let file =
        idFilename
        |> env.JoinF idLocation 
    
    [
        "# A file to allow application access to Azure git repositories" |> Ok
        company
        project
        tokenName
        tokenValue
    ]
    |> MaybeList.flatten
    |> mjoin
    |> FileSystem.maybeWriteAllText file
    