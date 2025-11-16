using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using org.g14.FeatureFlags.Generation.JsonStructure;

namespace org.g14.FeatureFlags.Generation;

[Generator(LanguageNames.CSharp)]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        string baseNamespace = "org.g14.UsageExample";
        // TODO: use the namespace of the consuming project + ".GeneratedFeatureFlags"

        IncrementalValueProvider<ImmutableArray<AdditionalText>> appsettingsFiles =
            initContext.AdditionalTextsProvider
                .Where(static text => text.Path.EndsWith("appsettings.json"))
                .Collect();

        // TODO: make the lambda 'static', if possible
        initContext.RegisterSourceOutput(appsettingsFiles, (ctx, files) =>
        {
            // TODO: extract the content of this lambda as a class; use ctx and files as properties

            if (!IsFileCountValid(files, ctx))
            {
                // TODO: generate a file with a COMPILATION_ERROR property having a value of "the implementation could not be generated"
                return;
            }

            JsonType? parsedStructure = ReadAndParseAppsettings(ctx, files);
            if (parsedStructure is not JsonObjectType jsonObject) return;

            GenerateFeatureFlagClass(ctx, jsonObject, baseNamespace);
            // TODO: generare array items and nested object items

            GenerateUndefinedType(ctx, baseNamespace);
        });

        // TODO: generate service collection extension method using RegisterPostInitializationOutput
    }

    private static void GenerateUndefinedType(SourceProductionContext ctx, string baseNamespace)
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        ctx.AddSource(
            hintName: "Undefined.generated.cs",
            source: $$"""
                      namespace {{baseNamespace}};

                      /// <summary>
                      /// The type of the config item could not be identified
                      /// </summary>
                      public class Undefined
                      {
                      }
                      """);
    }

    private static JsonType? ReadAndParseAppsettings(SourceProductionContext ctx, ImmutableArray<AdditionalText> txts)
    {
        SourceText? appsettingsSourceText = txts.First().GetText();
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

    private static void GenerateFeatureFlagClass(SourceProductionContext ctx, JsonObjectType jsonStructure,
        string @namespace)
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
                    _ => ("Undefined", $" // the unidentitied JsonValueKind value was: {pair.Value.Kind}")
                };
                return $$"""public required {{type}} {{name}} { get; set; }{{comment}}""";
            });


        // TODO: order of members (once all are handeled): primitives, arrays, nested objects
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

    private static bool IsFileCountValid(ImmutableArray<AdditionalText> txts, SourceProductionContext ctx)
    {
        if (txts.Length == 0)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                descriptor: DiagnosticDescriptors.NotEnoughFiles,
                location: null,
                messageArgs: []));
            return false;
        }

        if (txts.Length != 1)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                descriptor: DiagnosticDescriptors.TooManyFiles,
                location: null,
                messageArgs: []));
            return false;
        }

        return true;
    }
}