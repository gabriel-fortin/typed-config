namespace org.g14.TypedConfig.Generator.CodeProduction.Models;

public record struct PropDetails(
    string PropType,
    string? RequiredNamespace,
    string PropName)
{
    public static PropDetails From(PartialPropDetails partial, string propName)
    {
        return new PropDetails(
            partial.PropType,
            partial.RequiredNamespace,
            propName);
    }
}