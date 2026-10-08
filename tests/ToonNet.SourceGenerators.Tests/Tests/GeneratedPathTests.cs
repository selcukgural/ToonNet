using System.Collections;
using System.Reflection;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.SourceGenerators.Tests.Models;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
/// Generated code converts collections, dictionaries, enums, dates and nested [ToonSerializable] types itself.
/// </summary>
public class GeneratedPathTests
{
    private static Warehouse CreateWarehouse() => new()
    {
        Name = "North",
        Shelves = [new Shelf("A1", 10, Status.Active), new Shelf("B2", 0, Status.Suspended)],
        ByCode = new Dictionary<string, Shelf> { ["a1"] = new("A1", 10, Status.Active) },
        Counts = new Dictionary<Guid, int[]> { [Guid.Parse("11111111-2222-3333-4444-555555555555")] = [1, 2, 3] },
        Notes = new Dictionary<Status, List<string?>> { [Status.Suspended] = ["broken", null] },
        Flags = [Status.Active],
        Readings = [1, null, 3],
        Grid = [[1, 2], [3, 4]],
        OpenedAt = new DateTimeOffset(2026, 10, 9, 8, 0, 0, TimeSpan.FromHours(2)),
        Window = TimeSpan.FromMinutes(90),
        Spare = null,
        Bin = new Bin(3, 4),
        Extra = new ToonString("raw")
    };

    /// <summary>The reflection-based serializer's metadata cache (private), to prove it was not used.</summary>
    private static ICollection CachedTypes()
    {
        var field = typeof(ToonSerializer).GetField("TypeMetadataCache", BindingFlags.NonPublic | BindingFlags.Static)!;
        return ((IDictionary)field.GetValue(null)!).Keys;
    }

    [Fact]
    public void SupportedGraph_UsesNoReflectionMetadata()
    {
        var owner = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var depot = new Depot
        {
            Crates = [new Crate("a", 1.5m, new DateOnly(2026, 1, 2))],
            ByOwner = new Dictionary<Guid, Crate[]> { [owner] = [new Crate("b", 2m, new DateOnly(2026, 3, 4))] },
            Slots = new Dictionary<Status, HashSet<Slot>> { [Status.Active] = [new Slot(1, 2)] },
            Entrance = new Slot(0, 0)
        };

        var copy = Depot.Deserialize(ToonDocument.Parse(new ToonNet.Core.Encoding.ToonEncoder().Encode(Depot.Serialize(depot))));

        var cached = CachedTypes().Cast<Type>().ToList();
        Assert.DoesNotContain(typeof(Depot), cached);
        Assert.DoesNotContain(typeof(Crate), cached);
        Assert.DoesNotContain(typeof(Slot), cached);

        Assert.Equal(depot.Crates, copy.Crates);
        Assert.Equal(depot.ByOwner[owner], copy.ByOwner[owner]);
        Assert.Equal(depot.Slots[Status.Active], copy.Slots[Status.Active]);
        Assert.Null(copy.Spare);
        Assert.Equal(depot.Entrance, copy.Entrance);
    }

    [Fact]
    public void SupportedGraph_RoundTrips()
    {
        var warehouse = CreateWarehouse();

        var toon = new ToonNet.Core.Encoding.ToonEncoder().Encode(Warehouse.Serialize(warehouse));
        var copy = Warehouse.Deserialize(ToonDocument.Parse(toon));

        Assert.Equal(warehouse.Shelves, copy.Shelves);
        Assert.Equal(warehouse.ByCode, copy.ByCode);
        Assert.Equal(warehouse.Counts.Single().Value, copy.Counts.Single().Value);
        Assert.Equal(warehouse.Notes[Status.Suspended], copy.Notes[Status.Suspended]);
        Assert.Equal(warehouse.Flags, copy.Flags);
        Assert.Equal(warehouse.Readings, copy.Readings);
        Assert.Equal(warehouse.Grid, copy.Grid);
        Assert.Equal(warehouse.OpenedAt, copy.OpenedAt);
        Assert.Equal(warehouse.Window, copy.Window);
        Assert.Null(copy.Spare);
        Assert.Equal(warehouse.Bin, copy.Bin);
        Assert.Equal("raw", Assert.IsType<ToonString>(copy.Extra).Value);
    }

    [Fact]
    public void SupportedGraph_MatchesReflectionOutput()
    {
        var warehouse = CreateWarehouse();
        var generated = new ToonNet.Core.Encoding.ToonEncoder().Encode(Warehouse.Serialize(warehouse));

        Assert.Equal(ToonSerializer.Serialize(warehouse), generated);

        var fromReflection = ToonSerializer.Deserialize<Warehouse>(generated)!;
        Assert.Equal(generated, new ToonNet.Core.Encoding.ToonEncoder().Encode(Warehouse.Serialize(fromReflection)));
    }

    [Fact]
    public void IgnoreNullValues_AppliesToDictionaryValues_ButKeepsNullListItems()
    {
        var options = new ToonSerializerOptions { IgnoreNullValues = true };
        var warehouse = CreateWarehouse();

        var generated = new ToonNet.Core.Encoding.ToonEncoder().Encode(Warehouse.Serialize(warehouse, options));

        Assert.Equal(ToonSerializer.Serialize(warehouse, options), generated);
        Assert.Contains("Readings[3]: 1,null,3", generated);
        Assert.DoesNotContain("Spare", generated);
    }

    [Fact]
    public void CircularReference_Throws()
    {
        var node = new Node { Name = "a" };
        node.Next = node;

        Assert.Throws<ToonEncodingException>(() => Node.Serialize(node));
    }

    [Fact]
    public void DeepButFiniteChain_RespectsMaxDepth()
    {
        var head = new Node { Name = "0" };
        var current = head;

        for (var i = 1; i < 5; i++)
        {
            current.Next = new Node { Name = i.ToString() };
            current = current.Next;
        }

        Assert.Equal(ToonSerializer.Serialize(head), new ToonNet.Core.Encoding.ToonEncoder().Encode(Node.Serialize(head)));
        Assert.Throws<ToonEncodingException>(() => Node.Serialize(head, new ToonSerializerOptions { MaxDepth = 3 }));
        Assert.Throws<ToonEncodingException>(() => ToonSerializer.Serialize(head, new ToonSerializerOptions { MaxDepth = 3 }));
    }

    [Fact]
    public void DerivedInstance_InGeneratedProperty_IsWrittenWithItsRuntimeType()
    {
        var person = new Person { Name = "Ada", Address = new SpecialAddress { City = "Paris", Floor = "3" } };

        var generated = new ToonNet.Core.Encoding.ToonEncoder().Encode(Person.Serialize(person));

        Assert.Equal(ToonSerializer.Serialize(person), generated);
        Assert.Contains("Floor: \"3\"", generated);
    }

    [Fact]
    public void ArrayWhereObjectExpected_FailsLikeReflection()
    {
        const string toon = "Name: x\nShelves: 5";

        Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Warehouse>(toon));
        Assert.Throws<ToonSerializationException>(() => Warehouse.Deserialize(ToonDocument.Parse(toon)));
    }
}
