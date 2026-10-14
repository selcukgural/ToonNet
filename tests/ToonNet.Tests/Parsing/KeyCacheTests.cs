using ToonNet.Core.Models;

namespace ToonNet.Tests.Parsing;

public class KeyCacheTests
{
    /// <summary>
    ///     The keys repeated in every item of a list are shared strings in every process: the cache's hash is not
    ///     randomized, and two keys that want the same slot both stay cached.
    /// </summary>
    [Fact]
    public void RepeatedKeys_AreSharedStrings()
    {
        string[] keys = ["Id", "Customer", "Name", "Email", "ShipTo", "City", "ZipCode", "Total", "Paid", "CreatedAt", "Tags", "Notes", "Status"];
        var item = string.Join("\n", keys.Select((k, i) => (i == 0 ? "  - " : "    ") + k + ": " + i));
        var toon = "items[100]:\n" + string.Join("\n", Enumerable.Repeat(item, 100));

        var items = ToonDocument.Parse(toon).Root["items"]!.AsArray().Items;
        var first = items[0].AsObject().Properties.Keys;

        foreach (var other in items.Skip(1))
        {
            var otherKeys = other.AsObject().Properties.Keys;

            for (var i = 0; i < keys.Length; i++)
            {
                Assert.Same(first[i], otherKeys[i]);
            }
        }
    }
}
