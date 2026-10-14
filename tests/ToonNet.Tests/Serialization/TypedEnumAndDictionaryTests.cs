using System.Collections.Immutable;
using System.Collections.ObjectModel;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     Enum properties and dictionaries of primitives are written and read without boxing; the text and the values must
///     be those of the tree-based path and of <see cref="Enum"/>'s own <c>ToString</c> and <c>Parse</c>.
/// </summary>
public class TypedEnumAndDictionaryTests
{
    public enum Plain { Zero, One, Two }

    public enum Small : byte { A = 1, B = 200 }

    public enum Signed : long { Min = long.MinValue, MinusOne = -1, Max = long.MaxValue }

    [Flags]
    public enum Access { None = 0, Read = 1, Write = 2, All = Read | Write }

    public enum Alias { First = 1, Same = 1, Other = 2 }

    public enum CaseOnly { value, Value }

    public class Holder
    {
        public Plain Plain { get; set; }
        public Small Small { get; set; }
        public Signed Signed { get; set; }
        public Access Access { get; set; }
        public Alias Alias { get; set; }
        public CaseOnly CaseOnly { get; set; }
        public Plain? Maybe { get; set; }
    }

    public static TheoryData<Holder> Holders => new()
    {
        new Holder(),
        new Holder { Plain = Plain.Two, Small = Small.B, Signed = Signed.Min, Access = Access.All, Alias = Alias.Other, CaseOnly = CaseOnly.Value, Maybe = Plain.One },
        new Holder { Plain = (Plain)7, Small = (Small)3, Signed = (Signed)5, Access = (Access)5, Alias = Alias.Same, CaseOnly = CaseOnly.value },
        new Holder { Access = Access.Read | (Access)8, Signed = Signed.MinusOne }
    };

    private static string TreeText<T>(T value, ToonSerializerOptions? options = null)
    {
        options ??= new ToonSerializerOptions();
        return new ToonEncoder(options.ToonOptions).Encode(new ToonDocument(ToonSerializer.SerializeToValue(value, typeof(T), options)));
    }

    [Theory]
    [MemberData(nameof(Holders))]
    public void EnumProperties_AreWrittenAsTheirToString(Holder holder)
    {
        var text = ToonSerializer.Serialize(holder);
        var root = (ToonObject)ToonDocument.Parse(text).Root;

        Assert.Equal(holder.Plain.ToString(), ((ToonString)root["Plain"]!).Value);
        Assert.Equal(holder.Small.ToString(), ((ToonString)root["Small"]!).Value);
        Assert.Equal(holder.Signed.ToString(), ((ToonString)root["Signed"]!).Value);
        Assert.Equal(holder.Access.ToString(), ((ToonString)root["Access"]!).Value);
        Assert.Equal(holder.Alias.ToString(), ((ToonString)root["Alias"]!).Value);
        Assert.Equal(holder.CaseOnly.ToString(), ((ToonString)root["CaseOnly"]!).Value);

        // Tables write the same cells, and the tree writes the same text
        List<Holder> rows = [holder, holder];
        Assert.Equal(TreeText(rows), ToonSerializer.Serialize(rows));
        Assert.Equal(TreeText(holder), text);
    }

    [Theory]
    [MemberData(nameof(Holders))]
    public void EnumProperties_ReadBack(Holder holder)
    {
        var read = ToonSerializer.Deserialize<Holder>(ToonSerializer.Serialize(holder))!;

        // Names that differ only in case read as Enum.Parse with ignoreCase does (the first of them)
        Assert.Equal(Enum.Parse<CaseOnly>(holder.CaseOnly.ToString(), ignoreCase: true), read.CaseOnly);
        read.CaseOnly = holder.CaseOnly;
        Assert.Equivalent(holder, read, strict: true);
    }

    [Theory]
    [InlineData("Plain: Two", Plain.Two)]
    [InlineData("Plain: two", Plain.Two)]
    [InlineData("Plain: TWO", Plain.Two)]
    [InlineData("Plain: \"1\"", Plain.One)]
    [InlineData("Plain: 2", Plain.Two)]
    [InlineData("Plain: \" Two \"", Plain.Two)]
    public void EnumNames_AreReadLikeEnumParse(string toon, Plain expected)
    {
        Assert.Equal(expected, ToonSerializer.Deserialize<Holder>(toon)!.Plain);
        Assert.Equal(expected, ToonSerializer.DeserializeFromValue<Holder>(ToonDocument.Parse(toon).Root)!.Plain);
    }

