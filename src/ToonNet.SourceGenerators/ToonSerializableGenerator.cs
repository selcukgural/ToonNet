using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ToonNet.SourceGenerators.Analysis;
using ToonNet.SourceGenerators.Generators;
using ToonNet.SourceGenerators.Utilities;

namespace ToonNet.SourceGenerators;

/// <summary>
/// Provides a Roslyn incremental source generator for the ToonSerializable attribute.
/// Facilitates the generation of compile-time methods for serialization and deserialization
/// of classes that are adorned with the ToonSerializable attribute.
/// </summary>
[Generator, ExcludeFromCodeCoverage]
public sealed class ToonSerializableGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental generator.
    /// </summary>
    /// <param name="context">
    /// Provides the initialization context for registering syntax providers and source outputs in the generator.
    /// </param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register attribute to search for
        var attributeProvider = context.SyntaxProvider.CreateSyntaxProvider(IsSyntaxTargetForGeneration, GetSemanticTargetForGeneration)
                                       .Where(static m => m is not null);

        // Combine with compilation
        var compilationAndClasses = context.CompilationProvider.Combine(attributeProvider.Collect());

        // Generate code
        context.RegisterSourceOutput(compilationAndClasses, (spc, source) => ExecuteGeneration(source.Left, source.Right, spc));
    }

    /// <summary>
    /// Determines whether the specified syntax node is a class declaration that is
    /// a potential target for source generation based on its attributes and modifiers.
    /// </summary>
    /// <param name="node">
    /// The syntax node to evaluate.
    /// </param>
    /// <param name="ct">
    /// A cancellation token to observe while performing the operation.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the node is a class declaration suitable for generation.
    /// </returns>
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken ct)
    {
        return node is ClassDeclarationSyntax
        {
            AttributeLists.Count: > 0,
            Modifiers           : var mods
        } && mods.Any(m => m.Text == "partial");
    }

    /// <summary>
    /// Gets the semantic target for generation, specifically identifying class declarations
    /// annotated with the [ToonSerializable] attribute.
    /// </summary>
    /// <param name="context">
    /// The <see cref="GeneratorSyntaxContext"/> providing context about the syntax node
    /// to analyze during source generation.
    /// </param>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> to observe while executing the method.
    /// </param>
    /// <returns>
    /// A <see cref="ClassDeclarationSyntax"/> representing the class declaration with the
    /// [ToonSerializable] attribute, or null if no valid target is found.
    /// </returns>
    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken ct)
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
    /// Executes the code generation process for the provided compilation and class declarations.
    /// </summary>
    /// <param name="compilation">
    /// The current compilation context, used to analyze and process symbols.
    /// </param>
    /// <param name="classes">
    /// A collection of class declarations targeted for source generation.
    /// </param>
    /// <param name="context">
    /// The source production context for generating source files and reporting diagnostics.
    /// </param>
    private static void ExecuteGeneration(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        var symbolAnalyzer = new SymbolAnalyzer(compilation);

        foreach (var classDecl in classes.OfType<ClassDeclarationSyntax>())
        {
            try
            {
                GenerateForClass(classDecl, compilation, symbolAnalyzer, context);
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticHelper.GenerationError, Location.None, classDecl.Identifier.Text, ex.Message));
            }
        }
    }

    /// <summary>
    /// Generates source code for the given class declaration, including
    /// serialization and deserialization methods if applicable.
    /// </summary>
    /// <param name="classDecl">
    /// The class declaration syntax node to process.
    /// </param>
    /// <param name="compilation">
    /// The current Roslyn compilation object that provides semantic information.
    /// </param>
    /// <param name="analyzer">
    /// The symbol analyzer used to extract serialization metadata from the class.
    /// </param>
    /// <param name="context">
    /// The source production context used to report diagnostics and add generated sources.
    /// </param>
    private static void GenerateForClass(ClassDeclarationSyntax classDecl, Compilation compilation, SymbolAnalyzer analyzer,
                                         SourceProductionContext context)
    {
        var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
        var symbol = semanticModel.GetDeclaredSymbol(classDecl);

        if (symbol is null)
        {
            return;
        }

        // Analyze the class
        var classInfo = analyzer.AnalyzeClass(symbol, classDecl);

        // Verify it's partial
        if (!classInfo.IsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticHelper.InvalidClassStructure, Location.None, symbol.Name));
            return;
        }

        // Check if we have properties
        if (classInfo.Properties.IsEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticHelper.NoProperties, Location.None, symbol.Name));
        }

        // Generate a partial class with serialization methods
        var generated = GeneratePartialClass(classInfo);

        context.AddSource($"{symbol.Name}.g.cs", generated);
    }

    /// <summary>
    /// Generates the partial class with automatically implemented Serialize and Deserialize methods
    /// based on the specified class information.
    /// </summary>
    /// <param name="classInfo">
    /// An instance of <see cref="ClassInfo"/> that contains metadata about the class,
    /// such as its name and namespace, required for generating the partial class.
    /// </param>
    /// <returns>
    /// A string representing the generated C# code for the partial class, including
    /// its Serialize and Deserialize methods.
    /// </returns>
    private static string GeneratePartialClass(ClassInfo classInfo)
    {
        var code = new CodeBuilder();

        code.AppendLine("// <auto-generated />");
        code.AppendLine("#nullable enable");
        code.AppendLine();
        code.AppendLine($"namespace {classInfo.Namespace};");
        code.AppendLine();
        code.AppendLine("/// <summary>");
        code.AppendLine($"/// Generated serialization methods for {classInfo.Name}.");
        code.AppendLine("/// </summary>");
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