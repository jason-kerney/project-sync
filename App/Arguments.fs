module ProjectSync.App.Arguments

open Argu
open ProjectSync.Types

type RunTimeArguments =
    {
        SyncLocation : string maybe
        IdLocation: string maybe
        Company: string maybe
        Project: string maybe
        TokenName: string maybe
        Token: string maybe
        Add: bool
        Remove: bool
        InsertFilter: string maybe
        RemoveFilter: string maybe
    }
    member this.ToSimplifiedString() =
        let maybeToString value =
            match value with
            | Ok v -> $"Ok ({v})"
            | Error _ -> "Error"
            
        [
            $"{nameof this.SyncLocation}: {this.SyncLocation |> maybeToString}"
            $"{nameof this.IdLocation}: {this.IdLocation |> maybeToString}"
            $"{nameof this.Company}: {this.Company |> maybeToString}"
            $"{nameof this.Project}: {this.Project |> maybeToString}"
            $"{nameof this.TokenName}: {this.TokenName |> maybeToString}"
            $"{nameof this.Token}: {this.Token |> maybeToString}"
            $"{nameof this.Add}: {this.Add}"
            $"{nameof this.Remove}: {this.Remove}"
            $"{nameof this.InsertFilter}: {this.InsertFilter |> maybeToString}"
            $"{nameof this.RemoveFilter}: {this.RemoveFilter |> maybeToString}"
        ]
        |> List.filter (fun s -> s.Contains "Error" |> not)
        |> joinBys "; "
        |> sprintf "{{%s}}"
    
type ArgumentParseResults =
    | ShowVersion
    | ShowHelp of string
    | Run of RunTimeArguments
    member this.ToSimplifiedString () =
        match this with
        | ShowVersion
        | ShowHelp _ -> this.ToString ()
        | Run args ->
            $"Run ({args.ToSimplifiedString ()})"

type private Arguments =
    | [<Unique>](*[<AltCommandLine("--target-path")>][<AltCommandLine("-tp")>]*)[<MainCommand>] Sync_Location of string
    | [<Unique>][<AltCommandLine("-idp")>] Azure_Id_Config_Path of string
    | [<Unique>][<AltCommandLine("-c")>] Company_Name of string
    | [<Unique>][<AltCommandLine("-p")>] Project_Name of string
    | [<Unique>][<AltCommandLine("-u")>] Token_Name of string
    | [<Unique>][<AltCommandLine("--password")>][<AltCommandLine("-pwd")>] Token_Value of string
    | [<Unique>][<AltCommandLine("-add")>] Should_Add_New
    | [<Unique>][<AltCommandLine("-ins")>] Insert_Matching of string
    | [<Unique>][<AltCommandLine("-del")>] Should_Delete
    | [<Unique>][<AltCommandLine("-rem")>] Remove_Matching of string
    | [<Unique>][<AltCommandLine("-v")>] Version
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Sync_Location _ -> "The directory housing the location of your repositories. The Azure Id config file is assumed to be here if not specified."
            | Azure_Id_Config_Path _ -> "The directory where the .azureIdentity file is located"
            | Company_Name _ -> "Used if the .azureIdentity does not exist. It is the company used to access Azure"
            | Project_Name _ -> "Used if the .azureIdentity does not exist. It is the Azure project housing the repositories"
            | Token_Name _ -> "Used if the .azureIdentity does not exist. It is the name given to the used auth token."
            | Token_Value _ -> "Used if the .azureIdentity does not exist. It is the auth token value."
            | Should_Add_New -> "Signals to add repositories and will prompt for filter text if \"-insert\" is not present"
            | Insert_Matching _ -> "A regex to filter the selection list of repositories to add locally"
            | Should_Delete -> "Signals to remove repositories and will prompt for filter text if \"-remove\" is not present"
            | Remove_Matching _ -> "A regex to filter the selection list of repositories to delete locally"
            | Version -> "Shows the current applications version"
            
let private toMaybe key result : _ maybe =
    match result with
    | Some v -> Ok v
    | None -> key |> sprintf "%s not provided" |> asGeneralFailure
    
let private toBool (result: _ maybe) =
    match result with
    | Ok _ -> true
    | _ -> false
            
let parse args =
    let parser = ArgumentParser.Create<Arguments>(programName = "ProjectSync.exe")
    let results = parser.ParseCommandLine(inputs = args, raiseOnUsage = false)
    
    let tp = typeof<Arguments>
    let path =
        let root =  tp.Assembly.Location |> System.IO.Path.GetDirectoryName
        "AppSettings.xml"
        |> sprintf "%s%c%s" root System.IO.Path.DirectorySeparatorChar
    
    let resultsXml =
        try
            let configurationReader = ConfigurationReader.FromAppSettingsFile (path)
            parser.ParseConfiguration (configurationReader = configurationReader, ignoreMissing = true)
        with _ -> results
    
    let syncLocation =
        resultsXml.TryGetResult Sync_Location |> toMaybe "--sync_location"
        |> prefer (results.TryGetResult Sync_Location |> toMaybe "--sync_location")
    let idLocation =
        resultsXml.TryGetResult Azure_Id_Config_Path |> toMaybe "--azure-id-config-path"
        |> prefer (results.TryGetResult Azure_Id_Config_Path |> toMaybe "--azure-id-config-path")
    let company =
        resultsXml.TryGetResult Company_Name |> toMaybe "--company-name"
        |> prefer (results.TryGetResult Company_Name |> toMaybe "--company-name")
    let project =
        resultsXml.TryGetResult Project_Name |> toMaybe "--project-name"
        |> prefer (results.TryGetResult Project_Name |> toMaybe "--project-name")
    let tokenName =
        resultsXml.TryGetResult Token_Name |> toMaybe "--token-name"
        |> prefer (results.TryGetResult Token_Name |> toMaybe "--token-name")
    let tokenValue =
        resultsXml.TryGetResult Token_Value |> toMaybe "--toke-value"
        |> prefer (results.TryGetResult Token_Value |> toMaybe "--toke-value")
    let insertFilter =
        resultsXml.TryGetResult Insert_Matching |> toMaybe "--insert-matching"
        |> prefer (results.TryGetResult Insert_Matching |> toMaybe "--insert-matching")
    let shouldAdd =
        resultsXml.Contains Should_Add_New
        || results.Contains Should_Add_New
        || (insertFilter |> toBool)
        
    let removeFilter =
        resultsXml.TryGetResult Remove_Matching |> toMaybe "--remove-matching"
        |> prefer (results.TryGetResult Remove_Matching |> toMaybe "--remove-matching")
    let shouldDelete =
        resultsXml.Contains Should_Delete
        || results.Contains Should_Delete
        || removeFilter |> toBool
    let showVersion =
        resultsXml.Contains Version
        || results.Contains Version
    let showHelp = results.IsUsageRequested
    
    if showHelp then parser.PrintUsage () |> ShowHelp
    elif showVersion then ShowVersion
    else
        {
            SyncLocation = syncLocation
            IdLocation = idLocation
            Company = company
            Project = project
            TokenName = tokenName
            Token = tokenValue
            Add = shouldAdd
            Remove = shouldDelete
            InsertFilter = insertFilter
            RemoveFilter = removeFilter
        } |> Run
    

