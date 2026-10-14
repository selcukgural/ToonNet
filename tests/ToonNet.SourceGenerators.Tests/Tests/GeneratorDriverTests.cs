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
        Run(string source, bool allowUnsafe = false)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                         .Split(Path.PathSeparator)
                         .Select(path => MetadataReference.CreateFromFile(path))
                         .Append(MetadataReference.CreateFromFile(typeof(ToonSerializer).Assembly.Location));

        var compilation = CSharpCompilation.Create("Snippet",
                                                   [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12))],
                                                   references,
                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                                                nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: allowUnsafe));

        var driver = CSharpGeneratorDriver.Create(new Generator::ToonNet.SourceGenerators.ToonSerializableGenerator())
                                          .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        var generated = driver.GetRunResult().GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToArray();
        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        LastGeneratedSources = driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()).ToArray();
        LastCompilationWarnings = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

        return (diagnostics, errors, generated);
    }

    private static string[] LastGeneratedSources = [];

    private static List<Diagnostic> LastCompilationWarnings = [];

    [Theory]
    [InlineData("older major", true)]
    [InlineData("next major", true)]
    [InlineData("same minor, later patch", false)]
    [InlineData("later minor", false)]
    public void ACoreOfAnotherVersion_IsReported_InsteadOfErrorsInGeneratedCode(string kind, bool mismatch)
    {
        // An assembly named ToonNet.Core with the attribute, as another version of the package would be
        var generator = typeof(Generator::ToonNet.SourceGenerators.ToonSerializableGenerator).Assembly.GetName().Version!;
        var coreVersion = kind switch
        {
            "older major" => $"{generator.Major - 1}.9.0",
            "next major" => $"{generator.Major + 1}.0.0",
            "same minor, later patch" => $"{generator.Major}.{generator.Minor}.{generator.Build + 1}",
            _ => $"{generator.Major}.{generator.Minor + 1}.0"
        };
        var version = coreVersion + ".0";
        var core = CSharpCompilation.Create("ToonNet.Core",
                                            [CSharpSyntaxTree.ParseText($$"""
                                                [assembly: System.Reflection.AssemblyVersion("{{version}}")]
                                                namespace ToonNet.Core.Serialization.Attributes { public sealed class ToonSerializableAttribute : System.Attribute { } }
                                                """)],
                                            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
                                            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var image = new MemoryStream();
        Assert.True(core.Emit(image).Success);

        var compilation = CSharpCompilation.Create("Snippet",
                                                   [CSharpSyntaxTree.ParseText("[ToonNet.Core.Serialization.Attributes.ToonSerializable] public partial class A { public int X { get; set; } }")],
                                                   [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromImage(image.ToArray())],
                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new Generator::ToonNet.SourceGenerators.ToonSerializableGenerator())
                                          .RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        if (mismatch)
        {
            var diagnostic = Assert.Single(diagnostics, d => d.Id == "TOON011");
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal($"ToonNet.SourceGenerators {generator.Major}.{generator.Minor}.{generator.Build} needs ToonNet.Core {generator.Major}.{generator.Minor}.{generator.Build} " +
                         $"or a later {generator.Major}.x version, but the project references ToonNet.Core {coreVersion}; " +
                         "reference the same version of both packages", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
            Assert.Empty(driver.GetRunResult().GeneratedTrees); // no generated code full of errors
        }
        else
        {
            Assert.DoesNotContain(diagnostics, d => d.Id == "TOON011");
            Assert.NotEmpty(driver.GetRunResult().GeneratedTrees);
        }
    }

    [Fact]
    public void KeywordPropertyNames_Compile()
    {
        var (diagnostics, errors, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class Keywords
            {
                public int @class { get; set; }
                public string? @event { get; set; }
                public System.Collections.Generic.List<int> @int { get; set; } = new();
            }
            [ToonSerializable] public partial record Positional(int @class, string @namespace);
            """);

        Assert.Empty(errors);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void MembersNamedLikeGeneratedMethods_LeaveThemOut_WithToon009()
    {
        var (diagnostics, errors, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class Settings
            {
                public bool Serialize { get; set; }
                public string Deserialize = "";
                public int Value { get; set; }
            }
            public static class Use
            {
                public static object Info => Settings.ToonTypeInfo;
            }
            """);

        Assert.Empty(errors);
        Assert.Equal(2, diagnostics.Count(d => d.Id == "TOON009"));
        Assert.DoesNotContain("static global::ToonNet.Core.Models.ToonDocument Serialize(", LastGeneratedSources.Single());
    }

    [Theory]
    [InlineData("public static partial class Holder { public static int X { get; set; } }", "static class")]
    [InlineData("public ref partial struct Span { public int X { get; set; } }", "ref struct")]
    [InlineData("file partial class Local { public int X { get; set; } }", "file-local")]
    public void UnsupportedTypeKinds_ReportToon010_AndGenerateNothing(string declaration, string reason)
    {
        var (diagnostics, errors, generated) = Run($$"""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] {{declaration}}
            """);

        Assert.Empty(errors);
        var diagnostic = Assert.Single(diagnostics, d => d.Id == "TOON010");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains(reason, diagnostic.GetMessage());
        Assert.Empty(generated);
    }

    [Fact]
    public void MethodsWithTheGeneratedSignature_ReportToon009_OtherOverloadsDoNot()
    {
        var (diagnostics, errors, _) = Run("""
            using ToonNet.Core.Models;
            using ToonNet.Core.Serialization;
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class Taken
            {
                public int Value { get; set; }
                public static ToonDocument Serialize(Taken value, ToonSerializerOptions? options = null) => new(new ToonObject());
                public static Taken Deserialize(ToonDocument document, ToonSerializerOptions? options = null) => new();
            }
            [ToonSerializable] public partial class Overloaded
            {
                public int Value { get; set; }
                public static string Serialize(Overloaded value, int indent) => "";
                public string Serialize() => "";
                public static Overloaded Deserialize(string text) => new();
            }
            """);

        Assert.Empty(errors);
        Assert.Equal(2, diagnostics.Count(d => d.Id == "TOON009"));
        Assert.All(diagnostics.Where(d => d.Id == "TOON009"), d => Assert.Contains("Taken", d.GetMessage()));
        Assert.Contains(LastGeneratedSources, source => source.Contains("partial class Overloaded") &&
                                                         source.Contains("static global::ToonNet.Core.Models.ToonDocument Serialize("));
    }

    [Fact]
    public void UnrelatedEdits_ReuseTheGeneratedOutput()
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                         .Split(Path.PathSeparator)
                         .Select(path => MetadataReference.CreateFromFile(path))
                         .Append(MetadataReference.CreateFromFile(typeof(ToonSerializer).Assembly.Location));
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
        var compilation = CSharpCompilation.Create("Snippet",
                                                   [CSharpSyntaxTree.ParseText("""
                                                        using ToonNet.Core.Serialization.Attributes;
                                                        namespace App;
                                                        [ToonSerializable] public partial class Person { public string Name { get; set; } = ""; public int Age { get; set; } }
                                                        """, parseOptions)],
                                                   references,
                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create([new Generator::ToonNet.SourceGenerators.ToonSerializableGenerator().AsSourceGenerator()],
                                                              parseOptions: parseOptions,
                                                              driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None,
                                                                                                        trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("namespace App; public class Other { public int X; }",
                                                                                            parseOptions)));

        var outputs = driver.GetRunResult().Results.Single().TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs).ToList();
        Assert.NotEmpty(outputs);
        Assert.All(outputs, output => Assert.True(output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
                                                  output.Reason.ToString()));
    }

    [Fact]
    public void TypesWhoseNamesDifferOnlyInCaseOrSymbols_AllGetCode()
    {
        var (diagnostics, errors, generated) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App
            {
                [ToonSerializable] public partial class Url { public int A { get; set; } }
                [ToonSerializable] public partial class URL { public int B { get; set; } }
                [ToonSerializable] public partial class G<T> { public int C { get; set; } }
                [ToonSerializable] public partial class G_T_ { public int D { get; set; } }
                [ToonSerializable] public partial class Other { public int E { get; set; } }
                public static class Use { public static object[] All => [Url.Serialize(new()), URL.Serialize(new()), G_T_.Serialize(new()), Other.Serialize(new())]; }
            }
            namespace App.@event { [ToonSerializable] public partial class X { public int F { get; set; } } }
            namespace App._event { [ToonSerializable] public partial class X { public int G { get; set; } } }
            """);

        Assert.Empty(errors);
        Assert.DoesNotContain(diagnostics, d => d.Severity >= DiagnosticSeverity.Warning);
        Assert.Equal(7, generated.Length);
        Assert.Equal(7, generated.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void KeywordTypeNames_DateTimeDefaults_AndRefStructConstructors_Compile()
    {
        var (diagnostics, errors, _) = Run("""
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class @class { public int A { get; set; } }
            public partial class Outer<@in> { [ToonSerializable] public partial class Inner { public int B { get; set; } } }
            [ToonSerializable] public partial class Dated
            {
                public Dated([Optional, DateTimeConstant(638000000000000000)] DateTime when) => When = when;
                public DateTime When { get; }
            }
            [ToonSerializable] public partial struct Spanned
            {
                public Spanned(Span<int> s) { X = s.Length; }
                public int X { get; set; }
            }
            """);

        Assert.Empty(errors);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(LastGeneratedSources, s => s.Contains("new global::System.DateTime(638000000000000000L)"));
    }

    [Fact]
    public void BidirectionalControlsInNames_AreEscapedInGeneratedLiterals()
    {
        var (_, errors, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class Labeled
            {
                [ToonProperty("a\u202Eb")] public int A { get; set; }
            }
            """);

        Assert.Empty(errors);
        var source = Assert.Single(LastGeneratedSources);
        Assert.DoesNotContain('\u202E', source);
        Assert.Contains("\\u202E", source);
    }

    [Fact]
    public void ToonInclude_LetsGeneratedCodeUseANonPublicSetter()
    {
        var (_, errors, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class Account
            {
                public bool IsAdmin { get; private set; }
                [ToonInclude] public int Logins { get; private set; }
            }
            """);

        Assert.Empty(errors);
        var source = Assert.Single(LastGeneratedSources);
        Assert.Contains(".Logins = ", source);
        Assert.DoesNotContain(".IsAdmin = ", source);
    }

    [Fact]
    public void DynamicMembers_UnreadRequiredMembers_PointerAndRefReadonlyConstructors_Compile()
    {
        var (diagnostics, errors, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class D1 { public dynamic? Value { get; set; } public System.Collections.Generic.List<dynamic> Items { get; set; } = new(); }
            [ToonSerializable] public partial class RI { [ToonIgnore] public required string Secret { get; set; } public required int Field; public int X { get; set; } }
            public class RB { public required int Q; }
            [ToonSerializable] public partial class RD : RB { public int Y { get; set; } }
            [ToonSerializable] public unsafe partial class P1 { public P1() { } public P1(int* p) { } public int A { get; set; } }
            [ToonSerializable] public partial class RR { public RR(ref readonly int x, in int y) { X = x + y; } public int X { get; } }
            """, allowUnsafe: true);

        File.WriteAllText(Path.Combine(Path.GetTempPath(), "gen-d1.cs"), string.Join("\n//////\n", LastGeneratedSources));
        Assert.Empty(errors);
        Assert.DoesNotContain(LastCompilationWarnings, w => w.Id is "CS9192" or "CS9193");
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void DerivedTypesWithoutATypeInfo_CannotUseTheBaseTypes()
    {
        var (_, errors, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class Base { public int A { get; set; } }
            [ToonSerializable] public partial class GD<T> : Base { public int Extra { get; set; } = 2; }
            public static class Use { public static object Info => GD<int>.ToonTypeInfo; }
            """);

        File.WriteAllText(Path.Combine(Path.GetTempPath(), "gen-gd.cs"), string.Join("\n//////\n", LastGeneratedSources));
        var error = Assert.Single(errors);
        Assert.Equal("CS0619", error.Id);
        Assert.Contains("has no ToonTypeInfo of its own", error.GetMessage());
    }

    [Fact]
    public void ToonTypeInfoProperty_IsGenerated_UnlessAMemberTakesTheName()
    {
        var (_, errors, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class Plain { public int A { get; set; } }
            [ToonSerializable] public partial class Derived : Plain { public int B { get; set; } }
            [ToonSerializable] public partial class Taken { public int ToonTypeInfo { get; set; } }
            public class UserBase { public static string ToonTypeInfo => "mine"; }
            [ToonSerializable] public partial class FromUserBase : UserBase { public int C { get; set; } }
            [ToonSerializable(GeneratePublicMethods = false)] public partial class Hidden { public int D { get; set; } }
            public static class Use
            {
                public static object[] All() => [Plain.ToonTypeInfo, Derived.ToonTypeInfo, Hidden.ToonTypeInfo];
            }
            """);

        Assert.Empty(errors);
        Assert.DoesNotContain(LastCompilationWarnings, w => w.Id is "CS0108" or "CS0114");

        string Source(string type) => LastGeneratedSources.Single(s => s.Contains($"partial class {type}"));
        Assert.Contains("public static global::ToonNet.Core.Serialization.ToonTypeInfo<global::App.Plain> ToonTypeInfo", Source("Plain"));
        Assert.Contains("public new static global::ToonNet.Core.Serialization.ToonTypeInfo<global::App.Derived> ToonTypeInfo", Source("Derived"));
        Assert.Contains("internal static global::ToonNet.Core.Serialization.ToonTypeInfo<global::App.Hidden> ToonTypeInfo", Source("Hidden"));
        Assert.DoesNotContain("ToonTypeInfo =>", Source("Taken"));
        Assert.DoesNotContain("ToonTypeInfo =>", Source("FromUserBase"));
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
    public void PropertyNameWithUnpairedSurrogate_ReportsToon008()
    {
        // TOON cannot represent a lone surrogate (spec 4.4 §3); the name is encoded at compile time, so it is an error there
        var (diagnostics, _, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            [ToonSerializable]
            public partial class BadName { [ToonProperty("a\uD800")] public int X { get; set; } }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TOON008", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
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

    [Fact]
    public void ReachableNonGenericTypes_RegisterWithToonSerializer()
    {
        var (_, errors, generated) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class Visible { public int A { get; set; } }
            public partial class Outer
            {
                [ToonSerializable] private partial class Hidden { public int B { get; set; } }
                [ToonSerializable] internal partial class Nested { public int C { get; set; } }
            }
            [ToonSerializable] public partial class Generic<T> { public T? Value { get; set; } }
            """);

        Assert.Empty(errors);
        Assert.Equal(4, generated.Length);

        var registered = LastGeneratedSources.Where(s => s.Contains("[global::System.Runtime.CompilerServices.ModuleInitializer]")).ToList();
        Assert.Equal(2, registered.Count);
        Assert.Contains(registered, s => s.Contains("Register<global::App.Visible>(__ToonSerializeValue, __ToonDeserializeValue, new __ToonObjectWriter(), new __ToonObjectReader())"));
        Assert.Contains(registered, s => s.Contains("Register<global::App.Outer.Nested>("));
    }

    [Fact]
    public void NonPublicToonConstructor_IsUsedByGeneratedCode()
    {
        var (diagnostics, errors, _) = Run("""
            using ToonNet.Core.Serialization.Attributes;
            namespace App;
            [ToonSerializable] public partial class Token
            {
                public Token(int unused) { Value = ""; }
                [ToonConstructor] private Token(string value) { Value = value; }
                public string Value { get; }
            }
            """);

        Assert.Empty(diagnostics);
        Assert.Empty(errors);
        Assert.Contains(LastGeneratedSources, s => s.Contains("new global::App.Token(__a0)"));
    }
}
