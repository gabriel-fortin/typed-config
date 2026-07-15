# Publishing a new version

This file describes steps for creating a new version of the nuget package


## Update code and prepare package

- Move entries from AnalyzerReleases.Unshipped to AnalyzerReleases.Shipped in:
  + the generator project
  + the analyzer project
- Update the <Version> node in TypedConfig.csproj
- Update version of the TypedConfig package reference in UsageExample.csproj
- Commit the changes (so that the generated nuget package references the right git hash)
- Make sure you're using the Release configuration
- Build/Pack the TypedConfig project (creates .nupkg file in ./generated_nuget_packages)


## Verify the package
- open the nupkg file (it's a zip file)
- make sure the REAMDE is in `/`
- make sure the generator and analyzer DLLs are in `/analyzers/dotnet/cs/` 
- make sure the Newtonsoft.Json dependency DLL is in `/analyzers/dotnet/cs/`


## Share with the world
- Upload the .nupkg file to nuget.org
- Add tag for the new version to the git repo
- Push repo changes (including tag)
