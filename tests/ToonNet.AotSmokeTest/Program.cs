using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

// Writes and reads [ToonSerializable] types through their generated methods and through ToonSerializer with their type
// infos, which NativeAOT can compile without reflection. Exits with 1 when the text read back does not write the same text again.
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

// The same through UTF-8 bytes, without intermediate strings
var utf8 = new ToonEncoder().EncodeToUtf8Bytes(Catalog.Serialize(catalog));
var utf8Again = new ToonEncoder().EncodeToUtf8Bytes(Catalog.Serialize(Catalog.Deserialize(ToonDocument.Parse(utf8))));

Console.WriteLine(text);

if (text != expected || again != text)
{
    Console.WriteLine("FAILED: the text differs from the expected text or does not round-trip");
    return 1;
}

if (!utf8.AsSpan().SequenceEqual(System.Text.Encoding.UTF8.GetBytes(expected)) || !utf8Again.AsSpan().SequenceEqual(utf8))
{
    Console.WriteLine("FAILED: the UTF-8 output differs from the expected text or does not round-trip");
    return 1;
}

// ToonSerializer with type infos: straight to and from text through generated code, without reflection
var viaTypeInfo = ToonSerializer.Serialize(catalog, Catalog.ToonTypeInfo);
var readViaTypeInfo = ToonSerializer.Deserialize(System.Text.Encoding.UTF8.GetBytes(viaTypeInfo), Catalog.ToonTypeInfo)!;
var list = ToonTypeInfo.CreateList(Catalog.ToonTypeInfo);
var listText = ToonSerializer.Serialize(new List<Catalog> { catalog, readViaTypeInfo }, list);
var listBack = ToonSerializer.Deserialize(listText, list)!;

// The items of a root array, streamed
var streamed = new List<Catalog?>();

await foreach (var item in ToonSerializer.DeserializeAsyncEnumerable(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(listText)), Catalog.ToonTypeInfo))
{
    streamed.Add(item);
}

if (viaTypeInfo != expected || ToonSerializer.Serialize(readViaTypeInfo, Catalog.ToonTypeInfo) != expected ||
    listBack.Count != 2 || ToonSerializer.Serialize(listBack[1], Catalog.ToonTypeInfo) != expected ||
    streamed.Count != 2 || ToonSerializer.Serialize(streamed[1]!, Catalog.ToonTypeInfo) != expected)
{
    Console.WriteLine("FAILED: the type info overloads differ from the expected text or do not round-trip");
    return 1;
}

// Converters of the options are applied by generated code too
var withConverter = new ToonSerializerOptions();
withConverter.AddConverter(new SizeConverter());
var converted = ToonSerializer.Serialize(catalog, Catalog.ToonTypeInfo, withConverter);
var convertedBack = ToonSerializer.Deserialize(converted, Catalog.ToonTypeInfo, withConverter)!;

if (!converted.Contains("A1,9.99,1x2") || convertedBack.Products[1].Size.Height != 4 ||
    ToonSerializer.Serialize(convertedBack, Catalog.ToonTypeInfo, withConverter) != converted)
{
    Console.WriteLine("FAILED: the type info overloads with a converter differ or do not round-trip");
    Console.WriteLine(converted);
    return 1;
}

// Paths, values and type infos on a parsed tree
var tree = ToonDocument.Parse(expected).Root;
var cheap = tree.Select("$.Products[?@.Price < 10 && match(@.Sku, 'A.')].Sku");
tree.SetValue("$.Warehouses.oslo.Stock", 4);

if (cheap is not [ToonString { Value: "A1" }] || tree["Warehouses"]!["oslo"]!["Stock"]!.GetValue<int>() != 4 ||
    tree.Deserialize(Catalog.ToonTypeInfo)!.Warehouses["oslo"].Stock != 4 || !ToonValue.DeepEquals(tree, tree.DeepClone()) ||
    ToonSerializer.DeserializeAt(expected, "$.Products[1].Size", Size.ToonTypeInfo)!.Height != 4)
{
    Console.WriteLine("FAILED: paths or tree access");
    return 1;
}

Console.WriteLine("OK");
return 0;

public sealed class SizeConverter : ToonConverter<Size>
{
    public override ToonValue Write(Size? value, ToonSerializerOptions options) => new ToonString($"{value!.Width}x{value.Height}");

    public override Size Read(ToonValue value, ToonSerializerOptions options)
    {
        var parts = ((ToonString)value).Value.Split('x');
        return new Size { Width = int.Parse(parts[0]), Height = int.Parse(parts[1]) };
    }
}

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
