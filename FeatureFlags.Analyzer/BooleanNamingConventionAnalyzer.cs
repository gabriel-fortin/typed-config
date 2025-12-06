using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace org.g14.FeatureFlags.Analyzer;

/// <summary>
/// Analyzer that checks boolean configuration values in appsettings.json follow naming conventions.
/// Boolean values should start with prefixes like "is", "has", "can", "should", etc.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BooleanNamingConventionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.BooleanNamingConvention];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for syntax node analysis to check property declarations
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        // Check if the property type is bool
        var typeInfo = context.SemanticModel.GetTypeInfo(propertyDeclaration.Type, context.CancellationToken);
        if (typeInfo.Type?.SpecialType != SpecialType.System_Boolean)
        {
            return;
        }

        // Get the property name
        var propertyName = propertyDeclaration.Identifier.Text;

        // Check if the property name starts with a valid boolean prefix
        if (!StartsWithValidBooleanPrefix(propertyName))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.BooleanNamingConvention,
                propertyDeclaration.Identifier.GetLocation(),
                propertyName);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool StartsWithValidBooleanPrefix(string propertyName)
    {
        string lowerName = propertyName.ToLowerInvariant();
        
        return Const.BooleanPrefixes.Any(prefix => 
            lowerName.StartsWith(prefix) && 
            (lowerName.Length == prefix.Length || char.IsUpper(propertyName[prefix.Length])));
    }
}

