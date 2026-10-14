using Text = System.Text;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Utf8;

/// <summary>UTF-8 and span input: the same results and errors as string input, without an intermediate string.</summary>
public class Utf8DeserializationTests
{
    private const string Toon = """
                                Name: Çağrı 😀
                                Tags[2]: ünï,"a,b"
                                Items[2]{Id,Label}:
                                  1,şeker
                                  2,"x: y"
                                """;

    public sealed class Doc
    {
        public string? Name { get; set; }
        public List<string>? Tags { get; set; }
        public List<Item>? Items { get; set; }
    }

    public sealed class Item
    {
        public int Id { get; set; }
        public string? Label { get; set; }
    }

    private static readonly byte[] Utf8 = Text.Encoding.UTF8.GetBytes(Toon);

    private static readonly string Expected = Describe(ToonSerializer.Deserialize<Doc>(Toon));

    private static string Describe(Doc? doc) =>
        $"{doc!.Name}|{string.Join(";", doc.Tags!)}|{string.Join(";", doc.Items!.Select(i => $"{i.Id}={i.Label}"))}";

    [Fact]
    public void Bytes_DeserializeLikeTheString()
    {
        Assert.Equal("Çağrı 😀|ünï;a,b|1=şeker;2=x: y", Expected);
        Assert.Equal(Expected, Describe(ToonSerializer.Deserialize<Doc>(Utf8)));
        Assert.Equal(Expected, Describe((Doc?)ToonSerializer.Deserialize(Utf8.AsSpan(), typeof(Doc))));
    }

