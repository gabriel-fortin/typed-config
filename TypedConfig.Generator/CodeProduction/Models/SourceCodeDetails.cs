using Microsoft.CodeAnalysis;

namespace org.g14.TypedConfig.Generator.CodeProduction.Models;

/// <summary>
/// Represents to-be-generated source code
/// </summary>
/// <param name="FileName">the proposed file name</param>
/// <param name="SourceCodeText">the source code text to put in the file</param>
public record struct SourceCodeDetails(
    string FileName,
    string SourceCodeText)
{
    /// <summary>
    /// Adds this source code to the compilation process
    /// </summary>
    public void WriteTo(SourceProductionContext ctx)
    {
        ctx.AddSource(FileName, SourceCodeText);
    }
}