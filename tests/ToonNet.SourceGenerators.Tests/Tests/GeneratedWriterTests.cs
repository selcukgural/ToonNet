using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Tests;

public enum Mood
{
    Calm,
    Busy
}

[ToonSerializable]
public partial class WriterRow
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public double? Score { get; set; }
    public Mood Mood { get; set; }
    public DateTime When { get; set; }

    [ToonProperty("needs quote")]
    public bool Flag { get; set; }

    public int Twice => Id * 2;
}

[ToonSerializable]
public sealed partial class WriterLeaf
{
    public string? Text { get; set; }
    public List<int> Numbers { get; set; } = [];
}

[ToonSerializable]
public partial class WriterNode
{
    public string Name { get; set; } = "";
    public WriterLeaf? Leaf { get; set; }
    public WriterNode? Child { get; set; }
    public WriterRow? Row { get; set; }
    public object? Anything { get; set; }
    public List<WriterRow> Rows { get; set; } = [];
    public Dictionary<string, WriterLeaf> Leaves { get; set; } = [];
}

/// <summary>Not marked [ToonSerializable]: written with reflection when it appears where a WriterNode is declared.</summary>
public class DerivedWriterNode : WriterNode
{
    public int Extra { get; set; } = 7;
}

[ToonSerializable]
public partial class SameName
{
    [ToonProperty("x")]
    public int A { get; set; } = 1;

    [ToonProperty("x")]
    public int B { get; set; } = 2;
}

/// <summary>"Id" and "ID" share a name under the camelCase, snake_case and lowercase policies only.</summary>
[ToonSerializable]
public partial class CaseClash
{
    public int Id { get; set; } = 1;

    // ReSharper disable once InconsistentNaming
    public int ID { get; set; } = 2;
}

[Flags]
public enum Access
{
    None = 0,
    Read = 1,
    Write = 2
}

public enum Aliased
{
    First = 1,
    Same = 1,
    Other = -3
}

#pragma warning disable CS8981 // lowercase member names on purpose
public enum Keywordish
{
    @true,
    @null,
    plain
}
#pragma warning restore CS8981

[ToonSerializable]
public sealed partial class Collections
{
    public Access Access { get; set; } = Access.Read | Access.Write;
    public Access Single { get; set; } = Access.Write;
    public Access Undefined { get; set; } = (Access)8;
    public Aliased Alias { get; set; } = Aliased.Same;
    public Aliased Negative { get; set; } = Aliased.Other;
    public Keywordish Keyword { get; set; } = Keywordish.@true;
    public Keywordish? MaybeKeyword { get; set; } = Keywordish.@null;
    public List<Mood> Moods { get; set; } = [Mood.Busy, Mood.Calm];
    public int?[] Numbers { get; set; } = [1, null, 3];
    public HashSet<string> Names { get; set; } = ["a", "b c"];
    public List<string?> Empty { get; set; } = [];
    public IEnumerable<int> Lazy { get; set; } = Enumerable.Range(0, 3).Select(i => i * 2);
    public List<char> Letters { get; set; } = ['x', ':'];
    public Guid[] Ids { get; set; } = [Guid.Parse("c56a4180-65aa-42ec-a945-5fd21dec0538")];
    public List<int>? Missing { get; set; }
}