    [Fact]
    public void CharSpan_DeserializesLikeTheString()
    {
        var padded = ("xx" + Toon + "yy").AsSpan(2, Toon.Length);

        Assert.Equal(Expected, Describe(ToonSerializer.Deserialize<Doc>(padded)));
        Assert.Equal(Expected, Describe((Doc?)ToonSerializer.Deserialize(padded, typeof(Doc))));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4096)]
    public async Task Streams_DeserializeLikeTheString(int maxRead)
    {
        Assert.Equal(Expected, Describe(ToonSerializer.Deserialize<Doc>(new TrickleStream(Utf8, maxRead))));
        Assert.Equal(Expected, Describe((Doc?)ToonSerializer.Deserialize(new TrickleStream(Utf8, maxRead), typeof(Doc))));
        Assert.Equal(Expected, Describe(await ToonSerializer.DeserializeFromStreamAsync<Doc>(new TrickleStream(Utf8, maxRead))));
        Assert.Equal(Expected, Describe((Doc?)await ToonSerializer.DeserializeFromStreamAsync(new TrickleStream(Utf8, maxRead), typeof(Doc))));
    }

    [Fact]
    public void SeekableStream_IsReadFromItsPosition_AndLeftOpen()
    {
        var stream = new MemoryStream([.. "junk"u8, .. Utf8]) { Position = 4 };

        Assert.Equal(Expected, Describe(ToonSerializer.Deserialize<Doc>(stream)));
        Assert.True(stream.CanRead);
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public async Task LargeDocuments_ReadThroughSeveralChunks()
    {
        // Larger than the 16 KB read buffer, with multi-byte characters at every chunk boundary somewhere
        var items = Enumerable.Range(0, 3000).Select(i => new Item { Id = i, Label = $"ğüşİöç-{i}-😀" }).ToList();
        var text = ToonSerializer.Serialize(new Doc { Name = "big", Tags = [], Items = items });
        var bytes = Text.Encoding.UTF8.GetBytes(text);
        Assert.True(bytes.Length > 64 * 1024);

        var expected = Describe(ToonSerializer.Deserialize<Doc>(text));

        Assert.Equal(expected, Describe(ToonSerializer.Deserialize<Doc>(bytes)));
        Assert.Equal(expected, Describe(ToonSerializer.Deserialize<Doc>(new MemoryStream(bytes))));
        Assert.Equal(expected, Describe(ToonSerializer.Deserialize<Doc>(new TrickleStream(bytes, 1000))));
        Assert.Equal(expected, Describe(await ToonSerializer.DeserializeFromStreamAsync<Doc>(new TrickleStream(bytes, 7))));
    }

    [Fact]
    public void ByteOrderMark_IsIgnored()
    {
        byte[] withBom = [0xEF, 0xBB, 0xBF, .. Utf8];

        Assert.Equal(Expected, Describe(ToonSerializer.Deserialize<Doc>(withBom)));
        Assert.Equal(Expected, Describe(ToonSerializer.Deserialize<Doc>(new TrickleStream(withBom, 1))));
        Assert.Equal("v", ((ToonString)ToonDocument.Parse([0xEF, 0xBB, 0xBF, (byte)'v']).Root).Value);
    }

    public static TheoryData<byte[]> InvalidUtf8 => new()
    {
        new byte[] { (byte)'a', (byte)':', (byte)' ', 0xFF },
        new byte[] { (byte)'a', (byte)':', (byte)' ', 0xC3 },                   // truncated sequence at the end
        new byte[] { (byte)'a', (byte)':', (byte)' ', 0xC0, 0xAF },             // overlong encoding
        new byte[] { (byte)'a', (byte)':', (byte)' ', 0xED, 0xA0, 0x80 },       // encoded surrogate
        new byte[] { (byte)'a', (byte)':', (byte)' ', 0xE2, 0x28, 0xA1, (byte)'\n', (byte)'b', (byte)':', (byte)' ', (byte)'1' }
    };

    [Theory]
    [MemberData(nameof(InvalidUtf8))]
    public async Task InvalidUtf8_IsAParseError(byte[] bytes)
    {
        Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, object>>(bytes));
        Assert.Throws<ToonParseException>(() => ToonDocument.Parse(bytes));
        Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, object>>(new MemoryStream(bytes)));
        Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, object>>(new TrickleStream(bytes, 1)));
        await Assert.ThrowsAsync<ToonParseException>(async () =>
            await ToonSerializer.DeserializeFromStreamAsync<Dictionary<string, object>>(new TrickleStream(bytes, 2)));
    }

    [Fact]
    public void ParseErrors_ReportTheSamePosition()
    {
        const string invalid = "a: 1\nitems[3]: x,y";
        var expected = Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, object>>(invalid));

        foreach (var actual in new[]
                 {
                     Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, object>>(Text.Encoding.UTF8.GetBytes(invalid))),
                     Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, object>>(invalid.AsSpan())),
                     Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, object>>(new MemoryStream(Text.Encoding.UTF8.GetBytes(invalid))))
                 })
        {
            Assert.Equal(expected.Message, actual.Message);
            Assert.Equal(expected.Line, actual.Line);
            Assert.Equal(expected.Column, actual.Column);
        }
    }

    [Fact]
    public void Converters_ReadTheTreeFromTheSameBuffer()
    {
        // With converters, the serializer reads through the tree; the decoded buffer must still be in place
        var options = new ToonSerializerOptions();
        options.Converters.Add(new PrefixConverter());

        Assert.Equal("Çağrı 😀!", ToonSerializer.Deserialize<Doc>(Utf8, options)!.Name);
        Assert.Equal("şeker!", ToonSerializer.Deserialize<Doc>(new TrickleStream(Utf8, 2), options)!.Items![0].Label);
    }

    [Fact]
    public void EmptyInput_ParsesLikeTheEmptyString()
    {
        Assert.Equal(ToonDocument.Parse("").Root.GetType(), ToonDocument.Parse(ReadOnlySpan<byte>.Empty).Root.GetType());
        Assert.Equal(ToonDocument.Parse("").Root.GetType(), ToonDocument.Parse(ReadOnlySpan<char>.Empty).Root.GetType());
    }

    [Fact]
    public async Task NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Deserialize<Doc>((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Deserialize(Utf8, null!));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Deserialize("a: 1".AsSpan(), null!));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Deserialize(new MemoryStream(Utf8), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await ToonSerializer.DeserializeFromStreamAsync(null!, typeof(Doc)));
    }

    private sealed class PrefixConverter : ToonConverter<string>
    {
        public override ToonValue Write(string? value, ToonSerializerOptions options) => new ToonString(value!);

        public override string Read(ToonValue value, ToonSerializerOptions options) => ((ToonString)value).Value + "!";
    }
}
