using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using org.g14.FeatureFlags.Generation.JsonStructure;

namespace org.g14.FeatureFlags.Generation;

// helper type
public record PropDetails(string? RequiredNamespace, string PropType, string PropName);

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
        GenerateTheUndefinedType();

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

        // the actual generation of classes matching appsettings items
        var (ns, type) = GenerateAndGetTypeOfObjectJsonItem(jsonObject, baseNamespace, ROOT_CLASS_NAME);
        // TODO: use the created type name to generate a service collection extensions method
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

    /// <summary>
    /// Computes the type for an appsettings item of the object kind.
    /// Internally, causes code generation for that type.
    /// </summary>
    private (string requiredNamespace, string typeName) GenerateAndGetTypeOfObjectJsonItem(
        JsonObjectType jsonStructure, string @namespace, string nameInParent)
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        // generate nested types (required to generate properties for this class)
        PropDetails[] propsAndTheirTypes =
            jsonStructure.Properties
                .Select(GetTypeOfObjectProperty)
                .ToArray();

        var className = $"{nameInParent}Type";
        GenerateClassCodeForAppsettingsObject(@namespace, propsAndTheirTypes, className);
        return (requiredNamespace: @namespace, typeName: className);

        // local helper function
        PropDetails GetTypeOfObjectProperty(KeyValuePair<string, JsonType> kvp)
        {
            (string propName, JsonType propDetails) = kvp;

            (string? requiredNamespace, string type) = propDetails switch
            {
                JsonPrimitiveType primitive => GetTypeOfPrimitiveJsonItem(primitive),
                JsonArrayType arr =>
                    GetTypeOfArrayJsonItem(arr, $"{@namespace}.{propName}", propName),
                JsonObjectType obj =>
                    GenerateAndGetTypeOfObjectJsonItem(obj, $"{@namespace}.{propName}", propName),
                _ => (baseNamespace, UNDEFINED_CLASS_NAME),
            };

            return new PropDetails(requiredNamespace, type, propName);
        }
    }

    /// <summary>
    /// Computes the type for an appsettings item of the array kind.
    /// Possibly causes class code generation in downstream calls.
    /// </summary>
    private (string? requiredNamespace, string typeName) GetTypeOfArrayJsonItem(
        JsonArrayType jsonStructure, string @namespace, string nameInParent)
    {
        string nameForArrayItem = $"{nameInParent}Item";

        (string? requiredNamespace, string typeName) result = jsonStructure.ItemType switch
        {
            JsonPrimitiveType primitive => GetTypeOfPrimitiveJsonItem(primitive),
            JsonArrayType array => GetTypeOfArrayJsonItem(array, @namespace, nameForArrayItem),
            JsonObjectType obj => GenerateAndGetTypeOfObjectJsonItem(obj, @namespace, nameForArrayItem),
            _ => (baseNamespace, UNDEFINED_CLASS_NAME),
        };

        result.typeName += "[]";
        return result;
    }

    /// <summary>
    /// Computes the type for an appsettings item of the primitive kind.
    /// </summary>
    private (string?, string) GetTypeOfPrimitiveJsonItem(JsonPrimitiveType jsonStructure)
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

    // TODO: instead of using ctx, return a CodeDetails object and let the caller call ctx
    private void GenerateClassCodeForAppsettingsObject(
        string @namespace,
        PropDetails[] propsAndTheirTypes,
        string className)
    {
        IEnumerable<string> propsLines = propsAndTheirTypes
            .Select(x => $"public required {x.PropType} {x.PropName} {{ get; set; }}");

        IEnumerable<string> usingStatements = propsAndTheirTypes
            .Select(x => x.RequiredNamespace)
            .Distinct()
            .Where(ns => ns != null)
            .Select(ns => $"using {ns};");

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

    private void GenerateTheUndefinedType()
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