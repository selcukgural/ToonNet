using System.Buffers;
using System.Text;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
///     The overloads that take a ToonTypeInfo write and read through generated code only; the text and the objects must
///     be the same as with the other overloads, straight from text where generated code can do it.
/// </summary>
public partial class TypeInfoTests
{
    private static IEnumerable<ToonSerializerOptions> OptionVariants()
    {
        yield return new ToonSerializerOptions();
        yield return new ToonSerializerOptions { IgnoreNullValues = true };
        yield return new ToonSerializerOptions { IncludeReadOnlyProperties = false };
        yield return new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase };
        yield return new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase, IgnoreNullValues = true };
        yield return new ToonSerializerOptions { ToonOptions = new ToonOptions { Delimiter = '|', IndentSize = 4 } };
    }

    /// <summary>Checks every output and the value read back against the reflection-capable overloads.</summary>
    private static void AssertSame<T>(T? value, ToonTypeInfo<T> typeInfo, bool direct = true, bool directRead = true)
    {
        foreach (var options in OptionVariants())
        {
            var expected = ToonSerializer.Serialize(value, options);

            var writeFallbacks = ToonSerializer.DirectWriteFallbacks;
            Assert.Equal(expected, ToonSerializer.Serialize(value, typeInfo, options));
            Assert.Equal(direct, writeFallbacks == ToonSerializer.DirectWriteFallbacks);

            var utf8 = Encoding.UTF8.GetBytes(expected);
            Assert.Equal(utf8, ToonSerializer.SerializeToUtf8Bytes(value, typeInfo, options));

            var writer = new ArrayBufferWriter<byte>();
            ToonSerializer.Serialize(writer, value, typeInfo, options);
            Assert.Equal(utf8, writer.WrittenSpan.ToArray());

            var stream = new MemoryStream();
            ToonSerializer.Serialize(stream, value, typeInfo, options);
            Assert.Equal(utf8, stream.ToArray());

            var readFallbacks = ToonSerializer.DirectReadFallbacks;
            var read = ToonSerializer.Deserialize(expected, typeInfo, options);
            Assert.Equal(directRead, readFallbacks == ToonSerializer.DirectReadFallbacks);

            var reference = ToonSerializer.Deserialize<T>(expected, options);
            Assert.Equal(ToonSerializer.Serialize(reference, options), ToonSerializer.Serialize(read, options));
            Assert.Equal(ToonSerializer.Serialize(reference, options), ToonSerializer.Serialize(ToonSerializer.Deserialize(utf8, typeInfo, options), options));
            Assert.Equal(ToonSerializer.Serialize(reference, options),
                         ToonSerializer.Serialize(ToonSerializer.Deserialize(expected.AsSpan(), typeInfo, options), options));
            Assert.Equal(ToonSerializer.Serialize(reference, options),
                         ToonSerializer.Serialize(ToonSerializer.Deserialize(new MemoryStream(utf8), typeInfo, options), options));
        }
    }

    private static BoxSize Size(int w, int h) => new() { Width = w, Height = h };

    private static ParcelDepot Depot() => new()
    {
        Parcels = [new() { Code = "a", Box = Size(1, 2), Inner = Size(3, 4) }, new() { Code = "b", Box = Size(5, 6), Inner = Size(7, 8) }],
        Sizes = new Dictionary<string, BoxSize> { ["s"] = Size(1, 1), ["m"] = Size(2, 2) },
        Plan = new ShippingPlan { Small = Size(1, 1), Large = Size(9, 9) },
        Plans = new Dictionary<string, ShippingPlan>
        {
            ["eu"] = new() { Small = Size(1, 2), Large = Size(3, 4) },
            ["us"] = new() { Small = Size(5, 6), Large = Size(7, 8) }
        },
        Note = "ünï, \"quoted\""
    };

    private static List<WriterRow> Rows() =>
    [
        new() { Id = 1, Name = "a", Score = 1.5, Mood = Mood.Busy, When = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), Flag = true },
        new() { Id = 2, Name = null, Score = null }
    ];

    [Fact]
    public void GeneratedTypes_WriteAndReadLikeTheOtherOverloads_StraightFromText()
    {
        AssertSame(Depot(), ParcelDepot.ToonTypeInfo);
        AssertSame(new ParcelDepot(), ParcelDepot.ToonTypeInfo);
        AssertSame(Rows()[0], WriterRow.ToonTypeInfo);
        AssertSame(new Collections(), Collections.ToonTypeInfo);
        AssertSame(new ShippingPlan { Small = Size(1, 2), Large = Size(3, 4) }, ShippingPlan.ToonTypeInfo);
    }

    [Fact]
    public void ComposedTypeInfos_WriteAndReadLikeTheOtherOverloads()
    {
        AssertSame(Rows(), ToonTypeInfo.CreateList(WriterRow.ToonTypeInfo));
        AssertSame(new List<WriterRow>(), ToonTypeInfo.CreateList(WriterRow.ToonTypeInfo));
        AssertSame(Rows().ToArray(), ToonTypeInfo.CreateArray(WriterRow.ToonTypeInfo));
        AssertSame(new Dictionary<string, BoxSize> { ["a"] = Size(1, 2), ["b"] = Size(3, 4) }, ToonTypeInfo.CreateDictionary(BoxSize.ToonTypeInfo));
        AssertSame(new Dictionary<string, BoxedParcel> { ["x"] = new() { Code = "x", Box = Size(1, 1) } },
                   ToonTypeInfo.CreateDictionary(BoxedParcel.ToonTypeInfo));
        AssertSame(new List<List<BoxSize>> { new() { Size(1, 2) }, new() }, ToonTypeInfo.CreateList(ToonTypeInfo.CreateList(BoxSize.ToonTypeInfo)));
    }

    [Fact]
    public void NullValuesAndItems_MatchTheOtherOverloads()
    {
        AssertSame<ParcelDepot>(null, ParcelDepot.ToonTypeInfo);
        AssertSame<List<WriterRow>>(null, ToonTypeInfo.CreateList(WriterRow.ToonTypeInfo));
        AssertSame(new List<WriterRow?> { new(), null }, ToonTypeInfo.CreateList<WriterRow?>(WriterRow.ToonTypeInfo!));
    }

    [Fact]
    public void ValuesOnlyReflectionHandles_GoThroughTheGeneratedTree_WithTheSameText()
    {
        // An object without generated code in a property typed object: the whole document is written by the generated
        // tree-based method (read back, it is a dictionary, which needs no reflection)
        var node = new WriterNode { Name = "n", Anything = new PlainBox { X = 3 }, Rows = Rows() };
        AssertSame(node, WriterNode.ToonTypeInfo, direct: false);

        // Reading a property whose type has no generated code goes through the generated tree-based method
        AssertSame(new PlainHolder { Box = new PlainBox { X = 1 } }, PlainHolder.ToonTypeInfo, direct: false, directRead: false);

        // Primitives and null there need no reflection, so the same type is written and read straight from text
        AssertSame(new WriterNode { Name = "n", Anything = 5, Rows = Rows(), Leaf = new WriterLeaf { Numbers = [1] } }, WriterNode.ToonTypeInfo);
        AssertSame(new WriterNode { Name = "n", Anything = new List<object> { 1, "a" }, Leaf = new WriterLeaf() }, WriterNode.ToonTypeInfo);
        AssertSame(new WriterNode { Name = "n", Rows = Rows(), Leaf = new WriterLeaf { Numbers = [1] } }, WriterNode.ToonTypeInfo);
    }

    [Fact]
    public async Task AsyncStreams_MatchTheOtherOverloads()
    {
        var depot = Depot();
        var stream = new MemoryStream();

        await ToonSerializer.SerializeToStreamAsync(depot, stream, ParcelDepot.ToonTypeInfo);
        Assert.Equal(ToonSerializer.Serialize(depot), Encoding.UTF8.GetString(stream.ToArray()));

        stream.Position = 0;
        var read = await ToonSerializer.DeserializeFromStreamAsync(stream, ParcelDepot.ToonTypeInfo);
        Assert.Equal(ToonSerializer.Serialize(depot), ToonSerializer.Serialize(read));
    }

    [Fact]
    public async Task AsyncEnumerable_ReadsTheItemsOfARootArray()
    {
        var rows = Enumerable.Range(0, 500).Select(i => new WriterRow { Id = i, Name = $"n{i}", Mood = Mood.Calm }).ToList();
        var bytes = Encoding.UTF8.GetBytes(ToonSerializer.Serialize(rows));
        var read = new List<WriterRow?>();

        var fallbacks = ToonSerializer.DirectReadFallbacks;

        await foreach (var row in ToonSerializer.DeserializeAsyncEnumerable(new MemoryStream(bytes), WriterRow.ToonTypeInfo))
        {
            read.Add(row);
        }

        Assert.Equal(fallbacks, ToonSerializer.DirectReadFallbacks);
        Assert.Equal(ToonSerializer.Serialize(rows), ToonSerializer.Serialize(read));
    }

    [Fact]
    public void GetGenerated_ReturnsTheGeneratedTypeInfo()
    {
        Assert.Same(ParcelDepot.ToonTypeInfo, ToonTypeInfo.GetGenerated<ParcelDepot>());
        Assert.Equal(typeof(ParcelDepot), ParcelDepot.ToonTypeInfo.Type);
        Assert.Equal(typeof(List<BoxSize>), ToonTypeInfo.CreateList(BoxSize.ToonTypeInfo).Type);

        var ex = Assert.Throws<InvalidOperationException>(() => ToonTypeInfo.GetGenerated<DerivedWriterNode>());
        Assert.Contains("[ToonSerializable]", ex.Message);
    }

    [Fact]
    public void CaseInsensitiveReading_UsesReflection_AndIsNotSupportedWithATypeInfo()
    {
        var options = new ToonSerializerOptions { PropertyNameCaseInsensitive = true };

        var row = ToonSerializer.Deserialize<WriterRow>("id: 5\nNAME: x\nmood: busy", options)!;
        Assert.Equal((5, "x", Mood.Busy), (row.Id, row.Name, row.Mood));

        var fromTree = WriterRow.Deserialize(ToonDocument.Parse("id: 6\nname: y"), options);
        Assert.Equal((6, "y"), (fromTree.Id, fromTree.Name));

        Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize("Id: 1", WriterRow.ToonTypeInfo, options));
        Assert.Equal(ToonSerializer.Serialize(row), ToonSerializer.Serialize(row, WriterRow.ToonTypeInfo, options)); // writing is not affected
    }

    [Fact]
    public void Converters_AreNotSupported()
    {
        var options = new ToonSerializerOptions();
        options.Converters.Add(new BoxSizeConverter());

        Assert.Throws<NotSupportedException>(() => ToonSerializer.Serialize(Size(1, 2), BoxSize.ToonTypeInfo, options));
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize("Width: 1", BoxSize.ToonTypeInfo, options));
    }

    [Fact]
    public void Errors_AreReportedLikeTheOtherOverloads()
    {
        const string badSyntax = "Parcels[2]{Code}:\n  a";
        var expected = Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<ParcelDepot>(badSyntax));
        var actual = Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize(badSyntax, ParcelDepot.ToonTypeInfo));
        Assert.Equal(expected.Message, actual.Message);

        Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize("Width: wide", BoxSize.ToonTypeInfo));
        Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize("x: 1", ToonTypeInfo.CreateList(BoxSize.ToonTypeInfo)));

        var cycle = new WriterNode { Name = "a" };
        cycle.Child = cycle;
        Assert.Throws<ToonEncodingException>(() => ToonSerializer.Serialize(cycle, WriterNode.ToonTypeInfo));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Serialize(Size(1, 1), (ToonTypeInfo<BoxSize>)null!));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Deserialize((string)null!, BoxSize.ToonTypeInfo));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Deserialize((Stream)null!, BoxSize.ToonTypeInfo));
        Assert.Throws<ArgumentNullException>(() => ToonTypeInfo.CreateList<BoxSize>(null!));
    }

    /// <summary>A property of a type without generated code (handed to the reflection-based serializer).</summary>
    [ToonNet.Core.Serialization.Attributes.ToonSerializable]
    public sealed partial class PlainHolder
    {
        public PlainBox? Box { get; set; }
    }

    /// <summary>No generated code: only reflection writes and reads it.</summary>
    public sealed class PlainBox
    {
        public int X { get; set; }
    }

    private sealed class BoxSizeConverter : ToonConverter<BoxSize>
    {
        public override ToonValue Write(BoxSize? value, ToonSerializerOptions options) => new ToonString("box");

        public override BoxSize Read(ToonValue value, ToonSerializerOptions options) => new();
    }
}
