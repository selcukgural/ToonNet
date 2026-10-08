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

        // Only the generic Entry's TKey/TValue values need reflection
        Assert.All(diagnostics, d => Assert.Equal("TOON006", d.Id));
        Assert.Equal(["Key", "Value"], diagnostics.Select(d => d.GetMessage()).Select(m => m.Split('\'')[1]).Order());
        Assert.Empty(errors);
        Assert.Equal(3, generated.Length);
    }

    [Fact]
    public void ReflectionFallback_ReportsToon006_PerMember()
    {
        var (diagnostics, errors, _) = Run("""
            using System.Collections.Generic;
            using ToonNet.Core.Serialization.Attributes;
            public class Plain { public int A { get; set; } }
            public interface IShape { }
            [ToonSerializable]
            public partial class Holder
            {
                public object? Anything { get; set; }
                public IShape? Shape { get; set; }
                public List<Plain> Plains { get; set; } = new();
                public Dictionary<Plain, int> ByPlain { get; set; } = new();
                public List<int> Fine { get; set; } = new();
            }
            """);

        Assert.Empty(errors);
        Assert.All(diagnostics, d => Assert.Equal("TOON006", d.Id));

        var messages = diagnostics.Select(d => d.GetMessage()).ToList();
        Assert.Equal(4, messages.Count);
        Assert.Contains(messages, m => m.Contains("'Anything'") && m.Contains("not known at compile time"));
        Assert.Contains(messages, m => m.Contains("'Shape'"));
        Assert.Contains(messages, m => m.Contains("'Plains'") && m.Contains("'Plain' is not marked [ToonSerializable]"));
        Assert.Contains(messages, m => m.Contains("'ByPlain'") && m.Contains("dictionary keys"));
        Assert.All(diagnostics, d => Assert.True(d.Location.GetLineSpan().StartLinePosition.Line > 0)); // points at the member
    }

    [Fact]
    public void AllowReflectionFallbackFalse_ReportsToon007()
    {
        var (diagnostics, _, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            [ToonSerializable(AllowReflectionFallback = false)]
            public partial class Strict { public object? Anything { get; set; } public int Fine { get; set; } }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TOON007", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void ConverterProperty_DoesNotReportFallback()
    {
        var (diagnostics, errors, _) = Run("""
            using ToonNet.Core.Models;
            using ToonNet.Core.Serialization;
            using ToonNet.Core.Serialization.Attributes;
            public class Plain { }
            public sealed class PlainConverter : ToonConverter<Plain>
            {
                public override ToonValue? Write(Plain? value, ToonSerializerOptions options) => new ToonString("p");
                public override Plain? Read(ToonValue value, ToonSerializerOptions options) => new Plain();
            }
            [ToonSerializable]
            public partial record Holder([property: ToonConverter(typeof(PlainConverter))] Plain Item);
            """);

        Assert.Empty(diagnostics);
        Assert.Empty(errors);
    }
}
