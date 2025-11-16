using Microsoft.CodeAnalysis;

namespace org.g14.FeatureFlags.Generation;

public static class DiagnosticDescriptors
{
    private const string INPUT_FILES_CATEGORY = "Input files";
    
    public static readonly DiagnosticDescriptor TooManyFiles = new(
        id: "FLAGS_001",
        title: "Multiple appsettings files found",
        messageFormat: "Feature flags: multiple appsettings.json files were found among <AdditionalFiles>. " +
        "Make sure your csproj file declares exactly one appsettings.json file as <AdditionalFiles>.",
        category: INPUT_FILES_CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotEnoughFiles = new(
        id: "FLAGS_002",
        title: "No appsettings file found",
        messageFormat: "Feature flags: no appsettings.json file was found among <AdditionalFiles>. " +
        "Make sure your csproj file declares an appsettings.json file as <AdditionalFiles>.",
        category: INPUT_FILES_CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}