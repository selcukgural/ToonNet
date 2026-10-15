using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;
using ToonNet.Core.Validation;

namespace ToonNet.Tests.Security;

/// <summary>Documents and values built to make the core slow, large or wrong, and what it does with them.</summary>
public class UntrustedInputTests
{
    private static void Fast(Action action, int milliseconds = 1_000)
    {
        var watch = Stopwatch.StartNew();
        action();
        Assert.True(watch.ElapsedMilliseconds < milliseconds, $"took {watch.ElapsedMilliseconds} ms");
    }

    private static long Allocated(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static async IAsyncEnumerable<string> Async(params string[] items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    [Fact]
    public void Validate_LongKeyWithManyMembers_UsesMemoryInProportionToTheDocument()
    {
        var toon = new StringBuilder(new string('k', 80_000)).Append(":\n");

        for (var i = 0; i < 16_000; i++)
        {
            toon.Append("  c").Append(i).Append(": 1\n");
        }

        var document = ToonDocument.Parse(toon.ToString());

        var allocated = Allocated(() => Assert.True(ToonValidator.Validate(document).IsValid));
        Assert.True(allocated < 20_000_000, $"{allocated / 1_000_000} MB for a {toon.Length / 1000} KB document");
    }

    [Fact]
    public void Validate_DepthErrors_StillNameThePath()
    {
        var document = ToonDocument.Parse("a:\n  \"b c\":\n    d[1]:\n      - x: 1");

        var error = Assert.Single(ToonValidator.Validate(document, new ToonOptions { MaxDepth = 2 }).Errors);
        Assert.Equal("$.a['b c'].d", error.Path); // the array is the third level below the root
    }

    [Theory]
    [InlineData("0.00000000000000012345678901234567")]
    [InlineData("6.250879627769291e-15")]
    [InlineData("1.234567890123456789012345678901")]
    public void NumbersBeyondDecimalPrecision_AreNotRoundedThroughTheTree(string number)
    {
        var value = double.Parse(number, System.Globalization.CultureInfo.InvariantCulture);
        var root = ToonDocument.Parse("n: " + number).Root;

        Assert.Equal(value, root["n"]!.GetValue<double>());
        var encoded = new ToonEncoder().Encode(new ToonDocument(root));
        Assert.Equal(value, double.Parse(encoded["n: ".Length..], System.Globalization.CultureInfo.InvariantCulture));

        using var reader = ToonDocumentReader.Create(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("n: " + number)));

        while (reader.Read() && reader.TokenType != ToonTokenType.Number)
        {
        }

        Assert.False(reader.TryGetDecimal(out _));
    }

    [Fact]
    public void NumbersThatDifferBeyondDecimalPrecision_AreDifferent()
    {
        var a = ToonDocument.Parse("n: 6.250879627769291e-15").Root;
        var b = ToonDocument.Parse("n: 6.250879627769299e-15").Root;

        Assert.False(ToonValue.DeepEquals(a, b));
        Assert.NotEmpty(ToonMergePatch.Create(a, b).AsObject().Properties);

        // Numbers a decimal holds keep it, 29 digits included
        Assert.Equal(12345678901234567890.123456789m, ToonDocument.Parse("n: 12345678901234567890.123456789").Root["n"]!.GetValue<decimal>());
        Assert.Equal("1.10", ToonDocument.Parse("n: 1.10").Root["n"]!.GetValue<decimal>().ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Encoding_CyclicOrVeryDeepTrees_ThrowInsteadOfEndingTheProcess()
    {
        var cyclic = new ToonObject();
        cyclic["a"] = cyclic;
        cyclic["b"] = cyclic;

        Assert.Throws<ToonEncodingException>(() => new ToonEncoder().Encode(new ToonDocument(cyclic)));

        // Arrays of two objects nested a hundred thousand levels: each level looks like a table with a column group
        ToonValue deep = new ToonObject { ["v"] = new ToonNumber(1) };

        for (var i = 0; i < 100_000; i++)
        {
            deep = new ToonObject { ["g"] = deep };
        }

        var rows = new ToonArray([deep, deep]);
        Assert.Throws<ToonEncodingException>(() => new ToonEncoder().Encode(new ToonDocument(new ToonObject { ["rows"] = rows })));
    }

    [Fact]
    public void DeepJson_IsADepthError_InsteadOfEndingTheProcess()
    {
        JsonNode node = new JsonObject();

        for (var i = 0; i < 100_000; i++)
        {
            node = new JsonObject { ["a"] = node };
        }

        Assert.Throws<ToonSerializationException>(() => ToonSerializer.Serialize(node));

        var json = string.Concat(Enumerable.Repeat("[", 50_000)) + string.Concat(Enumerable.Repeat("]", 50_000));
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 100_000 });
        Assert.Throws<ToonSerializationException>(() => ToonSerializer.Serialize(document.RootElement));
    }

    private sealed class Holder
    {
        public JsonElement Json { get; set; }

        public JsonNode? Node { get; set; }
    }

    [Fact]
    public void TreesDeeperThanJsonsDefault_ReadIntoJsonValues()
    {
        // 70 levels: within the serializer's default depth (100), beyond System.Text.Json's (64)
        var toon = new StringBuilder("Json:\n");

        for (var i = 1; i <= 70; i++)
        {
            toon.Append(' ', 2 * i).Append("a:\n");
        }

        toon.Append(' ', 142).Append("v: 1");

        var holder = ToonSerializer.Deserialize<Holder>(toon.ToString())!;
        Assert.Equal(JsonValueKind.Object, holder.Json.ValueKind);
        Assert.NotNull(ToonSerializer.Deserialize<Holder>(toon.ToString().Replace("Json:", "Node:"))!.Node);
    }

    private sealed class Link
    {
        public Link? Next { get; set; }
    }

    private sealed class LinkConverter : ToonConverter<Link>
    {
        public override ToonValue? Write(Link value, ToonSerializerOptions options) =>
            new ToonObject { ["next"] = ToonSerializer.SerializeToValue(value.Next, options) };

        public override Link Read(ToonValue value, ToonSerializerOptions options) => new();
    }

    [Fact]
    public void ConverterThatRecursesOnACycle_ThrowsInsteadOfEndingTheProcess()
    {
        var link = new Link();
        link.Next = link;
        var options = new ToonSerializerOptions();
        options.AddConverter(new LinkConverter());

        Assert.Throws<InsufficientExecutionStackException>(() => ToonSerializer.Serialize(link, options));
    }

    [Fact]
    public async Task MultipleDocuments_AreStrictUtf8_WhateverByteOrderMarkTheyStartWith()
    {
        byte[] utf16 = [0xFF, 0xFE, .. System.Text.Encoding.Unicode.GetBytes("name: café")];

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await foreach (var _ in ToonSerializer.DeserializeDocumentsAsync<Dictionary<string, string>>(new MemoryStream(utf16)))
            {
            }
        });

