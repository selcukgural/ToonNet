using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Tests;

[ToonSerializable]
public partial class DeclaredCollections
{
    public IList<int>? Items { get; set; }
    public IEnumerable<int>? Sequence { get; set; }
    public IEnumerable<char>? Chars { get; set; }
    public IDictionary<string, int>? Map { get; set; }
}

[ToonSerializable]
public partial class NullableItems
{
    public List<string?>? Names { get; set; }
    public int?[]? Counts { get; set; }
}

/// <summary>A list type with its own converter: written as one string, never as a list.</summary>
[ToonConverter(typeof(TaggedListConverter))]
public sealed class TaggedList : List<int>;

public sealed class TaggedListConverter : ToonConverter<TaggedList>
{
    public override ToonValue Write(TaggedList? value, ToonSerializerOptions options) => new ToonString($"tagged:{value!.Count}");

    public override TaggedList Read(ToonValue value, ToonSerializerOptions options) => [];
}

/// <summary>A dictionary type the generated code does not know.</summary>
public sealed class CustomMap : Dictionary<string, int>;

/// <summary>
///     Generated code writes collections it knows (List, array, HashSet, Dictionary) itself; other runtime types behind
///     a declared interface or class go to the serializer, so the text is the reflection-based text in every path.
/// </summary>
[ToonSerializable]
public partial class KeyedCollections
{
    public Dictionary<long, int> ByLong { get; set; } = [];
    public HashSet<Guid> Ids { get; set; } = [];
    public Dictionary<string, int> ByName { get; set; } = [];
}

public class GeneratedCollectionTests
{
    [Fact]
    public void DictionariesAndSetsOfLargeKeys_GetComparersDocumentsCannotMakeCollide()
    {
        // Keys whose default hash codes are all 0 (the XOR of the halves of a 64-bit value)
        var longs = Enumerable.Range(1, 100_000).Select(i => ((long)i << 32) | (uint)i).ToList();
        var toon = "ByLong:\n" + string.Concat(longs.Select(k => $"  \"{k}\": 1\n")) + "Ids[1]: " + Guid.Empty + "\nByName:\n  a: 1";

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var text = ToonSerializer.Deserialize<KeyedCollections>(toon)!;
        var tree = ToonSerializer.DeserializeFromValue<KeyedCollections>(ToonNet.Core.Models.ToonDocument.Parse(toon).Root)!;
        // Linear: well under a second on a laptop; the default comparer takes about ten seconds (quadratic)
        Assert.True(watch.ElapsedMilliseconds < 5_000, $"took {watch.ElapsedMilliseconds} ms");

        foreach (var read in new[] { text, tree })
        {
            Assert.Equal(100_000, read.ByLong.Count);
            Assert.NotSame(EqualityComparer<long>.Default, read.ByLong.Comparer);
            Assert.NotSame(EqualityComparer<Guid>.Default, read.Ids.Comparer);
            Assert.Contains(Guid.Empty, read.Ids);
            Assert.Equal(1, read.ByName["a"]);
        }
    }

    private static void AssertAllPathsAgree(DeclaredCollections value, ToonSerializerOptions? options = null)
    {
        options ??= new ToonSerializerOptions();
        var reflection = ToonSerializer.Serialize(value, ReflectionOnly.Options(options));

        Assert.Equal(reflection, ToonSerializer.Serialize(value, options));
        Assert.Equal(reflection, new ToonEncoder(options.ToonOptions).Encode(DeclaredCollections.Serialize(value, options)));

        if (options.Converters.Count == 0)
        {
            Assert.Equal(reflection, ToonSerializer.Serialize(value, DeclaredCollections.ToonTypeInfo, options));
        }
    }

    [Fact]
    public void RuntimeTypesWithTheirOwnConverters_AreWrittenByTheSerializer()
    {
        var value = new DeclaredCollections { Items = new TaggedList { 1, 2 }, Sequence = new TaggedList { 3 } };

        AssertAllPathsAgree(value);
        Assert.Contains("tagged:2", ToonSerializer.Serialize(value));
    }

    [Fact]
    public void ConvertersForTheRuntimeType_ApplyBehindAnInterface()
    {
        var options = new ToonSerializerOptions();
        options.AddConverter(new JoinedListConverter());

        var value = new DeclaredCollections { Items = new List<int> { 1, 2, 3 } };

        AssertAllPathsAgree(value, options);
        Assert.Contains("1-2-3", ToonSerializer.Serialize(value, options));
    }

    [Fact]
    public void StringsAndOtherSequences_AreWrittenByTheirRuntimeType()
    {
        AssertAllPathsAgree(new DeclaredCollections { Chars = "abc", Sequence = Enumerable.Range(0, 3), Map = new CustomMap { ["a"] = 1 } });
        AssertAllPathsAgree(new DeclaredCollections { Items = new[] { 1, 2 }, Sequence = new HashSet<int> { 4 }, Map = new Dictionary<string, int> { ["b"] = 2 } });
    }

    [Fact]
    public void NullItemsPutIntoATree_ReadAsNull()
    {
        // ToonArray.Items is a list, so code can add null to it; generated code reads it like reflection, as null
        var tree = new ToonObject
        {
            ["Names"] = new ToonArray { Items = { new ToonString("a"), null! } },
            ["Counts"] = new ToonArray { Items = { null!, new ToonNumber(2) } }
        };

        foreach (var options in new[] { new ToonSerializerOptions(), ReflectionOnly.Options() })
        {
            var read = tree.Deserialize<NullableItems>(options)!;
            Assert.Equal(["a", null], read.Names!);
            Assert.Equal([null, 2], read.Counts!);
        }
    }

    private sealed class JoinedListConverter : ToonConverter<List<int>>
    {
        public override bool CanConvert(Type type) => type == typeof(List<int>);

        public override ToonValue Write(List<int>? value, ToonSerializerOptions options) => new ToonString(string.Join('-', value!));

        public override List<int> Read(ToonValue value, ToonSerializerOptions options) => [];
    }
}
