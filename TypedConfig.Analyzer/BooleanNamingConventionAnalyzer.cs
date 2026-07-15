using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace org.g14.TypedConfig.Analyzer;

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

        // Register compilation start so we can add symbol and operation actions
        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            compilationStartContext.RegisterOperationAction(AnalyzePropertyReferenceOperation,
                OperationKind.PropertyReference);
        });
    }

    private static void AnalyzeAppsettingsFile(AdditionalFileAnalysisContext context)
    {
        // Only analyze appsettings.json file
        bool isAppsettingsFile = Path.GetFileName(context.AdditionalFile.Path)
            .Equals("appsettings.json", StringComparison.OrdinalIgnoreCase);
        if (!isAppsettingsFile) return;

        SourceText? sourceText = context.AdditionalFile.GetText(context.CancellationToken);
        if (sourceText == null) return;

        ImmutableHashSet<string> excludedSections = GetExcludedSections(context);

        try
        {
            string jsonText = sourceText.ToString();
            JToken root = JToken.Parse(jsonText);

            AnalyzeJsonNode(context, root, sourceText, currentPath: "", excludedSections);
        }
        catch (JsonException)
        {
            // Invalid JSON - ignore
        }
    }

    /// <summary>
    /// Reads the <c>typed_config.excluded_sections</c> option from .editorconfig for the
    /// appsettings file. The value is a comma-separated list of ASP.NET-style colon paths
    /// (e.g. "Logging, Database:Advanced"); each names a section whose subtree is skipped.
    /// </summary>
    private static ImmutableHashSet<string> GetExcludedSections(AdditionalFileAnalysisContext context)
    {
        AnalyzerConfigOptions configOptions =
            context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.AdditionalFile);

        if (!configOptions.TryGetValue("typed_config.excluded_sections", out string? raw))
        {
            return ImmutableHashSet<string>.Empty;
        }

        return raw.Split(',')
            .Select(section => section.Trim())
            .Where(section => section.Length > 0)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void AnalyzeJsonNode(
        AdditionalFileAnalysisContext context,
        JToken jsonNode,
        SourceText sourceText,
        string currentPath,
        ImmutableHashSet<string> excludedSections)
    {
        if (jsonNode.Type != JTokenType.Object) return;

        foreach (JProperty property in ((JObject)jsonNode).Properties())
        {
            string propertyName = property.Name;

            // Build the ASP.NET-style path (e.g. "Database:Advanced") for this property
            string childPath = currentPath.Length == 0
                ? propertyName
                : currentPath + ":" + propertyName;

            if (excludedSections.Contains(childPath)) continue;

            switch (property.Value.Type)
            {
                // If this property is a boolean value
                case JTokenType.Boolean:
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
                case JTokenType.Object:
                    // Recursively analyze nested objects
                    AnalyzeJsonNode(context, property.Value, sourceText, childPath, excludedSections);
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

    private static void AnalyzePropertyReferenceOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation propRef) return;

        IPropertySymbol propertySymbol = propRef.Property;
        if (propertySymbol.Type.SpecialType != SpecialType.System_Boolean) return;

        INamedTypeSymbol? containingType = propertySymbol.ContainingType;
        if (containingType == null) return;

        if (!IsGeneratedModel(containingType)) return;

        string name = propertySymbol.Name;
        if (StartsWithValidBooleanPrefix(name)) return;

        // Report diagnostic at usage location
        Location location = context.Operation.Syntax.GetLocation();
        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.BooleanNamingConvention, location, name);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsGeneratedModel(INamedTypeSymbol type)
    {
        // Check for [GeneratedCode] attribute
        foreach (AttributeData attr in type.GetAttributes())
        {
            INamedTypeSymbol? attrClass = attr.AttributeClass;
            if (attrClass?.ToDisplayString() == "System.CodeDom.Compiler.GeneratedCodeAttribute")
            {
                return true;
            }
        }

        // Check namespace contains Generated
        string @namespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (@namespace.Contains("Generated", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check file paths for generated file patterns
        foreach (Location loc in type.Locations)
        {
            if (!loc.IsInSource) continue;
            string path = loc.SourceTree?.FilePath ?? string.Empty;
            if (path.Contains(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("generated", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}