        // A UTF-8 byte order mark is skipped
        var items = new List<Dictionary<string, string>?>();

        await foreach (var item in ToonSerializer.DeserializeDocumentsAsync<Dictionary<string, string>>(new MemoryStream([0xEF, 0xBB, 0xBF, .. "name: café\n\nname: b"u8])))
        {
            items.Add(item);
        }

        Assert.Equal(["café", "b"], items.Select(i => i!["name"]));
    }

    [Theory]
    [InlineData("===")]
    [InlineData("END")]
    public async Task DocumentsWithALineThatIsTheSeparator_AreNotWritten(string separator)
    {
        var options = new ToonMultiDocumentWriteOptions { Mode = ToonMultiDocumentSeparatorMode.ExplicitSeparator, DocumentSeparator = separator };

        await Assert.ThrowsAsync<ToonSerializationException>(() =>
            ToonSerializer.SerializeDocumentsAsync(Async("first", separator, "third"), new MemoryStream(), null, options).AsTask());

        var written = new MemoryStream();
        await ToonSerializer.SerializeDocumentsAsync(Async("first", separator + "x", "third"), written, null, options);
        var read = new List<string?>();
        var readOptions = new ToonMultiDocumentReadOptions { Mode = ToonMultiDocumentSeparatorMode.ExplicitSeparator, DocumentSeparator = separator };

        await foreach (var item in ToonSerializer.DeserializeDocumentsAsync<string>(new MemoryStream(written.ToArray()), null, readOptions))
        {
            read.Add(item);
        }

        Assert.Equal(["first", separator + "x", "third"], read);
    }

    private sealed class LyingStream(byte[] data) : MemoryStream(data)
    {
        public override long Length => 1L << 40;
    }

    [Fact]
    public void StreamsThatReportAHugeLength_DoNotReserveIt()
    {
        var allocated = Allocated(() => Assert.Equal(1, ToonSerializer.Deserialize<Dictionary<string, int>>(new LyingStream("a: 1"u8.ToArray()))!["a"]));

        Assert.True(allocated < 100_000_000, $"{allocated / 1_000_000} MB for a 4-byte document");
    }

    [Fact]
    public void DocumentWriter_RejectsATableWithAnInvalidField_BeforeWritingIt()
    {
        var text = new StringWriter();
        var writer = new ToonDocumentWriter(text);
        writer.WriteStartObject();

        Assert.Throws<ToonEncodingException>(() => writer.WriteStartTable("t", 1, ["ok", "bad\uD800"]));

        writer.WriteString("next", "v");
        writer.WriteEndObject();
        writer.Flush();

        Assert.Equal("next: v", text.ToString());
    }

    [Fact]
    public void VeryLongIntegers_AreRejected_BeforeTheirSlowParse()
    {
        var digits = new string('9', 1_000_000);

        Fast(() => Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<BigInteger>(digits)));
        Fast(() => Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<BigInteger>("\"" + digits + "\"")));
        Fast(() => Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Int128>(digits)));

        var allowed = new string('7', 10_000);
        Assert.Equal(BigInteger.Parse(allowed), ToonSerializer.Deserialize<BigInteger>(allowed));
    }

    [Fact]
    public void Deserialize_NullType_IsAnArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Deserialize("a: 1", (Type)null!));
    }

    private sealed class CountingStream(byte[] data) : MemoryStream(data)
    {
        public long BytesRead { get; private set; }

        public override int Read(Span<byte> buffer) => Count(base.Read(buffer));

        public override int Read(byte[] buffer, int offset, int count) => Count(base.Read(buffer, offset, count));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Count(base.Read(buffer.Span)));

        private int Count(int read)
        {
            BytesRead += read;
            return read;
        }
    }

    [Fact]
    public async Task Streaming_StopsAtContentAfterTheArray_InStrictMode()
    {
        // A million lines after the array: the first one makes the document an error
        var toon = "[2]:\n  - 1\n  - 2\n" + string.Concat(Enumerable.Repeat("z: 1234567890\n", 1_000_000));
        var stream = new CountingStream(System.Text.Encoding.UTF8.GetBytes(toon));
        var whole = Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<List<int>>(toon));

        var streamed = await Assert.ThrowsAsync<ToonParseException>(async () =>
        {
            await foreach (var _ in ToonSerializer.DeserializeAsyncEnumerable<int>(stream))
            {
            }
        });

        Assert.Equal(whole.Message, streamed.Message);
        Assert.True(stream.BytesRead < 1_000_000, $"{stream.BytesRead} of {toon.Length} bytes read");
    }
}