/// <summary>
/// ToonSerializer writes [ToonSerializable] types with their generated field writers; the text must match the
/// generated tree and the reflection-based writer for every option combination, without falling back.
/// </summary>
public class GeneratedWriterTests
{
    private static IEnumerable<ToonSerializerOptions> OptionVariants()
    {
        yield return new ToonSerializerOptions();
        yield return new ToonSerializerOptions { IgnoreNullValues = true };
        yield return new ToonSerializerOptions { IncludeReadOnlyProperties = false };
        yield return new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase };
        yield return new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase, IgnoreNullValues = true };
        yield return new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.LowerCase };
        yield return new ToonSerializerOptions { PropertyNamingPolicy = (PropertyNamingPolicy)42 };
        yield return new ToonSerializerOptions { ToonOptions = new ToonOptions { Delimiter = '|', IndentSize = 4 } };
    }

    private static void AssertSameText<T>(T value, Func<T, ToonSerializerOptions?, ToonNet.Core.Models.ToonDocument>? generatedTree = null)
    {
        foreach (var options in OptionVariants())
        {
            var variant = $"policy {options.PropertyNamingPolicy}, ignore nulls {options.IgnoreNullValues}, " +
                          $"read-only {options.IncludeReadOnlyProperties}, delimiter '{options.ToonOptions.Delimiter}'";
            var fallbacks = ToonSerializer.DirectWriteFallbacks;
            var direct = ToonSerializer.Serialize(value, options);
            Assert.True(fallbacks == ToonSerializer.DirectWriteFallbacks, $"fell back ({variant})");

            var reflection = ToonSerializer.Serialize(value, ReflectionOnly.Options(options));
            Assert.True(reflection == direct, $"reflection differs ({variant}):\n{reflection}\n---\n{direct}");

            if (generatedTree != null)
            {
                var tree = new ToonEncoder(options.ToonOptions).Encode(generatedTree(value, options));
                Assert.True(tree == direct, $"generated tree differs ({variant}):\n{tree}\n---\n{direct}");
            }
        }
    }

    private static List<WriterRow> Rows(bool withNulls) =>
    [
        new() { Id = 1, Name = "a", Score = 1.5, Mood = Mood.Busy, When = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), Flag = true },
        new() { Id = 2, Name = withNulls ? null : "b", Score = withNulls ? null : 2 }
    ];

    private static WriterNode Tree() => new()
    {
        Name = "root",
        Leaf = new WriterLeaf { Text = "leaf", Numbers = [1, 2] },
        Child = new WriterNode { Name = "child", Leaf = new WriterLeaf() },
        Row = Rows(false)[0],
        Anything = 5,
        Rows = Rows(true),
        Leaves = new Dictionary<string, WriterLeaf> { ["k"] = new() { Text = "v" }, ["empty"] = new() }
    };

    [Fact]
    public void Rows_MatchTheTree()
    {
        AssertSameText(Rows(false));
        AssertSameText(Rows(true));
        AssertSameText(new List<WriterRow> { new(), new() });
        AssertSameText(new List<WriterRow?> { new(), null });
        AssertSameText(Rows(true)[1], WriterRow.Serialize);
    }

    [Fact]
    public void NestedObjects_MatchTheTree()
    {
        AssertSameText(Tree(), WriterNode.Serialize);
        AssertSameText(new WriterNode(), WriterNode.Serialize);
        AssertSameText(new List<WriterNode> { Tree(), new() });
        AssertSameText(new List<WriterLeaf> { new() { Text = "a" }, new() { Text = "b", Numbers = [1] } });
        AssertSameText(new List<WriterLeaf> { new() { Text = "a" }, new() { Text = null } });
    }

    [Fact]
    public void DerivedRuntimeTypes_AreWrittenWithTheirOwnProperties()
    {
        var node = new WriterNode { Name = "n", Child = new DerivedWriterNode { Name = "d" } };

        AssertSameText(node, WriterNode.Serialize);
        AssertSameText(new List<WriterNode> { new DerivedWriterNode(), new() });
        Assert.Contains("Extra: 7", ToonSerializer.Serialize(node));
    }

    [Fact]
    public void EnumsAndPrimitiveCollections_MatchTheTree()
    {
        AssertSameText(new Collections(), Collections.Serialize);
        AssertSameText(new List<Collections> { new(), new() { Moods = [] } });

        var toon = ToonSerializer.Serialize(new Collections());
        Assert.Contains("Access: \"Read, Write\"", toon);
        Assert.Contains("Keyword: \"true\"", toon);
        Assert.Contains("Lazy[3]: 0,2,4", toon);
        Assert.Contains("Empty: []", toon);
    }

    [Fact]
    public void SharedNames_KeepTheLastValueLikeTheTree()
    {
        AssertSameText(new SameName(), SameName.Serialize);
        AssertSameText(new List<SameName> { new(), new() });
        AssertSameText(new CaseClash(), CaseClash.Serialize);
        AssertSameText(new List<CaseClash> { new(), new() });

        Assert.Equal("x: 2", ToonSerializer.Serialize(new SameName()));
        Assert.Equal("Id: 1\nID: 2", ToonSerializer.Serialize(new CaseClash()));
        Assert.Equal("id: 2", ToonSerializer.Serialize(new CaseClash(), new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase }));
    }
}
