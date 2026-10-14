using System.Buffers;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using ToonNet.Core.Serialization;

namespace ToonNet.Comparison;

/// <summary>
///     Bytes in and out: ToonNet through a string (encoding or decoding it as UTF-8 separately), ToonNet's UTF-8
///     overloads, and System.Text.Json's UTF-8 methods as a reference point.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Utf8Benchmarks
{
    private Func<object?> _serializeViaString = null!;
    private Func<object?> _serializeUtf8 = null!;
    private Func<object?> _serializeBufferWriter = null!;
    private Func<object?> _serializeJson = null!;
    private Func<object?> _deserializeViaString = null!;
    private Func<object?> _deserializeUtf8 = null!;
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
                Bind(Datasets.Employees(Count));
                break;
            case Shape.Orders:
                Bind(Datasets.Orders(Count));
                break;
            case Shape.Products:
                Bind(Datasets.Products(Count));
                break;
            default:
                Bind(Datasets.Servers(Count));
                break;
        }
    }

    private void Bind<T>(T value)
    {
        var toon = ToonSerializer.SerializeToUtf8Bytes(value);
        var json = JsonSerializer.SerializeToUtf8Bytes(value);
        var writer = new ArrayBufferWriter<byte>(toon.Length);

        if (!toon.AsSpan().SequenceEqual(Encoding.UTF8.GetBytes(ToonSerializer.Serialize(value))))
        {
            throw new InvalidOperationException("SerializeToUtf8Bytes differs from the UTF-8 of Serialize");
        }

        _serializeViaString = () => Encoding.UTF8.GetBytes(ToonSerializer.Serialize(value));
        _serializeUtf8 = () => ToonSerializer.SerializeToUtf8Bytes(value);
        _serializeBufferWriter = () =>
        {
            writer.ResetWrittenCount();
            ToonSerializer.Serialize(writer, value);
            return writer;
        };
        _serializeJson = () => JsonSerializer.SerializeToUtf8Bytes(value);
        _deserializeViaString = () => ToonSerializer.Deserialize<T>(Encoding.UTF8.GetString(toon));
        _deserializeUtf8 = () => ToonSerializer.Deserialize<T>(toon);
        _deserializeJson = () => JsonSerializer.Deserialize<T>(json);
    }

    [Benchmark(Baseline = true, Description = "ToonNet via string")]
    [BenchmarkCategory("Serialize")]
    public object? Serialize_ViaString() => _serializeViaString();

    [Benchmark(Description = "ToonNet SerializeToUtf8Bytes")]
    [BenchmarkCategory("Serialize")]
    public object? Serialize_Utf8() => _serializeUtf8();

    [Benchmark(Description = "ToonNet IBufferWriter (reused)")]
    [BenchmarkCategory("Serialize")]
    public object? Serialize_BufferWriter() => _serializeBufferWriter();

    [Benchmark(Description = "System.Text.Json UTF-8")]
    [BenchmarkCategory("Serialize")]
    public object? Serialize_Json() => _serializeJson();

    [Benchmark(Baseline = true, Description = "ToonNet via string")]
    [BenchmarkCategory("Deserialize")]
    public object? Deserialize_ViaString() => _deserializeViaString();

    [Benchmark(Description = "ToonNet UTF-8")]
    [BenchmarkCategory("Deserialize")]
    public object? Deserialize_Utf8() => _deserializeUtf8();

    [Benchmark(Description = "System.Text.Json UTF-8")]
    [BenchmarkCategory("Deserialize")]
    public object? Deserialize_Json() => _deserializeJson();
}
