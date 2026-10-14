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
        "[2]: 1"                                                    // an inline array with a wrong count
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
}
