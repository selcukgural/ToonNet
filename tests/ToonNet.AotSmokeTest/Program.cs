using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization.Attributes;

// Writes and reads [ToonSerializable] types through their generated methods, which NativeAOT can compile without
// reflection. Exits with 1 when the text read back does not write the same text again.
var catalog = new Catalog
{
    Name = "Spring",
    Status = Status.Active,
    Published = new DateTime(2026, 3, 1, 9, 30, 0, DateTimeKind.Utc),
    Products =
    [
        new() { Sku = "A1", Price = 9.99m, Size = new() { Width = 1, Height = 2 } },
        new() { Sku = "B2", Price = 19.5m, Size = new() { Width = 3, Height = 4 } }
    ],
    Warehouses = new() { ["berlin"] = new() { City = "Berlin", Stock = 10 }, ["oslo"] = new() { City = "Oslo", Stock = 3 } },
    Tags = ["new", "sale"]
};

const string expected = """
                        Name: Spring
                        Status: Active
                        Published: "2026-03-01T09:30:00Z"
                        Products[2]{Sku,Price,Size{Width,Height}}:
                          A1,9.99,1,2
                          B2,19.5,3,4
                        Warehouses[2:]{City,Stock}:
                          berlin: Berlin,10
                          oslo: Oslo,3
                        Tags[2]: new,sale
                        """;

var text = new ToonEncoder().Encode(Catalog.Serialize(catalog));
var again = new ToonEncoder().Encode(Catalog.Serialize(Catalog.Deserialize(ToonDocument.Parse(text))));

Console.WriteLine(text);

if (text != expected || again != text)
{
    Console.WriteLine("FAILED: the text differs from the expected text or does not round-trip");
    return 1;
}

Console.WriteLine("OK");
return 0;

public enum Status
{
    Draft,
    Active
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial class Size
{
    public int Width { get; set; }
    public int Height { get; set; }
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial class Product
{
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public Size Size { get; set; } = new();
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial class Warehouse
{
    public string City { get; set; } = "";
    public int Stock { get; set; }
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial class Catalog
{
    public string Name { get; set; } = "";
    public Status Status { get; set; }
    public DateTime Published { get; set; }
    public List<Product> Products { get; set; } = [];
    public Dictionary<string, Warehouse> Warehouses { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}