    [Theory]
    [InlineData("value")]
    [InlineData("Value")]
    [InlineData("VALUE")]
    public void NamesThatDifferOnlyInCase_AreReadLikeEnumParse(string name)
    {
        var expected = Enum.Parse<CaseOnly>(name, ignoreCase: true);

        Assert.Equal(expected, ToonSerializer.Deserialize<Holder>($"CaseOnly: {name}")!.CaseOnly);
        Assert.Equal(expected, ToonSerializer.Deserialize<List<CaseOnly>>($"[1]: {name}")![0]);
    }

    [Theory]
    [InlineData("Access: \"Read, Write\"", Access.All)]
    [InlineData("Access: All", Access.All)]
    [InlineData("Alias: Same", Alias.First)]
    [InlineData("Signed: Min", Signed.Min)]
    public void FlagListsAliasesAndExtremeValues_AreRead(string toon, object expected)
    {
        var holder = ToonSerializer.Deserialize<Holder>(toon)!;
        var actual = expected switch
        {
            Access => (object)holder.Access,
            Alias => holder.Alias,
            _ => holder.Signed
        };

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnknownEnumNames_ReportTheSameError()
    {
        const string toon = "Plain: Three";

        var direct = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Holder>(toon));
        var tree = Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeFromValue<Holder>(ToonDocument.Parse(toon).Root));
        Assert.Equal(tree.Message, direct.Message);
    }

    [Fact]
    public void EnumLists_AreWrittenAndReadLikeTheTree()
    {
        List<Plain> plain = [Plain.Zero, Plain.Two, (Plain)9];
        Access[] flags = [Access.All, Access.Read | (Access)8, Access.None];
        List<Plain?> maybe = [Plain.One, null];
        List<Alias> aliases = [Alias.Same, Alias.Other];

        Assert.Equal(TreeText(plain), ToonSerializer.Serialize(plain));
        Assert.Equal(TreeText(flags), ToonSerializer.Serialize(flags));
        Assert.Equal(TreeText(maybe), ToonSerializer.Serialize(maybe));
        Assert.Equal(TreeText(aliases), ToonSerializer.Serialize(aliases));

        Assert.Equal(plain, AssertReadParity<List<Plain>>(ToonSerializer.Serialize(plain)));
        Assert.Equal(flags, AssertReadParity<Access[]>(ToonSerializer.Serialize(flags)));
        Assert.Equal(maybe, AssertReadParity<List<Plain?>>(ToonSerializer.Serialize(maybe)));
        AssertReadParity<List<Plain>>("[4]: two,\"1\",0,Two");
        AssertReadParity<HashSet<Access>>("[2]: \"Read, Write\",None");
    }

    private static T? AssertReadParity<T>(string toon)
    {
        var direct = ToonSerializer.Deserialize<T>(toon);
        var tree = ToonSerializer.DeserializeFromValue<T>(ToonDocument.Parse(toon).Root);
        Assert.Equivalent(tree, direct, strict: true);
        return direct;
    }

    [Theory]
    [InlineData("\"0\": 1\n\"-5\": 2\n\"2147483647\": 3\n\"-2147483648\": 4")]
    [InlineData("\"-0\": 1")]
    [InlineData("\"1.0\": 1")]
    [InlineData("\"1e2\": 1")]
    [InlineData("\" 7 \": 1")]
    [InlineData("\"1\": 1\n\"1\": 2")]
    public void IntegerKeys_ReadLikeTheTree(string toon)
    {
        var options = new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } };
        var direct = ToonSerializer.Deserialize<Dictionary<int, int>>(toon, options);
        var tree = ToonSerializer.DeserializeFromValue<Dictionary<int, int>>(ToonDocument.Parse(toon, options.ToonOptions).Root, options);

        Assert.Equal(tree, direct);
    }

    [Theory]
    [InlineData("\"007\": 1")]
    [InlineData("\"2147483648\": 1")]
    [InlineData("\"x\": 1")]
    [InlineData("\"\": 1")]
    [InlineData("\"1\": x")]
    public void InvalidEntries_ReportTheTreeError(string toon)
    {
        var direct = Assert.ThrowsAny<ToonException>(() => ToonSerializer.Deserialize<Dictionary<int, int>>(toon));
        var tree = Assert.ThrowsAny<ToonException>(() => ToonSerializer.DeserializeFromValue<Dictionary<int, int>>(ToonDocument.Parse(toon).Root));

        Assert.Equal(tree.GetType(), direct.GetType());
        Assert.Equal(tree.Message, direct.Message);
    }

    [Fact]
    public void DictionariesOfPrimitives_ReadLikeTheTree()
    {
        var guid = Guid.NewGuid();

        AssertReadParity<Dictionary<long, double>>("\"-9223372036854775808\": 1.5\n\"9223372036854775807\": -2");
        AssertReadParity<Dictionary<Guid, string>>($"\"{guid}\": a\n\"{guid:N}\": b");
        AssertReadParity<Dictionary<string, int?>>("a: 1\nb: null");
        AssertReadParity<Dictionary<string, DateTime>>("a: \"2026-10-14T10:00:00Z\"");
        AssertReadParity<Dictionary<string, string>>("a: \"\"\nb: null\nc: 12");
        AssertReadParity<Dictionary<string, Plain>>("a: Two\nb: one\nc: 0");
        AssertReadParity<IReadOnlyDictionary<int, bool>>("\"1\": true\n\"2\": false");
        AssertReadParity<SortedDictionary<int, int>>("\"3\": 1\n\"1\": 2");
        AssertReadParity<ImmutableDictionary<string, int>>("a: 1\nb: 2");
        AssertReadParity<Dictionary<int, List<int>>>("\"1\"[2]: 1,2");
        AssertReadParity<Dictionary<string, Holder>>("a:\n  Plain: One");
    }

    public static TheoryData<object> WrittenDictionaries => new()
    {
        new Dictionary<int, int> { [0] = 0, [-1] = 1, [int.MaxValue] = int.MinValue },
        new Dictionary<long, double> { [long.MinValue] = double.NaN, [1] = 1.5, [2] = double.PositiveInfinity, [3] = -0.0 },
        new Dictionary<short, decimal> { [short.MinValue] = 1.10m },
        new Dictionary<byte, float> { [255] = 0.1f },
        new Dictionary<sbyte, long> { [-128] = long.MaxValue },
        new Dictionary<ushort, ulong> { [ushort.MaxValue] = ulong.MaxValue },
        new Dictionary<uint, char> { [uint.MaxValue] = '"' },
        new Dictionary<ulong, bool> { [ulong.MaxValue] = true },
        new Dictionary<Guid, DateTimeOffset> { [Guid.Empty] = new DateTimeOffset(2026, 10, 14, 1, 2, 3, TimeSpan.FromHours(3)) },
        new Dictionary<string, string?> { ["a b"] = "x, y", ["c"] = null, ["true"] = "true", [""] = "" },
        new Dictionary<string, Guid> { ["id"] = Guid.Empty },
        new Dictionary<string, int?> { ["a"] = 1, ["b"] = null },
        new Dictionary<string, Plain> { ["a"] = Plain.Two, ["b"] = (Plain)9 },
        new SortedDictionary<int, string> { [2] = "b", [1] = "a" },
        new ReadOnlyDictionary<int, int>(new Dictionary<int, int> { [1] = 2 }),
        ImmutableDictionary<string, int>.Empty.Add("k", 1),
        new Dictionary<int, int>(),
        new Holder2 { Map = new Dictionary<int, double> { [1] = 0.5 }, Items = [new Dictionary<int, int> { [1] = 1 }, new Dictionary<int, int> { [2] = 2, [3] = 3 }] }
    };

    public class Holder2
    {
        public Dictionary<int, double>? Map { get; set; }
        public List<Dictionary<int, int>>? Items { get; set; }
    }

    [Theory]
    [MemberData(nameof(WrittenDictionaries))]
    public void DictionariesOfPrimitives_AreWrittenLikeTheTree(object value)
    {
        foreach (var options in new[]
                 {
                     new ToonSerializerOptions(), new ToonSerializerOptions { IgnoreNullValues = true },
                     new ToonSerializerOptions { ToonOptions = new ToonOptions { Delimiter = '|', IndentSize = 4 } }
                 })
        {
            var tree = new ToonEncoder(options.ToonOptions).Encode(new ToonDocument(ToonSerializer.SerializeToValue(value, value.GetType(), options)));
            Assert.Equal(tree, ToonSerializer.Serialize(value, value.GetType(), options));
        }
    }

    [Fact]
    public void Converters_StillApplyToDictionaryValues()
    {
        var options = new ToonSerializerOptions();
        options.AddConverter(new HexConverter());

        Assert.Equal("\"1\": 0xff", ToonSerializer.Serialize(new Dictionary<int, int> { [1] = 255 }, options));
        Assert.Equal(255, ToonSerializer.Deserialize<Dictionary<int, int>>("\"1\": \"0xff\"", options)![1]);
    }

    private sealed class HexConverter : ToonConverter<int>
    {
        public override ToonValue Write(int value, ToonSerializerOptions options) => new ToonString($"0x{value:x}");

        // Keys go through the converter too
        public override int Read(ToonValue value, ToonSerializerOptions options) =>
            ((ToonString)value).Value is var text && text.StartsWith("0x") ? Convert.ToInt32(text[2..], 16) : int.Parse(text);
    }
}
