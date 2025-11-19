using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace org.g14.FeatureFlags.Generation;

[Generator(LanguageNames.CSharp)]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // prepare pipeline: getting the root namespace of the project that uses this generator
        IncrementalValueProvider<string> rootNamespace =
            initContext.AnalyzerConfigOptionsProvider.Select(GetRootNamespace);

        // prepare pipeline: getting the appsettings files of the project that uses this generator
        IncrementalValueProvider<ImmutableArray<AdditionalText>> appsettingsFiles =
            initContext.AdditionalTextsProvider
                .Where(static text => text.Path.EndsWith("appsettings.json"))
                .Collect();

        // use the data from the pipelines to start generating source code
        initContext.RegisterSourceOutput(
            source: appsettingsFiles.Combine(rootNamespace),
            action: (sourceProductionContext, input) =>
            {
                ImmutableArray<AdditionalText> files = input.Left;
                string baseNamespace = input.Right + ".GeneratedFeatureFlags";

                // the core logic is hidden in here
                var typesCreator = new FeatureFlagTypesGenerator(files, baseNamespace, sourceProductionContext);
                typesCreator.ScanAppsettingsAndGenerateMatchingSourceFiles();
            });

        // TODO: generate service collection extension method using RegisterPostInitializationOutput
    }

    private static string GetRootNamespace(AnalyzerConfigOptionsProvider opts, CancellationToken _)
    {
        if (opts.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rn))
            return rn;
        if (opts.GlobalOptions.TryGetValue("build_property.AssemblyName", out var an))
            return an;

        return "Global";
    }
}