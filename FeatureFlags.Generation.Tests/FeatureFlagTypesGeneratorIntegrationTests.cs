using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace org.g14.FeatureFlags.Generation.Tests;

/// <summary>
/// Tests for FeatureFlagTypesGenerator class which generates types based on appsettings JSON structure
/// Tests use the Roslyn GeneratorDriver to properly test source generation
/// </summary>
[TestFixture]
public class FeatureFlagTypesGeneratorIntegrationTests
{
    #region Helper Methods

    private static bool IsErrorSeverity(Diagnostic d) => d.Severity == DiagnosticSeverity.Error;

    private static string GeneratedCode(GeneratorDriverRunResult result) =>
        string.Join("\n", result.GeneratedTrees.Select(t => t.GetText().ToString()));

    private static GeneratorDriverRunResult RunGenerator(string? appsettingsContent = null)
    {
        // Create a compilation
        CSharpCompilation compilation = CSharpCompilation.Create("TestAssembly",
            syntaxTrees: Array.Empty<SyntaxTree>(),
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        // Create an instance of our generator
        var generatorUnderTest = new IncrementalGenerator();

        // Create additional files if provided
        ImmutableArray<AdditionalText> additionalTexts = appsettingsContent != null
            ? [new TestAdditionalText("appsettings.json", appsettingsContent)]
            : ImmutableArray<AdditionalText>.Empty;

        // Create and run the driver
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generatorUnderTest)
            .AddAdditionalTexts(additionalTexts);

        return driver.RunGenerators(compilation).GetRunResult();
    }

