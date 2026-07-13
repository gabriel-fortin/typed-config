using Microsoft.CodeAnalysis;

namespace org.g14.TypedConfig.Generator;

public static class DiagnosticDescriptors
{
    private const string INPUT_FILES_CATEGORY = "Input files";

    public static readonly DiagnosticDescriptor TooManyFiles = new(
        id: "TYPEDCONFIG_001",
        title: "Multiple appsettings files found",
        messageFormat: "Typed config: multiple appsettings.json files were found among <AdditionalFiles>. " +
        "Make sure your csproj file declares exactly one appsettings.json file as <AdditionalFiles>.",
        category: INPUT_FILES_CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotEnoughFiles = new(
        id: "TYPEDCONFIG_002",
        title: "No appsettings file found",
        messageFormat: "Typed config: no appsettings.json file was found among <AdditionalFiles>. " +
        "Make sure your csproj file declares an appsettings.json file as <AdditionalFiles>.",
        category: INPUT_FILES_CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CannotReadFile = new(
        id: "TYPEDCONFIG_003",
        title: "File cannot be read or parsed",
        messageFormat: "Typed config: cannot read or parse appsettings file: '{FileName}'",
        category: INPUT_FILES_CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}