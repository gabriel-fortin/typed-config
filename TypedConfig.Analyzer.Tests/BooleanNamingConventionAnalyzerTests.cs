using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace org.g14.TypedConfig.Analyzer.Tests;

/// <summary>
/// Tests for <see cref="BooleanNamingConventionAnalyzer"/>.
/// Running (or debugging) these tests drives the analyzer directly in-process, so breakpoints
/// set in BooleanNamingConventionAnalyzer.cs hit normally - no attach-to-build-process needed.
/// </summary>
[TestFixture]
public class BooleanNamingConventionAnalyzerTests
{
    #region Helper Methods

    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(
        string? sourceCode = null,
        string? appsettingsContent = null,
        IReadOnlyDictionary<string, string>? editorConfig = null)
    {
        SyntaxTree[] syntaxTrees = sourceCode != null
            ? [CSharpSyntaxTree.ParseText(sourceCode)]
            : [];

        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        ];

        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ImmutableArray<AdditionalText> additionalTexts = appsettingsContent != null
            ? [new TestAdditionalText("appsettings.json", appsettingsContent)]
            : ImmutableArray<AdditionalText>.Empty;

        AnalyzerOptions analyzerOptions = editorConfig != null
            ? new AnalyzerOptions(additionalTexts, new TestConfigOptionsProvider(new TestConfigOptions(editorConfig)))
            : new AnalyzerOptions(additionalTexts);

