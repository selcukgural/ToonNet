using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using ToonNet.Core.Serialization;

namespace ToonNet.Comparison;

/// <summary>The data shapes compared.</summary>
public enum Shape
{
    /// <summary>Flat records (tabular form).</summary>
    Employees,

    /// <summary>Records with nested objects and an array (list form).</summary>
    Orders,

    /// <summary>Records with uniform nested objects (tabular form with nested field groups).</summary>
    Products,

    /// <summary>A dictionary of uniform records (keyed tabular form).</summary>
    Servers
}

/// <summary>
///     Typed serialization and deserialization with ToonNet (generated code, the reflection-free type info overloads, and
///     reflection), the official TOON library
///     (Toon.Format) and System.Text.Json as a reference point. Setup checks that ToonNet and Toon.Format write the same
///     text and read it back to the same values, so the comparison is between equal outputs.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ComparisonBenchmarks
{
    private static readonly ToonSerializerOptions Generated = new();
    private static readonly ToonSerializerOptions Reflection = new() { UseGeneratedCode = false };

    private Func<object?> _serializeToonNet = null!;
    private Func<object?> _serializeToonNetReflection = null!;
    private Func<object?> _serializeToonNetTypeInfo = null!;
    private Func<object?> _serializeToonFormat = null!;
    private Func<object?> _serializeJson = null!;
    private Func<object?> _deserializeToonNet = null!;
    private Func<object?> _deserializeToonNetReflection = null!;
    private Func<object?> _deserializeToonNetTypeInfo = null!;
    private Func<object?> _deserializeToonFormat = null!;
    private Func<object?> _deserializeJson = null!;

    [Params(Shape.Employees, Shape.Orders, Shape.Products, Shape.Servers)]
    public Shape Data { get; set; }

    [Params(100, 10_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        switch (Data)
        {
            case Shape.Employees:
                Bind(Datasets.Employees(Count), ToonTypeInfo.CreateList(Employee.ToonTypeInfo));
                break;
            case Shape.Orders:
                Bind(Datasets.Orders(Count), ToonTypeInfo.CreateList(Order.ToonTypeInfo));
                break;
            case Shape.Products:
                Bind(Datasets.Products(Count), ToonTypeInfo.CreateList(Product.ToonTypeInfo));
                break;
            default:
                Bind(Datasets.Servers(Count), ToonTypeInfo.CreateDictionary(Server.ToonTypeInfo));
                break;
        }
    }

    private void Bind<T>(T value, ToonTypeInfo<T> typeInfo)
    {
        var toon = Parity.CheckTyped(Data.ToString(), value);
        var json = JsonSerializer.Serialize(value);

        _serializeToonNet = () => ToonSerializer.Serialize(value, Generated);
        _serializeToonNetReflection = () => ToonSerializer.Serialize(value, Reflection);
        _serializeToonNetTypeInfo = () => ToonSerializer.Serialize(value, typeInfo);
        _serializeToonFormat = () => Toon.Format.ToonEncoder.Encode(value);
        _serializeJson = () => JsonSerializer.Serialize(value);
        _deserializeToonNet = () => ToonSerializer.Deserialize<T>(toon, Generated);
        _deserializeToonNetReflection = () => ToonSerializer.Deserialize<T>(toon, Reflection);
        _deserializeToonNetTypeInfo = () => ToonSerializer.Deserialize(toon, typeInfo);
        _deserializeToonFormat = () => Toon.Format.ToonDecoder.Decode<T>(toon);
        _deserializeJson = () => JsonSerializer.Deserialize<T>(json);
    }

    [Benchmark(Baseline = true, Description = "Toon.Format")]
    [BenchmarkCategory("Serialize")]
    public object? Serialize_ToonFormat() => _serializeToonFormat();

    [Benchmark(Description = "ToonNet")]
    [BenchmarkCategory("Serialize")]
    public object? Serialize_ToonNet() => _serializeToonNet();

    [Benchmark(Description = "ToonNet (reflection)")]
    [BenchmarkCategory("Serialize")]
    public object? Serialize_ToonNetReflection() => _serializeToonNetReflection();

    [Benchmark(Description = "ToonNet (type info)")]
    [BenchmarkCategory("Serialize")]
    public object? Serialize_ToonNetTypeInfo() => _serializeToonNetTypeInfo();

    [Benchmark(Description = "System.Text.Json")]
    [BenchmarkCategory("Serialize")]
    public object? Serialize_Json() => _serializeJson();

    [Benchmark(Baseline = true, Description = "Toon.Format")]
    [BenchmarkCategory("Deserialize")]
    public object? Deserialize_ToonFormat() => _deserializeToonFormat();

    [Benchmark(Description = "ToonNet")]
    [BenchmarkCategory("Deserialize")]
    public object? Deserialize_ToonNet() => _deserializeToonNet();

    [Benchmark(Description = "ToonNet (reflection)")]
    [BenchmarkCategory("Deserialize")]
    public object? Deserialize_ToonNetReflection() => _deserializeToonNetReflection();

    [Benchmark(Description = "ToonNet (type info)")]
    [BenchmarkCategory("Deserialize")]
    public object? Deserialize_ToonNetTypeInfo() => _deserializeToonNetTypeInfo();

    [Benchmark(Description = "System.Text.Json")]
    [BenchmarkCategory("Deserialize")]
    public object? Deserialize_Json() => _deserializeJson();
}
