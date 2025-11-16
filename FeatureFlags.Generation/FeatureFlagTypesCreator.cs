using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using org.g14.FeatureFlags.Generation.JsonStructure;

namespace org.g14.FeatureFlags.Generation;

public class FeatureFlagTypesCreator(
    ImmutableArray<AdditionalText> appsettingsFiles,
    string baseNamespace,
    SourceProductionContext ctx)
{
    /// <summary>
    /// Reads the structure of feature flags from appsettings and generates types representing them
    /// </summary>
    public void ScanAppsettingsAndCreateSourceFiles()
    {
        if (!IsFileCountValid(appsettingsFiles))
        {
            // TODO: generate a file with a COMPILATION_ERROR property having a value of "the implementation could not be generated"
            return;
        }

        JsonType? parsedStructure = ReadAndParseAppsettings();
        if (parsedStructure is not JsonObjectType jsonObject) return;

        GenerateFeatureFlagClass(jsonObject, baseNamespace);
        // TODO: generate array items and nested object items

        GenerateUndefinedType();
    }

    private bool IsFileCountValid(ImmutableArray<AdditionalText> files)
    {
        if (files.Length == 0)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                descriptor: DiagnosticDescriptors.NotEnoughFiles,
                location: null,
                messageArgs: []));
            return false;
        }

        if (files.Length != 1)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                descriptor: DiagnosticDescriptors.TooManyFiles,
                location: null,
                messageArgs: []));
            return false;
        }

        return true;
    }

    private JsonType? ReadAndParseAppsettings()
    {
        SourceText? appsettingsSourceText = appsettingsFiles.First().GetText();
        if (appsettingsSourceText == null)
        {
            // TODO: diagnostic: cannot read file
            return null;
        }

        using JsonDocument appsettingsDoc = JsonDocument.Parse(appsettingsSourceText.ToString());
        ctx.CancellationToken.ThrowIfCancellationRequested();
        JsonElement featureFlagsSection = appsettingsDoc.RootElement.GetProperty("FeatureFlags");
        JsonType jsonStructure = JsonStrucureParser.Parse(featureFlagsSection);
        return jsonStructure;
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
                (string type, string comment) = pair.Value.Kind switch
                {
                    JsonValueKind.String => ("string", string.Empty),
                    JsonValueKind.Number => ("int", string.Empty),
                    JsonValueKind.False or JsonValueKind.True => ("bool", string.Empty),
                    _ => ("Undefined", $" // the unidentified JsonValueKind value was: {pair.Value.Kind}")
                };
                return $"public required {type} {name} {{ get; set; }}{comment}";
            });


        // TODO: order of members (once all are handled): primitives, arrays, nested objects
        // TODO: PERF: use a string builder to build the class's code
        ctx.AddSource(
            hintName: "FlagsRoot.generated.cs",
            source: $$"""
                      namespace {{@namespace}};

                      public class FlagsRoot
                      {
                          {{string.Join("\n    ", propsLines)}}
                      }
                      """);
    }

    private void GenerateUndefinedType()
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        ctx.AddSource(
            hintName: "Undefined.generated.cs",
            source: $$"""
                      namespace {{baseNamespace}};

                      /// <summary>
                      /// The type of the item in appsettings could not be identified
                      /// </summary>
                      public class Undefined
                      {
                      }
                      """);
    }
}