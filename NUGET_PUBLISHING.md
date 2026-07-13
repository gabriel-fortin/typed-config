
increment version in TypedConfig.csproj


move entries from AnalyzerReleases.Unshipped to AnalyzerReleases.Shipped in:
- the generator project
- the analyzer project


pack Typed config
do the above twice if the project was never built


verify the generated file
Test 1
- open the nupkg file (it's a zip file)
- make sure the REAMDE is in `/`
- make sure the generator and analyzer DLLs are in `/analyzers/dotnet/cs/` 
Test 2
- update the nuget reference in the UsageExample project to the version you just packed
- build and run the project



Login to nuget.org and upload the nupkg file
