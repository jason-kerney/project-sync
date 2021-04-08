```
USAGE: ProjectSync.exe [--help] [--sync-location <string>] [--azure-id-config-path <string>] [--company-name <string>]
[--project-name <string>] [--token-name <string>] [--token-value <string>] [--should-add-new]
[--insert-matching <string>] [--should-delete] [--remove-matching <string>] [--version]

OPTIONS:

    --sync-location, --target-path, -tp <string>
                          The directory housing the location of your repositories. The Azure Id config file is assumed to be here if not specified.
    --azure-id-config-path, -idp <string>
                          The directory where the .azureIdentity file is located
    --company-name, -c <string>
                          Used if the .azureIdentity does not exist. It is the company used to access Azure
    --project-name, -p <string>
                          Used if the .azureIdentity does not exist. It is the Azure project housing the repositories
    --token-name, -u <string>
                          Used if the .azureIdentity does not exist. It is the name given to the used auth token.
    --token-value, -pwd <string>
                          Used if the .azureIdentity does not exist. It is the auth token value.
    --should-add-new, -add
                          Signals to add repositories and will prompt for filter text if "-insert" is not present
    --insert-matching, -ins <string>
                          A regex to filter the selection list of repositories to add locally
    --should-delete, -del Signals to remove repositories and will prompt for filter text if "-remove" is not present
    --remove-matching, -rem <string>
                          A regex to filter the selection list of repositories to delete locally
    --version, -v         Shows the current applications version
    --help                display this list of options.
```

All opions can be controlled via the AppSettings.xml also