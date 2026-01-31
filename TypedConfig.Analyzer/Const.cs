namespace org.g14.TypedConfig.Analyzer;

/// <summary>
/// Constants used throughout the analyzer.
/// </summary>
internal static class Const
{
    /// <summary>
    /// Common prefixes for boolean property/field names.
    /// </summary>
    public static readonly string[] BooleanPrefixes = new[]
    {
        "is",
        "was",
        "were",
        "will",
        "has",
        "can",
        "must",
        "should",
        "allow",
        "allows",
        "enable",
        "enables",
        "use",
        "uses",
        "needs",
        "require",
        "requires",
        "supports",
        "includes",
        "contains"
    };
}

