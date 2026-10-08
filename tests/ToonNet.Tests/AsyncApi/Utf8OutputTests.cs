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
    public async Task SerializeCollectionToStreamAsync_WritesNoBom()
    {
        using var stream = new MemoryStream();
        await ToonSerializer.SerializeCollectionToStreamAsync(Items, stream);

        AssertNoBom(stream.ToArray());
    }

    [Fact]
    public async Task SerializeStreamAsync_WritesNoBom()
    {
        using var stream = new MemoryStream();
        await ToonSerializer.SerializeStreamAsync(ToAsync(Items), stream);

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
