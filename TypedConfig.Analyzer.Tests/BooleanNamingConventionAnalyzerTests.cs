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
        string? appsettingsContent = null)
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

        var analyzer = new BooleanNamingConventionAnalyzer();
        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            [analyzer],
            new AnalyzerOptions(additionalTexts));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private class TestAdditionalText(string path, string content) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(content, Encoding.UTF8);

        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) => _text;
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
}