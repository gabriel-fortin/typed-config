using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace org.g14.TypedConfig.Generator;

[Generator(LanguageNames.CSharp)]
public class IncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // prepare pipeline: getting the root namespace of the project that uses this generator
        IncrementalValueProvider<string> baseNamespaceProvider =
            initContext.CompilationProvider
                .Select((compilation, _) => compilation.AssemblyName ?? "AssemblyNamespaceDetectionFailed")
                .Select((x, _) => x + ".GeneratedTypedConfig");

        // prepare pipeline: getting the appsettings file of the project that uses this generator
        IncrementalValueProvider<ImmutableArray<AdditionalText>> appsettingsFilesProvider =
            initContext.AdditionalTextsProvider
                .Where(static text => text.Path.EndsWith("appsettings.json"))
                .Collect();

        // prepare pipeline: getting the "typed_config.excluded_sections" .editorconfig option
        // for the appsettings file (comma-separated, colon-path sections, e.g. "Logging, Database:Advanced")
        IncrementalValueProvider<ImmutableHashSet<string>> excludedSectionsProvider =
            appsettingsFilesProvider
                .Combine(initContext.AnalyzerConfigOptionsProvider)
                .Select((pair, _) => GetExcludedSections(pair.Left, pair.Right));

        // use the data from the pipelines to start generating source code
        initContext.RegisterSourceOutput(
            source: appsettingsFilesProvider.Combine(baseNamespaceProvider).Combine(excludedSectionsProvider),
            action: (sourceProductionContext, input) =>
            {
                // the core logic is hidden in these lines
                var generator = new TypedConfigTypesGenerator(
                    baseNamespace: input.Left.Right,
                    excludedSections: input.Right,
                    ctx: sourceProductionContext);
                generator.GenerateServiceCollectionExtensionMethod();
                generator.GenerateExcludeFromBoolNamingConventionAttribute();
                generator.ScanAppsettingsAndGenerateMatchingSourceFiles(appsettingsFiles: input.Left.Left);
            });
    }

    /// <summary>
    /// Reads the <c>typed_config.excluded_sections</c> option from .editorconfig for the
    /// appsettings file. The value is a comma-separated list of ASP.NET-style colon paths
    /// (e.g. "Logging, Database:Advanced"); each names a section whose subtree is excluded.
    /// </summary>
    private static ImmutableHashSet<string> GetExcludedSections(
        ImmutableArray<AdditionalText> appsettingsFiles,
        AnalyzerConfigOptionsProvider optionsProvider)
    {
        if (appsettingsFiles.Length == 0) return ImmutableHashSet<string>.Empty;

        AnalyzerConfigOptions configOptions = optionsProvider.GetOptions(appsettingsFiles[0]);

        if (!configOptions.TryGetValue("typed_config.excluded_sections", out string? raw))
        {
            return ImmutableHashSet<string>.Empty;
        }

        return raw.Split(',')
            .Select(section => section.Trim())
            .Where(section => section.Length > 0)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }
}