    /// <summary>
    /// Test implementation of AdditionalText for testing purposes
    /// </summary>
    private class TestAdditionalText(
        string path,
        string content
    ) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(content, Encoding.UTF8);

        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default)
        {
            return _text;
        }
    }

    #endregion

    [Test]
    public void Generator_WithNoFiles_ReportsNotEnoughFilesError()
    {
        // Act
        GeneratorDriverRunResult result = RunGenerator();

        // Assert
        Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(result.Diagnostics[0].Id, Is.EqualTo("FLAGS_002"));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Error));
    }

    [Test]
    public void Generator_WithEmptyFeatureFlags_GeneratesTheRootType()
    {
        // Arrange
        var jsonContent = """
        {
            "FeatureFlags": {}
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("class FlagsRootType"));
    }

    [Test]
    public void Generator_WithEmptyArray_UsesUndefinedType()
    {
        // Arrange
        var jsonContent = """
        {
            "FeatureFlags": {
                "EmptyList": []
            }
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("public required Undefined[] EmptyList"));
    }

    [Test]
    public void Generator_WithAllPrimitiveTypes_GeneratesExpectedProperties()
    {
        // Arrange
        var jsonContent = """
        {
            "FeatureFlags": {
                "StringValue": "text",
                "NumberValue": 42,
                "TrueValue": true,
                "FalseValue": false
            }
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("class FlagsRootType"));
        Assert.That(generatedCode, Does.Contain("public required string StringValue"));
        Assert.That(generatedCode, Does.Contain("public required int NumberValue"));
        Assert.That(generatedCode, Does.Contain("public required bool TrueValue"));
        Assert.That(generatedCode, Does.Contain("public required bool FalseValue"));
    }

    [Test]
    public void Generator_WithNestedObject_GeneratesExpectedTypeAndProperties()
    {
        // Arrange
        var jsonContent = """
        {
            "FeatureFlags": {
                "Database": {
                    "ConnectionTimeout": 30,
                    "EnableRetry": true
                }
            }
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("class DatabaseType"));
        Assert.That(generatedCode, Does.Contain("public required int ConnectionTimeout"));
        Assert.That(generatedCode, Does.Contain("public required bool EnableRetry"));
    }

    [Test]
    public void Generator_WithArraysOfPrimitives_GeneratesExpectedProperties()
    {
        // Arrange
        var jsonContent = """
        {
            "FeatureFlags": {
                "AllowedHosts": ["localhost", "example.com"],
                "Ports": [80, 443, 8080]
            }
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("public required string[] AllowedHosts"));
        Assert.That(generatedCode, Does.Contain("public required int[] Ports"));
    }

    [Test]
    public void Generator_WithArrayOfObjects_GeneratesExpectedArrayItemTypeAndProperties()
    {
        // Arrange
        var jsonContent = """
        {
            "FeatureFlags": {
                "Endpoints": [
                    {
                        "Name": "API",
                        "Port": 8080
                    }
                ]
            }
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("class EndpointsItemType"));
        Assert.That(generatedCode, Does.Contain("public required EndpointsItemType[] Endpoints"));
        Assert.That(generatedCode, Does.Contain("public required string Name"));
        Assert.That(generatedCode, Does.Contain("public required int Port"));
    }

    [Test]
    public void Generator_WithComplexNestedStructure_GeneratesExpectedTypesAndProperties()
    {
        // Arrange
        var jsonContent = """
        {
            "FeatureFlags": {
                "Features": {
                    "Authentication": {
                        "Enabled": true,
                        "Providers": ["OAuth", "SAML"]
                    },
                    "Logging": {
                        "Level": "Info",
                        "MaxSize": 1024
                    }
                },
                "Settings": [
                    {
                        "Key": "Setting1",
                        "Value": 100
                    }
                ]
            }
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("class FeaturesType"));
        Assert.That(generatedCode, Does.Contain("class AuthenticationType"));
        Assert.That(generatedCode, Does.Contain("class LoggingType"));
        Assert.That(generatedCode, Does.Contain("class SettingsItemType"));
        Assert.That(generatedCode, Does.Contain("public required int MaxSize"));
    }

    [Test]
    public void Generator_WithLargeNestedStructure_GeneratesExpectedTypesAndProperty()
    {
        // Arrange
        var jsonContent = """
        {
            "FeatureFlags": {
                "Level1": {
                    "Level2": {
                        "Level3": {
                            "Level4": {
                                "Level5": {
                                    "DeepValue": true
                                }
                            }
                        }
                    }
                }
            }
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("class Level5Type"));
        Assert.That(generatedCode, Does.Contain("public required bool DeepValue"));
    }

    [Test]
    public void Generator_WithMultiDimensionalArray_GeneratesExpectedProperty()
    {
        // Arrange
        var jsonContent = """
        {
            "FeatureFlags": {
                "Matrix": [[1, 2], [3, 4]]
            }
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("public required int[][] Matrix"));
    }

    [Test]
    public void Generator_WithMixedArrayTypes_GeneratesExpectedProperties()
    {
        // Arrange
        var jsonContent = """
        {
            "FeatureFlags": {
                "StringArray": ["a", "b", "c"],
                "NumberArray": [1, 2, 3],
                "BoolArray": [true, false, true]
            }
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("public required string[] StringArray"));
        Assert.That(generatedCode, Does.Contain("public required int[] NumberArray"));
        Assert.That(generatedCode, Does.Contain("public required bool[] BoolArray"));
    }

    [Test]
    public void Generator_AdditionalContentInJson_RunsSuccessfullyAndGeneratesExpectedProperties()
    {
        // Arrange
        var jsonContent = """
        {
            "Logging": {
                "LogLevel": {
                    "Default": "Information",
                    "Microsoft.Hosting.Lifetime": "Information"
                }
            },
            "FeatureFlags": {
                "StringValue": "text",
                "NumberValue": 42,
                "BoolValue": true,
            }
        }
        """;

        // Act
        GeneratorDriverRunResult result = RunGenerator(jsonContent);

        // Assert
        Assert.That(result.Diagnostics.Where(IsErrorSeverity), Is.Empty);
        Assert.That(result.GeneratedTrees.Length, Is.GreaterThan(0));
        string generatedCode = GeneratedCode(result);
        Assert.That(generatedCode, Does.Contain("class FlagsRootType"));
        Assert.That(generatedCode, Does.Contain("public required string StringValue"));
        Assert.That(generatedCode, Does.Contain("public required int NumberValue"));
        Assert.That(generatedCode, Does.Contain("public required bool BoolValue"));
    }
}