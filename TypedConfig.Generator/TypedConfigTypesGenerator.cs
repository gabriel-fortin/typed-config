using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using org.g14.TypedConfig.Generator.CodeProduction;
using org.g14.TypedConfig.Generator.CodeProduction.Models;
using org.g14.TypedConfig.Generator.JsonParsing;
using org.g14.TypedConfig.Generator.JsonParsing.Models;

namespace org.g14.TypedConfig.Generator;

public partial class TypedConfigTypesGenerator(
    string baseNamespace,
    ImmutableHashSet<string> excludedSections,
    SourceProductionContext ctx)
{
    private const string ROOT_CLASS_NAME = "TypedConfig";
    private const string UNDEFINED_CLASS_NAME = "Undefined";

    private readonly ISourceCodeCreator code = new EfficientSourceCodeCreator(baseNamespace, ctx.CancellationToken);
    // private readonly ISourceCodeCreator code = new SimpleSourceCodeCreator(baseNamespace, ctx.CancellationToken);

    /// <summary>
    /// Reads the structure of appsettings
    /// and generates classes to match that structure
    /// </summary>
    public void ScanAppsettingsAndGenerateMatchingSourceFiles(ImmutableArray<AdditionalText> appsettingsFiles)
    {
        // add 'unknown' type; used only when something goes wrong
        code.GetUnknownTypeClass(UNDEFINED_CLASS_NAME).WriteTo(ctx);

        if (!IsFileCountValid(appsettingsFiles, out Diagnostic? diagnostic1))
        {
            ctx.ReportDiagnostic(diagnostic1!);
            code.GetErrorIndicatingClass(diagnostic1!.GetMessage(), ROOT_CLASS_NAME).WriteTo(ctx);
            return;
        }

        if (!TryReadAppsettingsFileStructure(appsettingsFiles,
                out JsonType? parsedStructure, out Diagnostic? diagnostic2))
        {
            ctx.ReportDiagnostic(diagnostic2!);
            code.GetErrorIndicatingClass(diagnostic2!.GetMessage(), ROOT_CLASS_NAME).WriteTo(ctx);
            return;
        }

        if (parsedStructure is not JsonObjectType jsonObject) return;

        // the actual generation of classes for representing appsettings items
        var locationContext = LocationContext.Init(baseNamespace, excludedSections);
        _ = GenerateAndGetTypeOfObjectJsonItem(jsonObject, ROOT_CLASS_NAME, locationContext);
    }


    public void GenerateServiceCollectionExtensionMethod()
    {
        code.GetServiceCollectionExtensionMethod(ROOT_CLASS_NAME).WriteTo(ctx);
    }

    public void GenerateExcludeFromBoolNamingConventionAttribute()
    {
        code.GetExcludeFromBoolNamingConventionAttributeClass().WriteTo(ctx);
    }

    private bool TryReadAppsettingsFileStructure(
        ImmutableArray<AdditionalText> appsettingsFiles,
        out JsonType? parsedStructure,
        out Diagnostic? diagnostic)
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

        try
        {
            JToken jsonToken = JToken.Parse(appsettingsSourceText.ToString());
            ctx.CancellationToken.ThrowIfCancellationRequested();
            parsedStructure = JsonStructureParser.Parse(jsonToken);

            diagnostic = null;
            return true;
        }
        catch (JsonReaderException ex)
        {
            parsedStructure = null;
            diagnostic = Diagnostic.Create(
                descriptor: DiagnosticDescriptors.CannotReadFile,
                location: null,
                messageArgs: [file.Path + ": " + ex.Message]);
            return false;
        }
    }

    /// <summary>
    /// Computes the type for an appsettings item of the object kind.
    /// Internally, causes code generation for that type.
    /// </summary>
    private PartialPropDetails GenerateAndGetTypeOfObjectJsonItem(JsonObjectType jsonStructure, string className,
        LocationContext loc)
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        // generate nested types (required to generate properties for this class)
        PropDetails[] propsAndTheirTypes =
            jsonStructure.Properties
                .Select(GetOrGenerateTypeOfObjectProperty)
                .ToArray();

        code.GetAppsettingsObjectClass(loc.Namespace, propsAndTheirTypes, className).WriteTo(ctx);

        return new PartialPropDetails(
            PropType: className,
            RequiredNamespace: loc.Namespace);

        // local helper function
        PropDetails GetOrGenerateTypeOfObjectProperty(KeyValuePair<string, JsonType> kvp)
        {
            string propName = kvp.Key;
            JsonType propDetails = kvp.Value;
            LocationContext propLoc = loc.Child(propName);

            PartialPropDetails partialResult = propDetails switch
            {
                JsonPrimitiveType primitive => GetTypeOfPrimitiveJsonItem(primitive),
                JsonArrayType arr => GetOrGenerateTypeOfArrayJsonItem(arr, $"{propName}ItemType", propLoc),
                JsonObjectType obj => GenerateAndGetTypeOfObjectJsonItem(obj, $"{propName}Type", propLoc),
                _ => new(PropType: UNDEFINED_CLASS_NAME, RequiredNamespace: baseNamespace),
            };

            bool excludePropFromNamingConventionCheck = propLoc.IsExcludedFromBoolConventionCheck &&
                propDetails is JsonPrimitiveType { Kind: JTokenType.Boolean };

            return PropDetails.From(partialResult, propName, excludePropFromNamingConventionCheck);
        }
    }

    /// <summary>
    /// Computes the type for an appsettings item of the array kind.
    /// Possibly causes class code generation in downstream calls.
    /// </summary>
    private PartialPropDetails GetOrGenerateTypeOfArrayJsonItem(JsonArrayType jsonStructure, string className,
        LocationContext loc)
    {
        PartialPropDetails result = jsonStructure.ItemType switch
        {
            JsonPrimitiveType primitive => GetTypeOfPrimitiveJsonItem(primitive),
            JsonArrayType array => GetOrGenerateTypeOfArrayJsonItem(array, className, loc),
            JsonObjectType obj => GenerateAndGetTypeOfObjectJsonItem(obj, className, loc),
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
            { Kind: JTokenType.String } => new(Const.StringType, RequiredNamespace: null),
            { Kind: JTokenType.Integer } => new(Const.IntType, RequiredNamespace: null),
            { Kind: JTokenType.Float } => new(Const.IntType, RequiredNamespace: null),
            { Kind: JTokenType.Boolean } => new(Const.BoolType, RequiredNamespace: null),
            _ => new(PropType: UNDEFINED_CLASS_NAME, RequiredNamespace: baseNamespace),
        };
    }

    private static bool IsFileCountValid(ImmutableArray<AdditionalText> files,
        out Diagnostic? diagnostic)
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