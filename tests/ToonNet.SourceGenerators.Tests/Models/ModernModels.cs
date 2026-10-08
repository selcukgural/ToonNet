using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Models;

public enum Status
{
    Active,
    Suspended
}

/// <summary>Property types that go through the serializer (collections, enums, dates) next to inline primitives.</summary>
[ToonSerializable]
public partial class CatalogItem
{
    [ToonPropertyOrder(-1)]
    public int Id { get; set; }

    [ToonProperty("display name")]
    public string DisplayName { get; set; } = "";

    public List<int> Tags { get; set; } = [];

    public int[] Scores { get; set; } = [];

    public Dictionary<string, int> Stock { get; set; } = new();

    public List<Address> Addresses { get; set; } = [];

    public Status Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid Key { get; set; }

    public long Big { get; set; }

    public ulong Huge { get; set; }

    public decimal Price { get; set; }

    public double Ratio { get; set; }

    public float Weight { get; set; }

    public char Grade { get; set; }

    public int? MaybeCount { get; set; }

    public string? Note { get; set; }

    public int TagCount => Tags.Count;

    public int Revision { get; private set; }

    [ToonIgnore]
    public string Secret { get; set; } = "";

    public void Bump() => Revision++;
}

[ToonSerializable]
public partial record Product(string Name, decimal Price, List<string> Tags);

[ToonSerializable]
public partial record struct Coordinate(double Lat, double Lon);

[ToonSerializable]
public partial struct Counter
{
    public int Value { get; set; }

    public string? Label { get; init; }
}

[ToonSerializable]
public partial class Account
{
    public required string Email { get; init; }

    public string Plan { get; init; } = "free";

    public int Logins { get; private set; }

    public void Login() => Logins++;
}

public class EntityBase
{
    public int Id { get; set; }

    public virtual string Kind { get; set; } = "base";

    public DateTime Created { get; private set; }

    public void Touch(DateTime value) => Created = value;
}

[ToonSerializable]
public partial class Customer : EntityBase
{
    public string Name { get; set; } = "";

    public override string Kind { get; set; } = "customer";
}

[ToonSerializable]
public partial class Box<T>
{
    public T? Value { get; set; }

    public string Label { get; init; } = "";
}

public static partial class Outer
{
    [ToonSerializable]
    public partial class Inner
    {
        public int A { get; set; }
    }
}

[ToonSerializable]
public partial class Shipment
{
    public string Code { get; set; } = "";

    public Address? Destination { get; set; }

    public Other.Parcel? Parcel { get; set; }
}

/// <summary>Uses the naming policy from the options at runtime (no NamingPolicy on the attribute).</summary>
[ToonSerializable]
public partial class Contact
{
    public string FirstName { get; set; } = "";

    public string PhoneNumber { get; set; } = "";
}

/// <summary>Writes integers as strings, to check that options converters are honoured.</summary>
public sealed class IntAsStringConverter : ToonConverter<int>
{
    public override ToonValue? Write(int value, ToonSerializerOptions options) => new ToonString("#" + value);

    public override int Read(ToonValue value, ToonSerializerOptions options) => int.Parse(((ToonString)value).Value.TrimStart('#'));
}

[ToonSerializable]
public partial class Positional
{
    public Positional(string name, int count = 7, string mode = "auto")
    {
        Name = name;
        Count = count;
        Mode = mode;
    }

    public string Name { get; }

    public int Count { get; }

    public string Mode { get; }
}

// Used only by GeneratedPathTests, so the reflection-based serializer never caches metadata for them elsewhere.
[ToonSerializable(AllowReflectionFallback = false)]
public sealed partial class Warehouse
{
    public string Name { get; set; } = "";

    public List<Shelf> Shelves { get; set; } = [];

    public Dictionary<string, Shelf> ByCode { get; set; } = new();

    public Dictionary<Guid, int[]> Counts { get; set; } = new();

    public Dictionary<Status, List<string?>> Notes { get; set; } = new();

    public HashSet<Status> Flags { get; set; } = [];

    public IReadOnlyList<int?> Readings { get; set; } = [];

    public int[][] Grid { get; set; } = [];

    public DateTimeOffset OpenedAt { get; set; }

    public TimeSpan? Window { get; set; }

    public Shelf? Spare { get; set; }

    public Bin? Bin { get; set; }

    public ToonValue? Extra { get; set; }
}

[ToonSerializable(AllowReflectionFallback = false)]
public sealed partial record Shelf(string Code, int Capacity, Status Status);

[ToonSerializable(AllowReflectionFallback = false)]
public partial record struct Bin(int Row, int Column);

/// <summary>Self-referencing type, for cycle handling.</summary>
[ToonSerializable]
public partial class Node
{
    public string Name { get; set; } = "";

    public Node? Next { get; set; }
}

/// <summary>A derived instance stored in a property declared as the [ToonSerializable] base type.</summary>
public class SpecialAddress : Address
{
    public string Floor { get; set; } = "";
}

// Used only by GeneratedPathTests.SupportedGraph_UsesNoReflectionMetadata: no test may pass these to ToonSerializer.
[ToonSerializable(AllowReflectionFallback = false)]
public sealed partial class Depot
{
    public List<Crate> Crates { get; set; } = [];

    public Dictionary<Guid, Crate[]> ByOwner { get; set; } = new();

    public Dictionary<Status, HashSet<Slot>> Slots { get; set; } = new();

    public Crate? Spare { get; set; }

    public Slot? Entrance { get; set; }
}

[ToonSerializable(AllowReflectionFallback = false)]
public sealed partial record Crate(string Label, decimal Weight, DateOnly PackedOn);

[ToonSerializable(AllowReflectionFallback = false)]
public partial record struct Slot(int Row, int Column);
