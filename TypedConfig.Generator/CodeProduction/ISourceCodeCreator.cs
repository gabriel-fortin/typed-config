using System.Diagnostics.Contracts;
using org.g14.TypedConfig.Generator.CodeProduction.Models;

namespace org.g14.TypedConfig.Generator.CodeProduction;

public interface ISourceCodeCreator
{
    /// <summary>
    /// Creates code for a class representing an object in appsettings
    /// </summary>
    [Pure]
    SourceCodeDetails GetAppsettingsObjectClass(
        string @namespace,
        PropDetails[] propsAndTheirTypes,
        string className);

    /// <summary>
    /// Creates code for a very basic version of a class.
    /// Having this prevents some compilation errors (because the type is there)
    /// and allows to convey some details of the problem (in addition to regular diagnostics).
    /// </summary>
    [Pure]
    SourceCodeDetails GetErrorIndicatingClass(string errorMessage, string className);

    /// <summary>
    /// The unknown type is used if the actual type for an appsettings item could not be determined.
    /// Possible causes: the generator does not support something, bug.
    /// </summary>
    [Pure]
    SourceCodeDetails GetUnknownTypeClass(string className);

    [Pure]
    SourceCodeDetails GetServiceCollectionExtensionMethod(string className);

    /// <summary>
    /// Creates code for the attribute used to exclude a boolean property from the
    /// bool naming convention analyzer.
    /// </summary>
    [Pure]
    SourceCodeDetails GetExcludeFromBoolNamingConventionAttributeClass();
}