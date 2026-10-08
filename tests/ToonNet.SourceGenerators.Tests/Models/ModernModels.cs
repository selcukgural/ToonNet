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
