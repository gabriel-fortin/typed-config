using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using org.g14.FeatureFlags.Generation.CodeProduction;
using org.g14.FeatureFlags.Generation.CodeProduction.Models;
using org.g14.FeatureFlags.Generation.JsonParsing;
using org.g14.FeatureFlags.Generation.JsonParsing.Models;

namespace org.g14.FeatureFlags.Generation;

public class FeatureFlagTypesGenerator(
    string baseNamespace,
    SourceProductionContext ctx)
{
    private const string ROOT_CLASS_NAME = "FlagsRootType";
    private const string UNDEFINED_CLASS_NAME = "Undefined";

    private readonly ISourceCodeCreator code = new EfficientSourceCodeCreator(baseNamespace, ctx.CancellationToken);
    // private readonly ISourceCodeCreator code = new SimpleSourceCodeCreator(baseNamespace, ctx.CancellationToken);

    /// <summary>
    /// Reads the structure of feature flags from appsettings
    /// and generates classes to match that structure
    /// </summary>
    public void ScanAppsettingsAndGenerateMatchingSourceFiles(ImmutableArray<AdditionalText> appsettingsFiles)
    {
        // add 'unknown' type; used only when something goes wrong
        code.GetUnknownTypeClass(UNDEFINED_CLASS_NAME).WriteTo(ctx);

        if (!IsFileCountValid(appsettingsFiles, out Diagnostic? diagnostic1))
        {
            ctx.ReportDiagnostic(diagnostic1);
            code.GetErrorIndicatingClass(diagnostic1.GetMessage(), ROOT_CLASS_NAME).WriteTo(ctx);
            return;
        }

        if (!TryReadFeatureFlagsStructureFromAppsettings(appsettingsFiles,
                out JsonType? parsedStructure, out Diagnostic? diagnostic2))
        {
            ctx.ReportDiagnostic(diagnostic2);
            code.GetErrorIndicatingClass(diagnostic2.GetMessage(), ROOT_CLASS_NAME).WriteTo(ctx);
            return;
        }

        if (parsedStructure is not JsonObjectType jsonObject) return;

        // the actual generation of classes matching appsettings items
        _ = GenerateAndGetTypeOfObjectJsonItem(jsonObject, baseNamespace, ROOT_CLASS_NAME);
    }


    public void GenerateServiceCollectionExtensionMethod()
    {
        code.GetServiceCollectionExtensionMethod(ROOT_CLASS_NAME).WriteTo(ctx);
    }

    private bool TryReadFeatureFlagsStructureFromAppsettings(
        ImmutableArray<AdditionalText> appsettingsFiles,
        [NotNullWhen(true)] out JsonType? parsedStructure,
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
        JsonElement featureFlagsSection = appsettingsDoc.RootElement.GetProperty(Const.FlagsRootKey);
        parsedStructure = JsonStructureParser.Parse(featureFlagsSection);

        diagnostic = null;
        return true;
    }

    /// <summary>
    /// Computes the type for an appsettings item of the object kind.
    /// Internally, causes code generation for that type.
    /// </summary>
    private PartialPropDetails GenerateAndGetTypeOfObjectJsonItem(
        JsonObjectType jsonStructure, string @namespace, string className)
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        // generate nested types (required to generate properties for this class)
        PropDetails[] propsAndTheirTypes =
            jsonStructure.Properties
                .Select(GetTypeOfObjectProperty)
                .ToArray();

        code.GetAppsettingsObjectClass(@namespace, propsAndTheirTypes, className).WriteTo(ctx);

        return new PartialPropDetails(
            PropType: className,
            RequiredNamespace: @namespace);

        // local helper function
        PropDetails GetTypeOfObjectProperty(KeyValuePair<string, JsonType> kvp)
        {
            (string propName, JsonType propDetails) = kvp;

            PartialPropDetails partialResult = propDetails switch
            {
                JsonPrimitiveType primitive => GetTypeOfPrimitiveJsonItem(primitive),
                JsonArrayType arr =>
                    GetTypeOfArrayJsonItem(arr, $"{@namespace}.{propName}", $"{propName}ItemType"),
                JsonObjectType obj =>
                    GenerateAndGetTypeOfObjectJsonItem(obj, $"{@namespace}.{propName}", $"{propName}Type"),
                _ => new(PropType: UNDEFINED_CLASS_NAME, RequiredNamespace: baseNamespace),
            };

            return PropDetails.From(partialResult, propName);
        }
    }

    /// <summary>
    /// Computes the type for an appsettings item of the array kind.
    /// Possibly causes class code generation in downstream calls.
    /// </summary>
    private PartialPropDetails GetTypeOfArrayJsonItem(
        JsonArrayType jsonStructure, string @namespace, string className)
    {
        PartialPropDetails result = jsonStructure.ItemType switch
        {
            JsonPrimitiveType primitive => GetTypeOfPrimitiveJsonItem(primitive),
            JsonArrayType array => GetTypeOfArrayJsonItem(array, @namespace, className),
            JsonObjectType obj => GenerateAndGetTypeOfObjectJsonItem(obj, @namespace, className),
            _ => new(PropType: UNDEFINED_CLASS_NAME, RequiredNamespace: baseNamespace),
        };

        result.PropType += "[]";
        return result;
    }

    /// <summary>
    /// Computes the type for an appsettings item of the primitive kind.
    /// </summary>
    private PartialPropDetails GetTypeOfPrimitiveJsonItem(JsonPrimitiveType jsonStructure)
    {
        return jsonStructure switch
        {
            { Kind: JsonValueKind.String } => new(Const.StringType, RequiredNamespace: null),
            { Kind: JsonValueKind.Number } => new(Const.IntType, RequiredNamespace: null),
            { Kind: JsonValueKind.True } => new(Const.BoolType, RequiredNamespace: null),
            { Kind: JsonValueKind.False } => new(Const.BoolType, RequiredNamespace: null),
            _ => new(PropType: UNDEFINED_CLASS_NAME, RequiredNamespace: baseNamespace),
        };
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