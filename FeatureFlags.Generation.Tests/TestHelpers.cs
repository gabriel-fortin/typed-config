namespace org.g14.FeatureFlags.Generation.Tests;

public static class TestHelpers
{
    /// <summary>
    /// Normalizes line endings to \n for consistent cross-platform string comparison
    /// </summary>
    public static string NormalizeLineEndings(string input)
    {
        return input.Replace("\r\n", "\n")
            .Replace("\n\n\n", "\n")
            .Replace("\n\n", "\n");
    }
}
