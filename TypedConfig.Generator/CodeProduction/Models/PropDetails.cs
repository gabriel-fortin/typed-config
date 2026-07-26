namespace org.g14.TypedConfig.Generator.CodeProduction.Models;

public record struct PropDetails(
    string PropType,
    string? RequiredNamespace,
    string PropName,
    bool ExcludeFromBoolNamingConvention = false)
{
    public static PropDetails From(PartialPropDetails partial, string propName, bool excludeFromBoolNamingConvention)
    {
        return new PropDetails(
            partial.PropType,
            partial.RequiredNamespace,
            propName,
            excludeFromBoolNamingConvention);
    }
}