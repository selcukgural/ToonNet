using System.Buffers;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using Text = System.Text;

namespace ToonNet.Tests.Utf8;

/// <summary>UTF-8 output: exactly the bytes of the string output, without an intermediate string.</summary>
public class Utf8SerializationTests
{
    public sealed class Item
    {
        public int Id { get; set; }
        public string? Label { get; set; }
        public List<string>? Tags { get; set; }
    }

    public sealed class Node
    {
        public string? Name { get; set; }
        public Node? Next { get; set; }
    }

    private static List<Item> Items(int count) =>
        Enumerable.Range(0, count).Select(i => new Item { Id = i, Label = $"ğüşİöç-{i}-😀", Tags = ["a", "ünï", "x,y"] }).ToList();

    public static TheoryData<int> Counts => new() { 0, 1, 3, 2000 };

    [Theory]
    [MemberData(nameof(Counts))]
    public void Bytes_AreTheUtf8OfTheString(int count)
    {
        var items = Items(count);
        var expected = Text.Encoding.UTF8.GetBytes(ToonSerializer.Serialize(items));

        Assert.Equal(expected, ToonSerializer.SerializeToUtf8Bytes(items));
        Assert.Equal(expected, ToonSerializer.SerializeToUtf8Bytes(items, typeof(List<Item>)));
    }

    [Theory]
    [MemberData(nameof(Counts))]
    public void BufferWriters_ReceiveTheUtf8OfTheString(int count)
    {
        var items = Items(count);
        var expected = Text.Encoding.UTF8.GetBytes(ToonSerializer.Serialize(items));

        var array = new ArrayBufferWriter<byte>();
        ToonSerializer.Serialize(array, items);
        Assert.Equal(expected, array.WrittenSpan.ToArray());

        // A writer that hands out spans of exactly the requested size makes the encoder convert in many small parts
        var exact = new ExactBufferWriter();
        ToonSerializer.Serialize(exact, (object)items, typeof(List<Item>));
        Assert.Equal(expected, exact.Written);
    }

    [Theory]
    [MemberData(nameof(Counts))]
    public void Streams_ReceiveTheUtf8OfTheString(int count)
    {
        var items = Items(count);
        var expected = Text.Encoding.UTF8.GetBytes(ToonSerializer.Serialize(items));

        var stream = new MemoryStream();
        ToonSerializer.Serialize(stream, items);
        Assert.Equal(expected, stream.ToArray());

        stream = new MemoryStream();
        ToonSerializer.Serialize(stream, (object)items, typeof(List<Item>));
        Assert.Equal(expected, stream.ToArray());
        Assert.True(stream.CanWrite, "the stream is left open");
    }

    [Fact]
    public async Task AsyncStreams_InTheSystemTextJsonShape()
    {
        var items = Items(50);
        var expected = ToonSerializer.Serialize(items);

        var stream = new MemoryStream();
        await ToonSerializer.SerializeAsync(stream, items);
        Assert.Equal(expected, Text.Encoding.UTF8.GetString(stream.ToArray()));

        stream.Position = 0;
        var back = await ToonSerializer.DeserializeAsync<List<Item>>(stream);
        Assert.Equal(expected, ToonSerializer.Serialize(back));

        var typed = new MemoryStream();
        await ToonSerializer.SerializeAsync(typed, items, typeof(List<Item>));
        typed.Position = 0;
        Assert.Equal(expected, ToonSerializer.Serialize((List<Item>?)await ToonSerializer.DeserializeAsync(typed, typeof(List<Item>))));
    }

    [Fact]
    public void RoundTrip_ThroughBytes()
    {
        var items = Items(500);
        var back = ToonSerializer.Deserialize<List<Item>>(ToonSerializer.SerializeToUtf8Bytes(items))!;

        Assert.Equal(items.Select(i => i.Label), back.Select(i => i.Label));
        Assert.Equal(items[7].Tags, back[7].Tags);
    }

    [Fact]
    public void Encoder_WritesTheUtf8OfTheString()
    {
        var document = new ToonDocument(ToonSerializer.SerializeToValue(Items(300)));
        var encoder = new ToonEncoder();
        var expected = Text.Encoding.UTF8.GetBytes(encoder.Encode(document));

        Assert.Equal(expected, encoder.EncodeToUtf8Bytes(document));

        var writer = new ExactBufferWriter();
        encoder.Encode(document, writer);
        Assert.Equal(expected, writer.Written);

        Assert.Throws<ArgumentNullException>(() => encoder.EncodeToUtf8Bytes(null!));
        Assert.Throws<ArgumentNullException>(() => encoder.Encode(document, null!));
    }

    [Fact]
    public void FailedSerialization_WritesNothing()
    {
        var node = new Node { Name = "a" };
        node.Next = node;

        var writer = new ArrayBufferWriter<byte>();
        var stream = new MemoryStream();

        Assert.ThrowsAny<ToonException>(() => ToonSerializer.Serialize(writer, node));
        Assert.ThrowsAny<ToonException>(() => ToonSerializer.Serialize(stream, node));
        Assert.Equal(0, writer.WrittenCount);
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.SerializeToUtf8Bytes(1, (Type)null!));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Serialize((IBufferWriter<byte>)null!, 1));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Serialize((Stream)null!, 1));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Serialize(new MemoryStream(), 1, (Type)null!));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Serialize(new ArrayBufferWriter<byte>(), 1, (Type)null!));
    }

    /// <summary>
    ///     The buffer's chunks can split a surrogate pair; every UTF-8 output must encode the pair, not two replaced halves.
    /// </summary>
    [Fact]
    public async Task SurrogatePairsSplitBetweenChunks_AreEncodedWhole()
    {
        // The first chunk holds 16 K characters; place the pair on every offset around its end
        for (var before = 16 * 1024 - 3; before <= 16 * 1024 + 1; before++)
        {
            using var buffer = new ToonTextBuffer();
            buffer.Append('x', before).Append("😀é😀").Append('y', 40_000).Append("😀");
            var expected = Text.Encoding.UTF8.GetBytes(buffer.ToString());

            Assert.Equal(expected.Length, buffer.GetUtf8ByteCount());
            Assert.Equal(expected, buffer.ToUtf8Bytes());

            var writer = new ExactBufferWriter();
            buffer.WriteUtf8(writer);
            Assert.Equal(expected, writer.Written);

            var stream = new MemoryStream();
            buffer.WriteUtf8(stream);
            Assert.Equal(expected, stream.ToArray());

            stream = new MemoryStream();
            await buffer.WriteUtf8Async(stream, CancellationToken.None);
            Assert.Equal(expected, stream.ToArray());
        }
    }

    [Fact]
    public void EmptyBuffer_WritesNothing()
    {
        using var buffer = new ToonTextBuffer();

        Assert.Empty(buffer.ToUtf8Bytes());
        Assert.Equal(0, buffer.GetUtf8ByteCount());

        var writer = new ExactBufferWriter();
        buffer.WriteUtf8(writer);
        Assert.Empty(writer.Written);
    }

    /// <summary>A buffer writer whose spans are exactly as large as requested.</summary>
    private sealed class ExactBufferWriter : IBufferWriter<byte>
    {
        private readonly List<byte> _written = [];
        private byte[] _span = [];

        public byte[] Written => [.. _written];

        public void Advance(int count)
        {
            Assert.InRange(count, 0, _span.Length);
            _written.AddRange(_span.AsSpan(0, count));
            _span = [];
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => _span = new byte[Math.Max(sizeHint, 1)];

        public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;
    }
}
