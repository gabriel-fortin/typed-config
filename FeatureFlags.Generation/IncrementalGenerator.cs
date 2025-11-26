using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace org.g14.FeatureFlags.Generation;

[Generator(LanguageNames.CSharp)]
public class IncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // prepare pipeline: getting the root namespace of the project that uses this generator
        IncrementalValueProvider<string> baseNamespaceProvider =
            initContext.AnalyzerConfigOptionsProvider
                .Select(GetRootNamespace)
                .Select((x, _) => x + ".GeneratedFeatureFlags");

        // prepare pipeline: getting the appsettings files of the project that uses this generator
        IncrementalValueProvider<ImmutableArray<AdditionalText>> appsettingsFilesProvider =
            initContext.AdditionalTextsProvider
                .Where(static text => text.Path.EndsWith("appsettings.json"))
                .Collect();

        // use the data from the pipelines to start generating source code
        initContext.RegisterSourceOutput(
            source: appsettingsFilesProvider.Combine(baseNamespaceProvider),
            action: (sourceProductionContext, input) =>
            {
                // the core logic is hidden in these lines
                var generator = new FeatureFlagTypesGenerator(
                    baseNamespace: input.Right,
                    ctx: sourceProductionContext);
                generator.GenerateServiceCollectionExtensionMethod();
                generator.ScanAppsettingsAndGenerateMatchingSourceFiles(appsettingsFiles: input.Left);
            });
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