        var analyzer = new BooleanNamingConventionAnalyzer();
        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            [analyzer],
            analyzerOptions);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private class TestAdditionalText(string path, string content) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(content, Encoding.UTF8);

        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) => _text;
    }

    /// <summary>
    /// Test double that surfaces a dictionary of .editorconfig key/values, using the same
    /// case-insensitive key comparison the real editorconfig options use.
    /// </summary>
    private sealed class TestConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values;

        public TestConfigOptions(IReadOnlyDictionary<string, string> values)
        {
            _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> pair in values)
            {
                _values[pair.Key] = pair.Value;
            }
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _values.TryGetValue(key, out value!);
        }

        public override IEnumerable<string> Keys => _values.Keys;
    }

    private sealed class TestConfigOptionsProvider(AnalyzerConfigOptions options) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;
    }

    #endregion

    [Test]
    public async Task Appsettings_WithBadlyNamedBooleanFlag_ReportsDiagnostic()
    {
        // Arrange
        var appsettings = """
        {
            "Flags": {
                "DarkMode": true
            }
        }
        """;

        // Act
        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(appsettingsContent: appsettings);

        // Assert
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo("TYPEDCONFIG_A_001"));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("DarkMode"));
    }

    [Test]
    public async Task Appsettings_WithWellNamedBooleanFlag_ReportsNoDiagnostic()
    {
        // Arrange
        var appsettings = """
        {
            "Flags": {
                "IsDarkModeEnabled": true
            }
        }
        """;

        // Act
        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(appsettingsContent: appsettings);

        // Assert
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task Appsettings_WithNestedBadlyNamedBooleanFlag_ReportsDiagnostic()
    {
        // Arrange
        var appsettings = """
        {
            "Flags": {
                "Database": {
                    "Retry": true
                }
            }
        }
        """;

        // Act
        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(appsettingsContent: appsettings);

        // Assert
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("Retry"));
    }

    [Test]
    public async Task Appsettings_WithBadlyNamedBooleanFlagAtRoot_ReportsDiagnostic()
    {
        // Arrange
        var appsettings = """
        {
            "DestroyEvil": true
        }
        """;

        // Act
        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(appsettingsContent: appsettings);

        // Assert
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("DestroyEvil"));
    }

    [Test]
    public async Task PropertyUsage_OnGeneratedModelWithBadlyNamedBoolean_ReportsDiagnostic()
    {
        // Arrange
        var source = """
        namespace TestApp.Generated
        {
            public class ConfigModel
            {
                public bool DarkMode { get; set; }
                public bool IsDarkModeEnabled { get; set; }
            }
        }

        namespace TestApp
        {
            public class Consumer
            {
                public void Use(Generated.ConfigModel config)
                {
                    var a = config.DarkMode;
                    var b = config.IsDarkModeEnabled;
                }
            }
        }
        """;

        // Act
        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(sourceCode: source);

        // Assert
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("DarkMode"));
    }

    [Test]
    public async Task PropertyUsage_OnGeneratedModelWithExcludedAttribute_ReportsNoDiagnostic()
    {
        // Arrange
        var source = """
        namespace TestApp.Generated
        {
            public sealed class ExcludeFromBoolNamingConventionAttribute : System.Attribute
            {
            }

            public class ConfigModel
            {
                [ExcludeFromBoolNamingConvention]
                public bool DarkMode { get; set; }
            }
        }

        namespace TestApp
        {
            public class Consumer
            {
                public void Use(Generated.ConfigModel config)
                {
                    var a = config.DarkMode;
                }
            }
        }
        """;

        // Act
        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(sourceCode: source);

        // Assert
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task PropertyUsage_OnNonGeneratedModel_ReportsNoDiagnostic()
    {
        // Arrange
        var source = """
        namespace TestApp
        {
            public class PlainClass
            {
                public bool DarkMode { get; set; }
            }

            public class Consumer
            {
                public void Use(PlainClass model)
                {
                    var a = model.DarkMode;
                }
            }
        }
        """;

        // Act
        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(sourceCode: source);

        // Assert
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task Appsettings_WithExcludedTopLevelSection_ReportsNoDiagnostic()
    {
        // Arrange
        var appsettings = """
        {
            "Logging": {
                "DarkMode": true
            }
        }
        """;
        var editorConfig = new Dictionary<string, string>
        {
            ["typed_config.excluded_sections"] = "Logging",
        };

        // Act
        ImmutableArray<Diagnostic> diagnostics =
            await RunAnalyzerAsync(appsettingsContent: appsettings, editorConfig: editorConfig);

        // Assert
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task Appsettings_WithExcludedNestedSection_ExcludesOnlyThatSubtree()
    {
        // Arrange - "Database:Advanced" is excluded, but a bad boolean elsewhere still reports
        var appsettings = """
        {
            "Database": {
                "Retry": true,
                "Advanced": {
                    "DarkMode": true
                }
            }
        }
        """;
        var editorConfig = new Dictionary<string, string>
        {
            ["typed_config.excluded_sections"] = "Database:Advanced",
        };

        // Act
        ImmutableArray<Diagnostic> diagnostics =
            await RunAnalyzerAsync(appsettingsContent: appsettings, editorConfig: editorConfig);

        // Assert
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("Retry"));
        Assert.That(diagnostics[0].GetMessage(), Does.Not.Contain("DarkMode"));
    }

    [Test]
    public async Task Appsettings_WithoutExclusionConfig_StillReports()
    {
        // Arrange - same JSON as the exclusion test, but no editorconfig option
        var appsettings = """
        {
            "Logging": {
                "DarkMode": true
            }
        }
        """;

        // Act
        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(appsettingsContent: appsettings);

        // Assert
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("DarkMode"));
    }

    [Test]
    public async Task Appsettings_ExcludedSectionMatchIsCaseInsensitive()
    {
        // Arrange - config uses lower-case "logging", JSON section is "Logging"
        var appsettings = """
        {
            "Logging": {
                "DarkMode": true
            }
        }
        """;
        var editorConfig = new Dictionary<string, string>
        {
            ["typed_config.excluded_sections"] = "logging",
        };

        // Act
        ImmutableArray<Diagnostic> diagnostics =
            await RunAnalyzerAsync(appsettingsContent: appsettings, editorConfig: editorConfig);

        // Assert
        Assert.That(diagnostics, Is.Empty);
    }
}