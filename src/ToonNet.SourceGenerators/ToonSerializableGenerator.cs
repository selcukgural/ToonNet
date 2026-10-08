using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ToonNet.SourceGenerators.Model;
using ToonNet.SourceGenerators.Utilities;

namespace ToonNet.SourceGenerators;

/// <summary>
/// Incremental source generator that adds static <c>Serialize</c> and <c>Deserialize</c> methods to every partial
/// class, struct or record marked with <c>[ToonSerializable]</c>.
/// </summary>
[Generator, ExcludeFromCodeCoverage]
public sealed class ToonSerializableGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName = "ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute";

    /// <summary>
    /// Registers the generation pipeline.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeMetadataName,
            static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            static (ctx, ct) => TypeModelBuilder.Build(ctx, ct));

        context.RegisterSourceOutput(models, static (spc, model) => Execute(spc, model));
    }

    private static void Execute(SourceProductionContext context, TypeModel model)
    {
        foreach (var diagnostic in model.Diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticHelper.Get(diagnostic.Id), diagnostic.ToLocation(), [.. diagnostic.Arguments]));
        }

        if (!model.Emit)
        {
            return;
        }

        try
        {
            context.AddSource(model.HintName, SourceEmitter.Emit(model));
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticHelper.GenerationError, Location.None, model.FullyQualifiedName, ex.Message));
        }
    }
}
