using Microsoft.CodeAnalysis;

namespace org.g14.FeatureFlags.Analyzer;

/// <summary>
/// Contains diagnostic descriptors for the FeatureFlags analyzer.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Naming = "Naming";

    /// <summary>
    /// Diagnostic for boolean configuration values that don't follow naming conventions.
    /// Boolean values should start with prefixes like "is", "has", "can", "should", etc.
    /// </summary>
    public static readonly DiagnosticDescriptor BooleanNamingConvention = new(
        id: "FLAGS_A_001",
        title: "Boolean configuration value should follow naming conventions",
        messageFormat: "Boolean configuration value '{0}' should start with 'is', 'has', 'can'," +
        " 'must', 'should', 'allow', 'enable', 'use', or similar prefix",
        category: Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Boolean configuration values should follow naming conventions to improve code" +
        " readability. Use prefixes like 'is', 'has', 'can', 'must', 'should', 'allow', 'enable', or 'use'.");
}