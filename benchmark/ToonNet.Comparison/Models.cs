using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Comparison;

// JSON-native property types only (no enums, which System.Text.Json and so the official library write as numbers
// while ToonNet writes their names), so both libraries see the same data model and must produce the same text.

/// <summary>A flat record: lists of it use the tabular form.</summary>
[ToonSerializable]
public sealed partial class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public double Score { get; set; }
    public bool IsActive { get; set; }
    public string Department { get; set; } = "";
    public decimal Salary { get; set; }
    public DateTime JoinedAt { get; set; }
}

[ToonSerializable]
public sealed partial class Customer
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

[ToonSerializable]
public sealed partial class Address
{
    public string City { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

/// <summary>Nested objects and an array: lists of it use the list form.</summary>
[ToonSerializable]
public sealed partial class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; } = new();
    public Address ShipTo { get; set; } = new();
    public decimal Total { get; set; }
    public bool Paid { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = [];
}

[ToonSerializable]
public sealed partial class Dimensions
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Depth { get; set; }
}

[ToonSerializable]
public sealed partial class Money
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
}

/// <summary>Uniform nested objects: lists of it use the tabular form with nested field groups (spec 4.4 §9.3).</summary>
[ToonSerializable]
public sealed partial class Product
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public Dimensions Size { get; set; } = new();
    public Money Price { get; set; } = new();
    public int Stock { get; set; }
}

/// <summary>Values of a dictionary keyed by name: the keyed tabular form (spec 4.4 §9.5).</summary>
[ToonSerializable]
public sealed partial class Server
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string Region { get; set; } = "";
    public bool Healthy { get; set; }
}
