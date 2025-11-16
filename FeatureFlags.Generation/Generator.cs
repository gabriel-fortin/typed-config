using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace org.g14.FeatureFlags.Generation;

[Generator(LanguageNames.CSharp)]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // prepare pipeline: getting the root namespace of the project using this generator
        IncrementalValueProvider<string> rootNamespace =
            initContext.AnalyzerConfigOptionsProvider
                .Select(static (opts, _) =>
                {
                    if (opts.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rn))
                        return rn;
                    if (opts.GlobalOptions.TryGetValue("build_property.AssemblyName", out var an))
                        return an;

                    return "Global";
                });

        // prepare pipeline: getting the appsettings files of the project using this generator
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
                string @namespace = input.Right + ".GeneratedFeatureFlags";

                // the core logic is hidden in here
                var typesCreator = new FeatureFlagTypesCreator(files, @namespace, sourceProductionContext);
                typesCreator.ScanAppsettingsAndCreateSourceFiles();
            });

        // TODO: generate service collection extension method using RegisterPostInitializationOutput
    }
}