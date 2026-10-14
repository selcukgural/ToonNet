using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.AsyncApi;

/// <summary>
///     TOON output written to files and streams is UTF-8 without a byte order mark, so other decoders do not read
///     U+FEFF as part of the first key.
/// </summary>
public sealed class Utf8OutputTests : IDisposable
{
    private static readonly Item[] Items = [new() { Name = "ä" }, new() { Name = "b" }];

    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"toonnet-{Guid.NewGuid():N}.toon");

    public void Dispose()
    {
        File.Delete(_filePath);
    }

    private static void AssertNoBom(byte[] bytes)
    {
        Assert.NotEmpty(bytes);
        Assert.False(bytes is [0xEF, 0xBB, 0xBF, ..], "Output starts with a UTF-8 byte order mark");
        Assert.StartsWith("Name: ", System.Text.Encoding.UTF8.GetString(bytes));
    }

    /// <summary>
    ///     Text far larger than the 16 KB chunks the writers encode through, with multi-byte characters (and surrogate
    ///     pairs) at every offset, so some of them straddle a chunk boundary.
    /// </summary>
    private static Item[] LargeItems() =>
        [.. Enumerable.Range(0, 3000).Select(i => new Item { Name = new string('x', i % 7) + "çğüşöı€😀" + i })];

    [Fact]
    public async Task StreamOutput_LargerThanAChunk_MatchesTheStringOutput()
    {
        var items = LargeItems();
        var expected = System.Text.Encoding.UTF8.GetBytes(ToonSerializer.Serialize(items));
        Assert.True(expected.Length > 64 * 1024);

        using var stream = new MemoryStream();
        await ToonSerializer.SerializeToStreamAsync(items, stream);
        Assert.Equal(expected, stream.ToArray());

        await ToonSerializer.SerializeToFileAsync(items, _filePath);
        Assert.Equal(expected, await File.ReadAllBytesAsync(_filePath));

        var document = ToonDocument.Parse(ToonSerializer.Serialize(items));
        using var encoded = new MemoryStream();
        await new ToonEncoder().EncodeToStreamAsync(document, encoded);
        Assert.Equal(expected, encoded.ToArray());

        await new ToonEncoder().EncodeToFileAsync(document, _filePath);
        Assert.Equal(expected, await File.ReadAllBytesAsync(_filePath));
    }

    [Fact]
    public async Task MultiDocumentOutput_LargerThanABatch_MatchesTheStringOutput()
    {
        var items = LargeItems();
        var expected = string.Join("\n\n", items.Select(item => ToonSerializer.Serialize(item)));

        using var collection = new MemoryStream();
        await ToonSerializer.SerializeDocumentsAsync(items, collection);
        Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(collection.ToArray()));

        using var batched = new MemoryStream();
        await ToonSerializer.SerializeDocumentsAsync(ToAsync(items), batched, null, new ToonMultiDocumentWriteOptions { BatchSize = 7 });
        Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(batched.ToArray()));

        var readBack = new List<Item?>();
        batched.Position = 0;
        using var reader = new StreamReader(batched);

        await foreach (var item in ToonSerializer.DeserializeDocumentsAsync<Item>(reader))
        {
            readBack.Add(item);
        }

        Assert.Equal(items.Select(i => i.Name), readBack.Select(i => i!.Name));
    }

    [Fact]
    public async Task SerializeToFileAsync_WritesNoBom()
    {
        await ToonSerializer.SerializeToFileAsync(Items[0], _filePath);

        AssertNoBom(await File.ReadAllBytesAsync(_filePath));
    }

    [Fact]
    public async Task SerializeCollectionToFileAsync_WritesNoBom()
    {
        await ToonSerializer.SerializeCollectionToFileAsync(Items, _filePath);

        AssertNoBom(await File.ReadAllBytesAsync(_filePath));
    }

    [Fact]
    public async Task SerializeToStreamAsync_WritesNoBom()
    {
        using var stream = new MemoryStream();
        await ToonSerializer.SerializeToStreamAsync(Items[0], stream);

        AssertNoBom(stream.ToArray());
    }

    [Fact]
    public async Task SerializeDocumentsAsync_Collection_WritesNoBom()
    {
        using var stream = new MemoryStream();
        await ToonSerializer.SerializeDocumentsAsync(Items, stream);

        AssertNoBom(stream.ToArray());
    }

    [Fact]
    public async Task SerializeDocumentsAsync_WritesNoBom()
    {
        using var stream = new MemoryStream();
        await ToonSerializer.SerializeDocumentsAsync(ToAsync(Items), stream);

        AssertNoBom(stream.ToArray());
    }

    [Fact]
    public async Task EncodeToFileAsync_WritesNoBom()
    {
        var document = new ToonDocument(new ToonObject { ["Name"] = new ToonString("ä") });

        await new ToonEncoder().EncodeToFileAsync(document, _filePath);

        AssertNoBom(await File.ReadAllBytesAsync(_filePath));
    }

    private static async IAsyncEnumerable<Item> ToAsync(IEnumerable<Item> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private sealed class Item
    {
        public string Name { get; set; } = "";
    }
}
