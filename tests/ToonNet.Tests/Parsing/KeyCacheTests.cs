using ToonNet.Core.Models;

namespace ToonNet.Tests.Parsing;

public class KeyCacheTests
{
    /// <summary>
    ///     The keys repeated in every item of a list are shared strings in every process: the cache's hash is not
    ///     randomized, and two keys that want the same first slot ("Acd" and "Ada" under FNV-1a) both stay cached.
    /// </summary>
    [Theory]
    [InlineData("Acd", "Ada")]
    [InlineData("Id", "Name")]
    public void RepeatedKeys_AreSharedStrings(string first, string second)
    {
        // The cache is used from 64 lines on
        var item = $"  - {first}: 1\n    {second}: 2";
        var toon = "items[100]:\n" + string.Join("\n", Enumerable.Repeat(item, 100));

        var items = ToonDocument.Parse(toon).Root["items"]!.AsArray().Items;
        var keys = items[0].AsObject().Properties.Keys;

        foreach (var other in items.Skip(1))
        {
            Assert.Same(keys[0], other.AsObject().Properties.Keys[0]);
            Assert.Same(keys[1], other.AsObject().Properties.Keys[1]);
        }
    }
}
