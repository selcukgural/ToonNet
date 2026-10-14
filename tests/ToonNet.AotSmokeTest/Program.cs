using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ToonNet.AspNetCore.DependencyInjection;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Paths;
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

// The items of a nested array, streamed
var streamedProducts = new List<Product?>();

await foreach (var product in ToonSerializer.DeserializeAsyncEnumerableAt(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(expected)), "$.Products",
                                                                          Product.ToonTypeInfo))
{
    streamedProducts.Add(product);
}

if (streamedProducts is not [{ Sku: "A1" }, { Sku: "B2" }])
{
    Console.WriteLine("FAILED: nested array streaming");
    return 1;
}

// Prompts: shortened arrays, answers read leniently
var promptText = ToonNet.Core.Llm.ToonPrompt.Serialize(catalog, Catalog.ToonTypeInfo, new ToonNet.Core.Llm.ToonPromptOptions { MaxArrayItems = 1 });
var answer = ToonNet.Core.Llm.ToonPrompt.Deserialize("Here:\n```toon\n" + promptText + "\n```", Catalog.ToonTypeInfo);

if (!promptText.Contains("# 1 more item") || answer is not { Products.Count: 1, Tags.Count: 1 })
{
    Console.WriteLine("FAILED: prompt helpers");
    return 1;
}

// Rules and registered functions
var rules = new ToonNet.Core.Validation.ToonRuleSet(new ToonPathOptions { Functions = new ToonPathFunctions().AddPredicate("is_sku", v => v is ToonString) })
    .Rule("$.Products[*]", "@.Price > 0 && is_sku(@.Sku) && starts_with(@.Sku, 'A')", "bad product")
    .Unique("$.Products[*].Sku", "repeated sku");

if (rules.Validate(expected).Violations is not [{ Path: "$['Products'][1]" }])
{
    Console.WriteLine("FAILED: rules");
    return 1;
}

// Paths, values and type infos on a parsed tree
var tree = ToonDocument.Parse(expected).Root;
var cheap = tree.Select("$.Products[?@.Price < 10 && match(@.Sku, 'A.')].Sku");
tree.SetValue("$.Warehouses.oslo.Stock", 4);

if (cheap is not [ToonString { Value: "A1" }] || tree["Warehouses"]!["oslo"]!["Stock"]!.GetValue<int>() != 4 ||
    tree.Deserialize(Catalog.ToonTypeInfo)!.Warehouses["oslo"].Stock != 4 || !ToonValue.DeepEquals(tree, tree.DeepClone()) ||
    ToonSerializer.DeserializeAt(expected, "$.Products[1].Size", Size.ToonTypeInfo)!.Height != 4 ||
    ToonSerializer.DeserializeAll(expected, "$.Products[?@.Price > 10]", Product.ToonTypeInfo) is not [{ Sku: "B2" }] ||
    ToonPath.Parse("$.Warehouses[?@.Stock < 5].City").SelectFrom(expected) is not [ToonString { Value: "Oslo" }])
{
    Console.WriteLine("FAILED: paths or tree access");
    return 1;
}

// Records with constructor defaults, structs, init and required members, sets and nullable values
var shapes = new Shapes(7, new Point { X = 1, Y = -2 }) { Code = "c", Labels = ["b", "a"], Maybe = 3, Origin = new(0, 0) };
const string shapesText = "Id: 7\nAt:\n  X: 1\n  Y: -2\nScale: 1.5\nCode: c\nLabels[2]: b,a\nMaybe: 3\nOrigin:\n  X: 0\n  Y: 0";
var shapesBack = ToonSerializer.Deserialize("Id: 7\nAt:\n  X: 1\n  Y: -2\nCode: c\nLabels[2]: b,a\nMaybe: null\nOrigin:\n  X: 0\n  Y: 0",
                                            Shapes.ToonTypeInfo)!;

if (ToonSerializer.Serialize(shapes, Shapes.ToonTypeInfo) != shapesText ||
    shapesBack is not { Id: 7, At: { X: 1, Y: -2 }, Scale: 1.5, Code: "c", Maybe: null, Origin: { X: 0, Y: 0 } } ||
    !shapesBack.Labels.SetEquals(["a", "b"]))
{
    Console.WriteLine("FAILED: records, structs, init and required members");
    Console.WriteLine(ToonSerializer.Serialize(shapes, Shapes.ToonTypeInfo));
    return 1;
}

// A generic type's init-only property with an initializer, without its key: read without reflection (no throw)
var paged = ToonSerializer.Deserialize("P:\n  Item: 1", PageHolder.ToonTypeInfo)!;

// A derived array held as its base array type is written as the base type, by generated code
var held = ToonSerializer.Serialize(new Holder { Sizes = [new LabeledSize { Width = 1, Height = 2, Label = "secret" }] }, Holder.ToonTypeInfo);

if (paged.P is not { Item: 1 } || held != "Sizes[1]{Width,Height}:\n  1,2")
{
    Console.WriteLine("FAILED: generic init members or declared array types");
    Console.WriteLine(held);
    return 1;
}

// The other packages' members without reflection: JSON both ways, YAML reading, options and configuration from AddToonNet
var fromJson = ToonNet.Extensions.Json.ToonConvert.FromJson("""{"a":[1,2],"b":{"c":"x"}}""");
var backToJson = ToonNet.Extensions.Json.ToonConvert.ToJson(fromJson);
var fromYaml = ToonNet.Extensions.Yaml.ToonYamlConvert.FromYaml("a:\n  - 1\n  - 2\nb:\n  c: x\n");

var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ToonNet:ToonOptions:IndentSize"] = "4",
                        ["ToonNet:ToonSerializerOptions:PropertyNamingPolicy"] = "camelCase",
                        ["ToonNet:ToonSerializerOptions:IgnoreNullValues"] = "true"
                    })
                    .Build();
using var provider = new ServiceCollection().AddToonNet(configuration).BuildServiceProvider();
var serializerOptions = provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value;

if (fromJson != "a[2]: 1,2\nb:\n  c: x" || backToJson != """{"a":[1,2],"b":{"c":"x"}}""" || fromYaml != fromJson ||
    serializerOptions is not { PropertyNamingPolicy: PropertyNamingPolicy.CamelCase, IgnoreNullValues: true, ToonOptions.IndentSize: 4 })
{
    Console.WriteLine("FAILED: JSON, YAML or AddToonNet");
    Console.WriteLine(fromJson);
    Console.WriteLine(backToJson);
    Console.WriteLine(fromYaml);
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

[ToonSerializable(AllowReflectionFallback = false)]
public partial struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
}

[ToonSerializable(AllowReflectionFallback = false)]
public readonly partial record struct Coordinates(int X, int Y);

[ToonSerializable(AllowReflectionFallback = false)]
public partial record Shapes(int Id, Point At, double Scale = 1.5)
{
    public required string Code { get; init; }
    public HashSet<string> Labels { get; init; } = [];
    public int? Maybe { get; set; }
    public Coordinates Origin { get; init; }
}

[ToonSerializable]
public partial class Page<T>
{
    public int Size { get; init; } = 20;
    public int Item { get; set; } // a generic type: its init-only members are set in the object initializer
}

[ToonSerializable]
public partial class PageHolder
{
    public Page<int>? P { get; set; }
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial class LabeledSize : Size
{
    public string Label { get; set; } = "";
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial class Holder
{
    public Size[] Sizes { get; set; } = [];
}
