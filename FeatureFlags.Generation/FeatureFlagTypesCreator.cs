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
            ctx.ReportDiagnostic(diagnostic1);
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

        GenerateClassForObject(jsonObject, baseNamespace, ROOT_CLASS_NAME);
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
        parsedStructure = JsonStructureParser.Parse(featureFlagsSection);
        diagnostic = null;
        return true;
    }

    private (string requiredNamespace, string typeName) GenerateClassForObject(
        JsonObjectType jsonStructure, string @namespace, string nameInParent)
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        // generate nested types (that will allow properties in this class to have appropriate types)
        (string? requiredNamespace, (string propType, string propName))[] propsAndTheirTypes =
            jsonStructure.Properties
                .Select((KeyValuePair<string, JsonType> kvp) =>
                {
                    (string propName, JsonType propDetails) = kvp;
                    (string? requiredNamespace, string type) = propDetails switch
                    {
                        JsonPrimitiveType primitive => GetTypeOfPrimitive(primitive),
                        JsonArrayType arr => GenerateClassForArray(arr, $"{@namespace}.{propName}", propName),
                        JsonObjectType obj => GenerateClassForObject(obj, $"{@namespace}.{propName}", propName),
                        _ => (baseNamespace, UNDEFINED_CLASS_NAME),
                    };
                    return (requiredNamespace, (type, propName));
                })
                .ToArray();

        IEnumerable<string> propsLines = propsAndTheirTypes
            .Select(x =>
            {
                string propType = x.Item2.propType;
                string propName = x.Item2.propName;
                return $"public required {propType} {propName} {{ get; set; }}";
            });

        IEnumerable<string> usingStatements = propsAndTheirTypes
            .Select(x => x.requiredNamespace)
            .Distinct()
            .Where(ns => ns != null)!
            .Select(ns => $"using {ns};");

        string className = $"{nameInParent}Type";

        // TODO: order of members: primitives, arrays, nested objects
        // TODO: PERF: use a string builder to build the class's code
        ctx.AddSource(
            hintName: $"{className}.generated.cs",
            source: $$"""
                      {{string.Join("\n", usingStatements)}}

                      namespace {{@namespace}};

                      public class {{className}}
                      {
                          {{string.Join("\n    ", propsLines)}}
                      }
                      """);

        return (requiredNamespace: @namespace, typeName: className);
    }

    private (string? requiredNamespace, string typeName) GenerateClassForArray(
        JsonArrayType jsonStructure, string @namespace, string nameInParent)
    {
        string nameForArrayItem = $"{nameInParent}Item";

        (string? requiredNamespace, string typeName) result = jsonStructure.ItemType switch
        {
            JsonPrimitiveType primitive => GetTypeOfPrimitive(primitive),
            JsonArrayType array => GenerateClassForArray(array, @namespace, nameForArrayItem),
            JsonObjectType obj => GenerateClassForObject(obj, @namespace, nameForArrayItem),
            _ => (baseNamespace, UNDEFINED_CLASS_NAME),
        };

        result.typeName += "[]";
        return result;
    }

    private (string?, string) GetTypeOfPrimitive(JsonPrimitiveType jsonStructure)
    {
        return jsonStructure switch
        {
            { Kind: JsonValueKind.String } => (null, "string"),
            { Kind: JsonValueKind.Number } => (null, "int"),
            { Kind: JsonValueKind.True } => (null, "bool"),
            { Kind: JsonValueKind.False } => (null, "bool"),
            { Kind: JsonValueKind.Null } => (null, "float"),
            _ => (baseNamespace, UNDEFINED_CLASS_NAME),
        };
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