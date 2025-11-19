using System.Diagnostics.Contracts;
using System.Web;
using org.g14.FeatureFlags.Generation.CodeProduction.Models;

namespace org.g14.FeatureFlags.Generation.CodeProduction;

/// <summary>
/// Creates text contents of source code files.
/// </summary>
public class SourceCodeCreator(
    string defaultNamespace,
    CancellationToken? cancellationToken = null
)
{
    [Pure]
    public SourceCodeDetails GetAppsettingsObjectClass(
        string @namespace,
        PropDetails[] propsAndTheirTypes,
        string className)
    {
        cancellationToken?.ThrowIfCancellationRequested();
        
        IEnumerable<string> propsLines = propsAndTheirTypes
            .Select(x => $"public required {x.PropType} {x.PropName} {{ get; set; }}");

        IEnumerable<string> usingStatements = propsAndTheirTypes
            .Select(x => x.RequiredNamespace)
            .Distinct()
            .Where(ns => ns != null)
            .Select(ns => $"using {ns};");

        // TODO: PERF: use a string builder to build the class's code

        return new(
            FileName: $"{className}.generated.cs",
            SourceCodeText:
            $$"""
              {{string.Join("\n", usingStatements)}}

              namespace {{@namespace}};

              public class {{className}}
              {
                  {{string.Join("\n    ", propsLines)}}
              }
              """);
    }

    /// <summary>
    /// Create a very basic version of a class.
    /// This prevents some compilation errors (because the type is there)
    /// and allows to convey some details of the problem (in addition to regular diagnostics).
    /// </summary>
    [Pure]
    public SourceCodeDetails GetErrorIndicatingClass(string errorMessage, string className)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        return new(
            FileName: $"{className}.generated.cs",
            SourceCodeText:
            $$"""
              namespace {{defaultNamespace}};

              public class {{className}}
              {
                  /// <summary>
                  /// {{HttpUtility.HtmlEncode(errorMessage)}}
                  /// </summary>
                  public string COMPILATION_ERROR = "File could not be generated. See the doc comment of this property for details";
              }
              """);
    }

    /// <summary>
    /// The unknown type is used if the actual type for an appsettings item could not be determined.
    /// Possible causes: the generator does not support something, bug.
    /// </summary>
    [Pure]
    public SourceCodeDetails GetUnknownTypeClass(string className)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        return new(
            FileName: $"{className}.generated.cs",
            SourceCodeText:
            $$"""
              namespace {{defaultNamespace}};

              /// <summary>
              /// The type of the item in appsettings could not be identified
              /// </summary>
              public class {{className}}
              {
              }
              """);
    }
}
