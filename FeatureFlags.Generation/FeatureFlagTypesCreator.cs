using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using org.g14.FeatureFlags.Generation.JsonStructure;

namespace org.g14.FeatureFlags.Generation;

public class FeatureFlagTypesCreator(
    ImmutableArray<AdditionalText> appsettingsFiles,
    string baseNamespace,
    SourceProductionContext ctx)
{
    private const string ROOT_CLASS_NAME = "FlagsRoot";
    private const string UNDEFINED_CLASS_NAME = "Undefined";

    /// <summary>
    /// Reads the structure of feature flags from appsettings and generates types representing them
    /// </summary>
    public void ScanAppsettingsAndCreateSourceFiles()
    {
        GenerateUndefinedType();

        if (!IsFileCountValid(appsettingsFiles, out Diagnostic? diagnostic1))
        {
            GenerateRootClassRepresentingError(diagnostic1.GetMessage());
            return;
        }

        if (!TryReadAndParseAppsettings(out var parsedStructure, out var diagnostic2))
        {
            ctx.ReportDiagnostic(diagnostic2);
            GenerateRootClassRepresentingError(diagnostic2.GetMessage());
            return;
        }

        if (parsedStructure is not JsonObjectType jsonObject) return;

        GenerateFeatureFlagClass(jsonObject, baseNamespace);
        // TODO: generate array items and nested object items
    }

    private bool TryReadAndParseAppsettings([NotNullWhen(true)] out JsonType? parsedStructure,
        [NotNullWhen(false)] out Diagnostic? diagnostic)
    {
        AdditionalText file = appsettingsFiles.First();
        SourceText? appsettingsSourceText = file.GetText(ctx.CancellationToken);
        if (appsettingsSourceText == null)
        {
            parsedStructure = null;
            diagnostic = Diagnostic.Create(
                descriptor: DiagnosticDescriptors.CannotReadFile,
                location: null,
                messageArgs: [file.Path]);
            return false;
        }

        using JsonDocument appsettingsDoc = JsonDocument.Parse(appsettingsSourceText.ToString());
        ctx.CancellationToken.ThrowIfCancellationRequested();
        JsonElement featureFlagsSection = appsettingsDoc.RootElement.GetProperty("FeatureFlags"u8);
        parsedStructure = JsonStrucureParser.Parse(featureFlagsSection);
        diagnostic = null;
        return true;
    }

    private void GenerateFeatureFlagClass(JsonObjectType jsonStructure, string @namespace)
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        // TODO: add param for the name of the class

        IEnumerable<KeyValuePair<string, JsonPrimitiveType>> props =
            jsonStructure.Properties
                .Where(x => x.Value is JsonPrimitiveType)
                .Select(x => new KeyValuePair<string, JsonPrimitiveType>(x.Key, (JsonPrimitiveType)x.Value));

        IEnumerable<string> propsLines = props
            .Select(pair =>
            {
                string name = pair.Key;
                string type = pair.Value.Kind switch
                {
                    JsonValueKind.String => "string",
                    JsonValueKind.Number => "int",
                    JsonValueKind.False => "bool",
                    JsonValueKind.True => "bool",
                    _ => UNDEFINED_CLASS_NAME
                };

                string result = $"public required {type} {name} {{ get; set; }}";

                if (type == UNDEFINED_CLASS_NAME)
                {
                    result += $" // the unidentified JsonValueKind value was: {pair.Value.Kind}";
                }

                return result;
            });


        // TODO: order of members (once all are handled): primitives, arrays, nested objects
        // TODO: PERF: use a string builder to build the class's code
        ctx.AddSource(
            hintName: $"{ROOT_CLASS_NAME}.generated.cs",
            source: $$"""
                      namespace {{@namespace}};

                      public class {{ROOT_CLASS_NAME}}
                      {
                          {{string.Join("\n    ", propsLines)}}
                      }
                      """);
    }

    private void GenerateRootClassRepresentingError(string errorMessage)
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        ctx.AddSource(
            hintName: $"{ROOT_CLASS_NAME}.generated.cs",
            source: $$"""
                      namespace {{baseNamespace}};

                      public class {{ROOT_CLASS_NAME}}
                      {
                          /// <summary>
                          /// {{HttpUtility.HtmlEncode(errorMessage)}}
                          /// </summary>
                          public string COMPILATION_ERROR = "File could not be generated. See the doc comment of this property for details";
                      }
                      """);
    }

    private void GenerateUndefinedType()
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        ctx.AddSource(
            hintName: $"{UNDEFINED_CLASS_NAME}.generated.cs",
            source: $$"""
                      namespace {{baseNamespace}};

                      /// <summary>
                      /// The type of the item in appsettings could not be identified
                      /// </summary>
                      public class {{UNDEFINED_CLASS_NAME}}
                      {
                      }
                      """);
    }

    private static bool IsFileCountValid(ImmutableArray<AdditionalText> files,
        [NotNullWhen(false)] out Diagnostic? diagnostic)
    {
        if (files.Length == 0)
        {
            diagnostic = Diagnostic.Create(
                descriptor: DiagnosticDescriptors.NotEnoughFiles,
                location: null,
                messageArgs: []);
            return false;
        }

        if (files.Length != 1)
        {
            diagnostic = Diagnostic.Create(
                descriptor: DiagnosticDescriptors.TooManyFiles,
                location: null,
                messageArgs: []);
            return false;
        }

        diagnostic = null;
        return true;
    }
}