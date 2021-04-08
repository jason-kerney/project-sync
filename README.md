
<!-- GENERATED DOCUMENT! DO NOT EDIT! -->
# Project Sync #


## Table Of Contents ##

- [Section 1: Badges](#user-content-badges)
- [Section 2: Summary](#user-content-summary)
- [Section 3: Use](#user-content-use)
- [Section 4: Contributers](#user-content-contributers)

## Badges ##
![.NET Core](https://github.com/jason-kerney/project-sync/workflows/.NET%20Core/badge.svg)
[![License](https://img.shields.io/github/license/jason-kerney/project-sync)](https://github.com/jason-kerney/SafeSqlBuilder/blob/main/LICENSE)

    

## Summary ##

A commandline tool to manage local repositories from a corpate Azure git instance.


    

## Use ##

### Features

This tool provides these basic features:

1. Fetch all local repositories with a single command.
1. Clone new repositories entirely from command line.
1. Remove local instances of a repository
1. Easily share a suite of repositories among developers by sharing 1 file.

### Command Line Guide

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

# AppSettings.xml
All opions can be controlled via the AppSettings.xml also

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <appSettings>
        <!-- These Are Global Overrides -->
        <!--<add key="sync location" value="." /> -->
        <!--<add key="azure id config path" value="." /> -->
        <!--<add key="company name" value="some-company" -->
        <!--<add key="project name" value="some-project"> -->
        <!--<add key="token name" value="some-user-name"> -->
        <!--<add key="token value" value="some-password"> -->
    </appSettings>
</configuration>
```
    

## Contributers ##
## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->
<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!
    

<!-- GENERATED DOCUMENT! DO NOT EDIT! -->
    