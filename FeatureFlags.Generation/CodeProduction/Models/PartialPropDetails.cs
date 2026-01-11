namespace org.g14.FeatureFlags.Generation.CodeProduction.Models;

public record struct PartialPropDetails(
    string PropType,
    string? RequiredNamespace);