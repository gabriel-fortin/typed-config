using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace org.g14.FeatureFlags.Analyzer;

/// <summary>
/// Analyzer that checks boolean configuration values in appsettings.json follow naming conventions.
/// Boolean values should start with prefixes like "is", "has", "can", "should", etc.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BooleanNamingConventionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.BooleanNamingConvention];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for additional file analysis to check appsettings.json
        context.RegisterAdditionalFileAction(AnalyzeAppsettingsFile);
    }

    private static void AnalyzeAppsettingsFile(AdditionalFileAnalysisContext context)
    {
        // Only analyze appsettings.json file
        bool isAppsettingsFile = Path.GetFileName(context.AdditionalFile.Path)
            .Equals("appsettings.json", StringComparison.OrdinalIgnoreCase);
        if (!isAppsettingsFile) return;

        SourceText? sourceText = context.AdditionalFile.GetText(context.CancellationToken);
        if (sourceText == null) return;

        try
        {
            var jsonText = sourceText.ToString();
            using var document = JsonDocument.Parse(jsonText);
            var root = document.RootElement;

            // Look for the FeatureFlags node
            if (root.TryGetProperty("FeatureFlags", out JsonElement featureFlagsNode))
            {
                AnalyzeJsonNode(context, featureFlagsNode, sourceText);
            }
        }
        catch (JsonException)
        {
            // Invalid JSON - ignore
        }
    }

    private static void AnalyzeJsonNode(
        AdditionalFileAnalysisContext context,
        JsonElement element,
        SourceText sourceText)
    {
        if (element.ValueKind != JsonValueKind.Object) return;

        foreach (JsonProperty property in element.EnumerateObject())
        {
            string propertyName = property.Name;

            switch (property.Value.ValueKind)
            {
                // If this property is a boolean value
                case JsonValueKind.True or JsonValueKind.False:
                    // If the property name follows naming conventions
                    if (!StartsWithValidBooleanPrefix(propertyName))
                    {
                        Location location = GetPropertyLocation(sourceText, propertyName, context.AdditionalFile.Path);

                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.BooleanNamingConvention,
                            location,
                            propertyName);

                        context.ReportDiagnostic(diagnostic);
                    }

                    break;
                case JsonValueKind.Object:
                    // Recursively analyze nested objects
                    AnalyzeJsonNode(context, property.Value, sourceText);
                    break;
            }
        }
    }

    private static Location GetPropertyLocation(SourceText sourceText, string propertyName, string filePath)
    {
        // Search for the property name in the source text
        var searchPattern = $"""
            "{propertyName}"
            """;
        int index = sourceText.ToString().IndexOf(searchPattern, StringComparison.Ordinal);

        if (index < 0)
        {
            // Fallback if we can't find the exact location
            return Location.Create(filePath, default, default);
        }

        int position = index + 1; // Position after the opening quote
        var span = new TextSpan(position, propertyName.Length);
        LinePositionSpan lineSpan = sourceText.Lines.GetLinePositionSpan(span);
        return Location.Create(filePath, span, lineSpan);
    }

    private static bool StartsWithValidBooleanPrefix(string propertyName)
    {
        string lowerName = propertyName.ToLowerInvariant();

        return Const.BooleanPrefixes.Any(prefix =>
            lowerName.StartsWith(prefix) &&
            (lowerName.Length == prefix.Length || char.IsUpper(propertyName[prefix.Length])));
    }
}