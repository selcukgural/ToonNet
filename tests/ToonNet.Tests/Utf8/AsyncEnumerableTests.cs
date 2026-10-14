using ToonNet.Core;
using ToonNet.Core.Serialization;
using Text = System.Text;

namespace ToonNet.Tests.Utf8;

/// <summary>DeserializeAsyncEnumerable: the items of a root array, a batch at a time.</summary>
public class AsyncEnumerableTests
{
    public sealed class Item
    {
        public int Id { get; set; }
        public string? Label { get; set; }
        public List<int>? Sizes { get; set; }
        public Item? Child { get; set; }
    }

    private static List<Item> Items(int count, bool nested) =>
        Enumerable.Range(0, count).Select(i => new Item
        {
            Id = i,
            Label = $"ğüş-{i}-😀",
            Sizes = nested ? [i, i + 1] : null,
            Child = nested && i % 3 == 0 ? new Item { Id = -i, Label = "c" } : null
        }).ToList();

    private static async Task<List<T?>> ReadAll<T>(IAsyncEnumerable<T?> items)
    {
        var list = new List<T?>();

        await foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static string Describe(List<Item?> items) => string.Join(";", items.Select(i => ToonSerializer.Serialize(i)));

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 64)]
    [InlineData(false, 32 * 1024)]
    [InlineData(true, 1)]
    [InlineData(true, 500)]
    [InlineData(true, 32 * 1024)]
    public async Task LargeArrays_ReadLikeTheWholeDocument(bool nested, int batch)
    {
        // Flat items are a tabular array, nested ones an expanded list
        var text = ToonSerializer.Serialize(Items(3000, nested));
        Assert.StartsWith(nested ? "[3000]:" : "[3000]{", text);

        var expected = Describe(ToonSerializer.Deserialize<List<Item?>>(text)!);
        var bytes = Text.Encoding.UTF8.GetBytes(text);

        Assert.Equal(expected, Describe(await ReadAll(ToonSerializer.DeserializeAsyncEnumerable<Item>(new TrickleStream(bytes, 7), new ToonSerializerOptions(), batch))));
        Assert.Equal(expected, Describe(await ReadAll(ToonSerializer.DeserializeAsyncEnumerable<Item>(new MemoryStream(bytes)))));
    }

    [Fact]
    public async Task ItemsArriveBeforeTheEndOfTheDocument()
    {
        // The reader returns items while the rest is still unread; a broken last line fails only at the end
        var text = ToonSerializer.Serialize(Items(2000, nested: false)) + "\n  x";
        var read = 0;

        var ex = await Assert.ThrowsAsync<ToonParseException>(async () =>
        {
            await foreach (var _ in ToonSerializer.DeserializeAsyncEnumerable<Item>(new MemoryStream(Text.Encoding.UTF8.GetBytes(text))))
            {
                read++;
            }
        });

        Assert.True(read > 0, "items before the error were returned");
        var whole = Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<List<Item>>(text));
        Assert.Equal(whole.Message, ex.Message);
        Assert.Equal(2002, ex.Line);
    }

    public static TheoryData<string> InvalidDocuments => new()
    {
        "[3]:\n  - 1\n  - 2",                                       // fewer items than declared
        "[1]:\n  - 1\n  - 2",                                       // more
        "[2]{a}:\n  1\n\n  2",                                      // a blank line between rows
        "[3]:\n  - 1\n\n  - 2\n  - 3",                              // a blank line between items
        "[2]{a,b}:\n  1,2\n  3",                                    // a short row
        "[2]:\n  - a: 1\n    b: 2\n  - a: \"x",                     // an unterminated string in the last item
        "[2]:\n  - 1\n  - 2\nextra: 1",                             // content after the array
        "a: 1",                                                     // not an array
        "",                                                         // empty (an object)
        "[2]: 1",                                                   // an inline array with a wrong count
        "[02]:\n  - 1\n  - 2",                                       // a length with a leading zero
        "[2]{a}:\n  1\n  b: 2"                                        // a line that is not a row (counts of the whole array)
    };

    [Theory]
    [MemberData(nameof(InvalidDocuments))]
    public async Task Errors_MatchTheWholeDocument(string toon)
    {
        var whole = Record.Exception(() => ToonSerializer.Deserialize<List<object?>>(toon));
        Assert.NotNull(whole);

        foreach (var batch in new[] { 1, 1 << 20 })
        {
            var streamed = await Record.ExceptionAsync(() =>
                ReadAll(ToonSerializer.DeserializeAsyncEnumerable<object>(new MemoryStream(Text.Encoding.UTF8.GetBytes(toon)), new ToonSerializerOptions(), batch)));

            Assert.NotNull(streamed);
            Assert.Equal(whole.GetType(), streamed.GetType());
            Assert.Equal(whole.Message, streamed.Message);
        }
    }

    /// <summary>Counts the bytes read, to see how much of the stream was read before an item or an error.</summary>
    private sealed class CountingStream(byte[] data) : MemoryStream(data)
    {
        public long BytesRead { get; private set; }

        public override int Read(Span<byte> buffer) => Count(base.Read(buffer));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Count(base.Read(buffer.Span)));

        private int Count(int read)
        {
            BytesRead += read;
            return read;
        }
    }

    [Fact]
    public async Task BlankLinesBetweenItems_DoNotHoldBackItems()
    {
        // Non-strict mode allows blank lines between items; batches are still cut, so items arrive early
        var text = "[50000]:\n" + string.Concat(Enumerable.Range(0, 50_000).Select(i => $"  - {i}\n\n"));
        var stream = new CountingStream(Text.Encoding.UTF8.GetBytes(text));
        var options = new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } };

        await foreach (var _ in ToonSerializer.DeserializeAsyncEnumerable<int>(stream, options))
        {
            break;
        }

        Assert.True(stream.BytesRead < stream.Length / 4, $"{stream.BytesRead} of {stream.Length} bytes read before the first item");
    }

    [Fact]
    public async Task BlankLineBetweenItems_IsReportedWhereItIs_InStrictMode()
    {
        var text = "[50000]:\n  - 0\n\n" + string.Concat(Enumerable.Range(1, 49_999).Select(i => $"  - {i}\n"));
        var stream = new CountingStream(Text.Encoding.UTF8.GetBytes(text));

        var ex = await Assert.ThrowsAsync<ToonParseException>(() => ReadAll(ToonSerializer.DeserializeAsyncEnumerable<int>(stream)));

        Assert.Equal(Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<List<int>>(text)).Message, ex.Message);
        Assert.True(stream.BytesRead < stream.Length / 4, $"{stream.BytesRead} of {stream.Length} bytes read before the error");
    }

    [Fact]
    public async Task NonStrictMode_AcceptsOtherCounts()
    {
        var options = new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } };
        var items = await ReadAll(ToonSerializer.DeserializeAsyncEnumerable<int>(new MemoryStream("[5]:\n  - 1\n  - 2"u8.ToArray()), options, 1));

        Assert.Equal([1, 2], items);
    }

    [Fact]
    public async Task InlineAndEmptyArrays()
    {
        Assert.Equal([1, 2, 3], await ReadAll(ToonSerializer.DeserializeAsyncEnumerable<int>(new MemoryStream("[3]: 1,2,3"u8.ToArray()))));
        Assert.Empty(await ReadAll(ToonSerializer.DeserializeAsyncEnumerable<int>(new MemoryStream("[]"u8.ToArray()))));
        Assert.Empty(await ReadAll(ToonSerializer.DeserializeAsyncEnumerable<int>(new MemoryStream("[0]:"u8.ToArray()))));
        Assert.Empty(await ReadAll(ToonSerializer.DeserializeAsyncEnumerable<Item>(new MemoryStream("null"u8.ToArray()))));
    }

    [Fact]
    public async Task CommentsByteOrderMarkAndCrLf_AreHandled()
    {
        var text = "﻿# items\r\n\r\n[2]{Id,Label}:\r\n  # first\r\n  1,a\r\n  2,b\r\n";
        var items = await ReadAll(ToonSerializer.DeserializeAsyncEnumerable<Item>(new TrickleStream(Text.Encoding.UTF8.GetBytes(text), 1),
                                                                                new ToonSerializerOptions(), 1));

        Assert.Equal(["a", "b"], items.Select(i => i!.Label));
    }

    [Fact]
    public async Task Cancellation_StopsReading()
    {
        var bytes = Text.Encoding.UTF8.GetBytes(ToonSerializer.Serialize(Items(1000, nested: false)));
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in ToonSerializer.DeserializeAsyncEnumerable<Item>(new TrickleStream(bytes, 64), cancellationToken: cancellation.Token))
            {
                cancellation.Cancel();
            }
        });
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.DeserializeAsyncEnumerable<Item>((Stream)null!));
    }

    #region DeserializeAsyncEnumerableAt

    /// <summary>Puts a root document in an envelope, as the value of <c>$.data.rows</c>, with siblings around it.</summary>
    private static string Envelope(string root, int indent = 2)
    {
        var pad = new string(' ', indent);
        var lines = root.Split('\n').Select((line, i) => i == 0 ? pad + "rows" + line : line.Length == 0 ? line : pad + line);

        return $"""
                meta:
                {pad}rows: 1
                {pad}data:
                {pad}{pad}rows[1]: x
                data:
                {pad}count: 2
                {pad}other:
                {pad}{pad}rows[2]: a,b
                {string.Join('\n', lines)}
                {pad}after:
                {pad}{pad}x: 1
                tail: 2
                """;
    }

    [Theory]
    [InlineData(false, 1, 2)]
    [InlineData(false, 64, 2)]
    [InlineData(true, 1, 2)]
    [InlineData(true, 500, 4)]
    [InlineData(true, 32 * 1024, 2)]
    public async Task NestedArrays_ReadLikeDeserializeAt(bool nested, int batch, int indent)
    {
        var options = new ToonSerializerOptions { ToonOptions = new ToonOptions { IndentSize = indent } };
        var root = ToonSerializer.Serialize(Items(500, nested), options);
        var text = Envelope(root, indent);
        var expected = Describe(ToonSerializer.DeserializeAt<List<Item?>>(text, "$.data.rows", options)!);
        Assert.Equal(500, ToonSerializer.DeserializeAt<List<Item?>>(text, "$.data.rows", options)!.Count);
        var bytes = Text.Encoding.UTF8.GetBytes(text);

        Assert.Equal(expected, Describe(await ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new TrickleStream(bytes, 7), "$.data.rows", options, batch))));
        Assert.Equal(expected, Describe(await ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new MemoryStream(bytes), "$.data.rows", options))));
        Assert.Equal(expected, Describe(await ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new MemoryStream(bytes), "$['data']['rows']", options))));
    }

    [Theory]
    [InlineData("[3]: 1,2,3")]
    [InlineData("[0]:")]
    [InlineData(": []")]
    [InlineData("[2]{Id,Label}:\n  1,a\n  # note\n  2,b")]
    [InlineData("[2|]{Id|Label}:\n  1|a\n  2|b")]
    [InlineData("[2]:\n  - Id: 1\n    Sizes[2]: 1,2\n  - Id: 2\n    Child:\n      Id: 3")]
    public async Task Forms_ReadLikeDeserializeAt(string root)
    {
        var text = Envelope(root);
        var expected = ToonSerializer.Serialize(ToonSerializer.DeserializeAt<List<object?>>(text.Replace("rows: []", "rows[0]:"), "$.data.rows"));

        foreach (var batch in new[] { 1, 1 << 20 })
        {
            Assert.Equal(expected,
                         ToonSerializer.Serialize(await ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<object>(new MemoryStream(Text.Encoding.UTF8.GetBytes(text)),
                                                                                                                  "$.data.rows", new ToonSerializerOptions(), batch))));
        }
    }

    [Fact]
    public async Task QuotedKeys_AndTheRoot()
    {
        var text = "\"the data\":\n  \"rows\"[2]{Id}:\n    1\n    2";

        Assert.Equal([1, 2], (await ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new MemoryStream(Text.Encoding.UTF8.GetBytes(text)),
                                                                                              "$['the data'].rows"))).Select(i => i!.Id));

        var root = ToonSerializer.Serialize(Items(3, false));
        Assert.Equal(3, (await ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new MemoryStream(Text.Encoding.UTF8.GetBytes(root)), "$"))).Count);
    }

    [Theory]
    [InlineData("$.missing")]
    [InlineData("$.data.missing")]
    [InlineData("$.data.count.rows")]
    [InlineData("$.meta.rows.x")]
    [InlineData("$.tail.rows")]
    public async Task Missing_GivesNoItems(string path)
    {
        var text = Envelope("[1]{Id}:\n  1");

        Assert.Empty(await ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new MemoryStream(Text.Encoding.UTF8.GetBytes(text)), path)));
        Assert.Null(ToonSerializer.DeserializeAt<List<Item>>(text, path));
    }

    [Theory]
    [InlineData("$.data.count")]
    [InlineData("$.data.after")]
    public async Task NotAnArray_Throws(string path)
    {
        var text = Envelope("[1]{Id}:\n  1");

        var ex = await Assert.ThrowsAsync<ToonSerializationException>(() =>
            ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new MemoryStream(Text.Encoding.UTF8.GetBytes(text)), path)));
        Assert.Equal($"The value at {path} is not an array", ex.Message);

        await Assert.ThrowsAsync<ToonSerializationException>(() =>
            ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new MemoryStream(Text.Encoding.UTF8.GetBytes("data:\n  rows[1:]{Id}:\n    a: 1")),
                                                                      "$.data.rows")));
    }

    [Fact]
    public async Task OtherSingularPaths_ReadTheWholeDocument()
    {
        var text = "pages[2]:\n  - rows[1]{Id}:\n      1\n  - rows[2]{Id}:\n      2\n      3";

        Assert.Equal([2, 3], (await ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new MemoryStream(Text.Encoding.UTF8.GetBytes(text)),
                                                                                              "$.pages[1].rows"))).Select(i => i!.Id));
        Assert.Throws<ArgumentException>(() => ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new MemoryStream(), "$.pages[*].rows"));
        Assert.Throws<ToonPathException>(() => ToonSerializer.DeserializeAsyncEnumerableAt<Item>(new MemoryStream(), "pages"));
    }

    [Theory]
    [MemberData(nameof(InvalidDocuments))]
    public async Task NestedErrors_MatchDeserializeAt(string root)
    {
        if (!root.StartsWith('[') || root.Contains("extra"))
        {
            return; // not an array, or content after it (a sibling in an envelope)
        }

        var text = Envelope(root);
        var whole = Record.Exception(() => ToonSerializer.DeserializeAt<List<object?>>(text, "$.data.rows"));
        Assert.NotNull(whole);

        foreach (var batch in new[] { 1, 1 << 20 })
        {
            var streamed = await Record.ExceptionAsync(() =>
                ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<object>(new MemoryStream(Text.Encoding.UTF8.GetBytes(text)), "$.data.rows",
                                                                            new ToonSerializerOptions(), batch)));

            Assert.NotNull(streamed);
            Assert.Equal(whole.GetType(), streamed.GetType());
            Assert.Equal(whole.Message, streamed.Message);
        }
    }

    [Fact]
    public async Task ReadingStops_AtTheEndOfTheArray()
    {
        var junk = string.Concat(Enumerable.Repeat("junk line that is not read\n", 100_000));
        var text = "data:\n  rows[2]{Id}:\n    1\n    2\n" + junk;
        var stream = new CountingStream(Text.Encoding.UTF8.GetBytes(text));

        Assert.Equal(2, (await ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Item>(stream, "$.data.rows"))).Count);
        Assert.True(stream.BytesRead < 100_000, $"read {stream.BytesRead} bytes");
    }

    #endregion
}
