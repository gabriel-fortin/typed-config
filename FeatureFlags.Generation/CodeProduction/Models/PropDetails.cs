namespace org.g14.FeatureFlags.Generation.CodeProduction.Models;

public record struct PropDetails(
    string? RequiredNamespace,
    string PropType,
    string PropName);