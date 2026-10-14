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

        // Generated code calls helpers of ToonNet.Core that come with the generator's version: an older Core (or another
        // major version) would fail with errors in generated files, so the mismatch is reported once instead
        var coreVersion = context.CompilationProvider.Select(static (compilation, _) =>
            compilation.ReferencedAssemblyNames.FirstOrDefault(a => a.Name == "ToonNet.Core")?.Version);

        context.RegisterSourceOutput(coreVersion.Combine(models.Collect()), static (spc, input) =>
        {
            if (!input.Right.IsEmpty && input.Left is { } core && !Fits(core))
            {
                spc.ReportDiagnostic(Diagnostic.Create(DiagnosticHelper.CoreVersionMismatch, Location.None,
                                                       VersionText(GeneratorVersion), VersionText(core), GeneratorVersion.Major));
            }
        });

        context.RegisterSourceOutput(models.Combine(coreVersion), static (spc, input) => Execute(spc, input.Left, input.Right is not { } core || Fits(core)));
    }

    private static readonly Version GeneratorVersion = typeof(ToonSerializableGenerator).Assembly.GetName().Version ?? new Version(0, 0);

    /// <summary>The same major version as the generator, and at least its minor version.</summary>
    private static bool Fits(Version core) => core.Major == GeneratorVersion.Major && core.Minor >= GeneratorVersion.Minor;

    private static string VersionText(Version version) => $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";

    private static void Execute(SourceProductionContext context, TypeModel model, bool coreFits)
    {
        foreach (var diagnostic in model.Diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticHelper.Get(diagnostic.Id), diagnostic.ToLocation(), [.. diagnostic.Arguments]));
        }

        if (!model.Emit || !coreFits)
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
