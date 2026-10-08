extern alias Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ToonNet.Core.Serialization;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
/// Runs the generator on source snippets to check diagnostics and that the generated code compiles.
/// </summary>
public class GeneratorDriverTests
{
    private static (IReadOnlyList<Diagnostic> GeneratorDiagnostics, IReadOnlyList<Diagnostic> CompilationErrors, string[] GeneratedFiles)
        Run(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                         .Split(Path.PathSeparator)
                         .Select(path => MetadataReference.CreateFromFile(path))
                         .Append(MetadataReference.CreateFromFile(typeof(ToonSerializer).Assembly.Location));

        var compilation = CSharpCompilation.Create("Snippet",
                                                   [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12))],
                                                   references,
                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                                                nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new Generator::ToonNet.SourceGenerators.ToonSerializableGenerator())
                                          .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        var generated = driver.GetRunResult().GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToArray();
        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        return (diagnostics, errors, generated);
    }

    [Fact]
    public void NonPartialType_ReportsToon002()
    {
        var (diagnostics, _, generated) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            [ToonSerializable] public class User { public string Name { get; set; } = ""; }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TOON002", diagnostic.Id);
        Assert.NotEqual(Location.None, diagnostic.Location);
        Assert.Empty(generated);
    }

    [Fact]
    public void NonPartialContainingType_ReportsToon002()
    {
        var (diagnostics, _, generated) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            public class Outer { [ToonSerializable] public partial class Inner { public int A { get; set; } } }
            """);

        Assert.Equal("TOON002", Assert.Single(diagnostics).Id);
        Assert.Empty(generated);
    }

    [Fact]
    public void NoPublicConstructor_ReportsToon005_AndStillGeneratesSerialize()
    {
        var (diagnostics, errors, generated) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class Token
            {
                private Token() { }
                public string Value { get; set; } = "";
                public static void Use(Token t) => Token.Serialize(t);
            }
            """);

        Assert.Equal("TOON005", Assert.Single(diagnostics).Id);
        Assert.Empty(errors);
        Assert.Single(generated);
    }

    [Fact]
    public void NoProperties_ReportsToon003()
    {
        var (diagnostics, errors, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            [ToonSerializable] public partial class Empty { }
            """);

        Assert.Equal("TOON003", Assert.Single(diagnostics).Id);
        Assert.Empty(errors);
    }

    [Fact]
    public void UnusualShapes_Compile()
    {
        var (diagnostics, errors, generated) = Run("""
            using System;
            using System.Collections.Generic;
            using ToonNet.Core.Serialization.Attributes;

            namespace App.Models;

            public enum Level { Low, High }

            [ToonSerializable]
            public partial record Order(Guid Id, Level Level, IReadOnlyList<string> Lines, DateTimeOffset? ShippedAt = null)
            {
                public required string Customer { get; init; }
                [ToonProperty("needs \"quotes\"\n")] public string Weird { get; set; } = "";
                [Obsolete] public int Legacy { get; set; }
                public int this[int i] => i;
                public static int Shared { get; set; }
            }

            public partial class Container<TKey, TValue> where TKey : notnull
            {
                [ToonSerializable]
                public readonly partial record struct Entry(TKey Key, TValue Value)
                {
                    public string Tag { get; init; } = "";
                }
            }

            [ToonSerializable]
            public partial class WithDefaults
            {
                public WithDefaults(Level level = Level.High, decimal rate = 0.1m, double ratio = -1.5, string? name = null, char c = '\'') { }
                public Level Level { get; set; }
            }
            """);

        Assert.Empty(diagnostics);
        Assert.Empty(errors);
        Assert.Equal(3, generated.Length);
    }
}
