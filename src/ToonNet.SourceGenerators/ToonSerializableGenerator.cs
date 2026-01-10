using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using ToonNet.SourceGenerators.Analysis;
using ToonNet.SourceGenerators.Generators;
using ToonNet.SourceGenerators.Utilities;

namespace ToonNet.SourceGenerators;

/// <summary>
/// Roslyn incremental source generator for ToonSerializable code generation.
/// Generates Serialize and Deserialize methods at compile-time.
/// </summary>
[Generator]
public sealed class ToonSerializableGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental generator.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register attribute to search for
        var attributeProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsSyntaxTargetForGeneration,
                transform: GetSemanticTargetForGeneration
            )
            .Where(static m => m is not null);

        // Combine with compilation
        var compilationAndClasses = context.CompilationProvider
            .Combine(attributeProvider.Collect());

        // Generate code
        context.RegisterSourceOutput(compilationAndClasses,
            (spc, source) => ExecuteGeneration(source.Left, source.Right!, spc));
    }

    /// <summary>
    /// Checks if a syntax node is a class declaration (potential target for generation).
    /// </summary>
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken ct)
    {
        return node is ClassDeclarationSyntax
        {
            AttributeLists.Count: > 0,
            Modifiers: var mods
        } && mods.Any(m => m.Text == "partial");
    }

    /// <summary>
    /// Gets the semantic target (class declaration with [ToonSerializable] attribute).
    /// </summary>
    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(
        GeneratorSyntaxContext context, CancellationToken ct)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeList in classDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute, ct).Symbol is IMethodSymbol
                    {
                        ContainingType.Name: "ToonSerializableAttribute"
                    })
                {
                    return classDecl;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Executes code generation for all collected classes.
    /// </summary>
    private static void ExecuteGeneration(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax?> classes,
        SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        var symbolAnalyzer = new SymbolAnalyzer(compilation);

        foreach (var classDecl in classes.OfType<ClassDeclarationSyntax>())
        {
            try
            {
                GenerateForClass(classDecl, compilation, symbolAnalyzer, context);
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticHelper.GenerationError,
                    Location.None,
                    classDecl.Identifier.Text,
                    ex.Message));
            }
        }
    }

    /// <summary>
    /// Generates code for a single class.
    /// </summary>
    private static void GenerateForClass(
        ClassDeclarationSyntax classDecl,
        Compilation compilation,
        SymbolAnalyzer analyzer,
        SourceProductionContext context)
    {
        var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
        var symbol = semanticModel.GetDeclaredSymbol(classDecl);

        if (symbol is not INamedTypeSymbol classSymbol)
            return;

        // Analyze the class
        var classInfo = analyzer.AnalyzeClass(classSymbol, classDecl);

        // Verify it's partial
        if (!classInfo.IsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticHelper.InvalidClassStructure,
                Location.None,
                classSymbol.Name));
            return;
        }

        // Check if we have properties
        if (classInfo.Properties.IsEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticHelper.NoProperties,
                Location.None,
                classSymbol.Name));
        }

        // Generate partial class with serialization methods
        var generated = GeneratePartialClass(classInfo);

        context.AddSource(
            $"{classSymbol.Name}.g.cs",
            generated);
    }

    /// <summary>
    /// Generates the partial class with Serialize and Deserialize methods.
    /// </summary>
    private static string GeneratePartialClass(ClassInfo classInfo)
    {
        var code = new CodeBuilder();

        code.AppendLine("// <auto-generated />");
        code.AppendLine("#nullable enable");
        code.AppendLine();
        code.AppendLine($"namespace {classInfo.Namespace};");
        code.AppendLine();
        code.AppendLine($"/// <summary>");
        code.AppendLine($"/// Generated serialization methods for {classInfo.Name}.");
        code.AppendLine($"/// </summary>");
        code.AppendLine($"public partial class {classInfo.Name}");
        code.BeginBlock("");

        // Generate Serialize method
        var serializeCode = SerializeMethodGenerator.Generate(classInfo);
        code.Append(serializeCode);
        code.AppendLine();

        // Generate Deserialize method
        var deserializeCode = DeserializeMethodGenerator.Generate(classInfo);
        code.Append(deserializeCode);

        code.EndBlock();

        return code.ToString();
    }
}
