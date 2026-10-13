using System.Text.Json;
using BenchmarkDotNet.Attributes;
using ToonNet.Benchmarks.Models;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

namespace ToonNet.Benchmarks;

/// <summary>
///     Typed serialization of lists through <see cref="ToonSerializer"/>: the reflection-based path, the generated
///     code path (the same models, as <c>[ToonSerializable]</c> types are dispatched to generated code) and
///     System.Text.Json as a reference point. Parse and encode are also measured on their own.
/// </summary>
/// <remarks>
///     Orders are nested objects written as list items; rows are flat objects written as a tabular array. Setup checks
///     that both serializer paths produce the same text, so an optimization that changes the output fails early.
/// </remarks>
[MemoryDiagnoser]
public class TypedSerializationBenchmarks
{
    private static readonly ToonSerializerOptions GeneratedOptions = new();
    private static readonly ToonSerializerOptions ReflectionOptions = new() { UseGeneratedCode = false };

    private List<OrderBenchmarkModel> _orders = null!;
    private List<RowBenchmarkModel> _rows = null!;
    private string _ordersToon = null!;
    private string _rowsToon = null!;
    private string _ordersJson = null!;
    private string _rowsJson = null!;
    private ToonDocument _ordersDocument = null!;
    private ToonDocument _rowsDocument = null!;

    [Params(100, 10_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        _orders = Enumerable.Range(0, Count).Select(i => new OrderBenchmarkModel
        {
            Id = i,
            Customer = new CustomerBenchmarkModel($"Customer {i}", $"customer{i}@example.com"),
            Address = new AddressBenchmarkModel { City = $"City {i % 100}", ZipCode = $"{10000 + i % 90000}" },
            Total = 10.25m + i % 1000,
            Rating = 1.5 + i % 4,
            Paid = i % 3 != 0,
            CreatedAt = start.AddMinutes(i),
            Status = (OrderStatus)(i % 3),
            Tags = [$"tag{i % 5}", $"tag{i % 7}"]
        }).ToList();

        _rows = Enumerable.Range(0, Count).Select(i => new RowBenchmarkModel
        {
            Id = i,
            Name = $"User {i}",
            Email = $"user{i}@example.com",
            Age = 20 + i % 50,
            Score = 50.5 + i % 50,
            IsActive = i % 2 == 0,
            Department = $"Dept {i % 10}",
            Salary = 50_000 + i % 50_000
        }).ToList();

        _ordersToon = ToonSerializer.Serialize(_orders);
        _rowsToon = ToonSerializer.Serialize(_rows);
        _ordersJson = JsonSerializer.Serialize(_orders);
        _rowsJson = JsonSerializer.Serialize(_rows);
        _ordersDocument = new ToonParser().Parse(_ordersToon);
        _rowsDocument = new ToonParser().Parse(_rowsToon);

        EnsureSameOutput(_orders, _ordersToon);
        EnsureSameOutput(_rows, _rowsToon);
    }

    private static void EnsureSameOutput<T>(T value, string toon)
    {
        var reflection = ToonSerializer.Serialize(value, ReflectionOptions);
        var readBack = ToonSerializer.Serialize(ToonSerializer.Deserialize<T>(toon, ReflectionOptions), GeneratedOptions);

        if (reflection != toon || readBack != toon)
        {
            throw new InvalidOperationException($"Reflection and generated serialization of {typeof(T).Name} differ");
        }
    }

    #region Orders (nested objects, list items)

    [Benchmark]
    [BenchmarkCategory("Orders", "Serialize")]
    public string Orders_Serialize_Reflection() => ToonSerializer.Serialize(_orders, ReflectionOptions);

    [Benchmark]
    [BenchmarkCategory("Orders", "Serialize")]
    public string Orders_Serialize_Generated() => ToonSerializer.Serialize(_orders, GeneratedOptions);

    [Benchmark]
    [BenchmarkCategory("Orders", "Serialize")]
    public string Orders_Serialize_Json() => JsonSerializer.Serialize(_orders);

    [Benchmark]
    [BenchmarkCategory("Orders", "Deserialize")]
    public List<OrderBenchmarkModel>? Orders_Deserialize_Reflection() =>
        ToonSerializer.Deserialize<List<OrderBenchmarkModel>>(_ordersToon, ReflectionOptions);

    [Benchmark]
    [BenchmarkCategory("Orders", "Deserialize")]
    public List<OrderBenchmarkModel>? Orders_Deserialize_Generated() =>
        ToonSerializer.Deserialize<List<OrderBenchmarkModel>>(_ordersToon, GeneratedOptions);

    [Benchmark]
    [BenchmarkCategory("Orders", "Deserialize")]
    public List<OrderBenchmarkModel>? Orders_Deserialize_Json() => JsonSerializer.Deserialize<List<OrderBenchmarkModel>>(_ordersJson);

    [Benchmark]
    [BenchmarkCategory("Orders", "Text")]
    public ToonDocument Orders_ParseOnly() => new ToonParser().Parse(_ordersToon);

    [Benchmark]
    [BenchmarkCategory("Orders", "Text")]
    public string Orders_EncodeOnly() => new ToonEncoder().Encode(_ordersDocument);

    #endregion

    #region Rows (flat objects, tabular)

    [Benchmark]
    [BenchmarkCategory("Rows", "Serialize")]
    public string Rows_Serialize_Reflection() => ToonSerializer.Serialize(_rows, ReflectionOptions);

    [Benchmark]
    [BenchmarkCategory("Rows", "Serialize")]
    public string Rows_Serialize_Generated() => ToonSerializer.Serialize(_rows, GeneratedOptions);

    [Benchmark]
    [BenchmarkCategory("Rows", "Serialize")]
    public string Rows_Serialize_Json() => JsonSerializer.Serialize(_rows);

    [Benchmark]
    [BenchmarkCategory("Rows", "Deserialize")]
    public List<RowBenchmarkModel>? Rows_Deserialize_Reflection() =>
        ToonSerializer.Deserialize<List<RowBenchmarkModel>>(_rowsToon, ReflectionOptions);

    [Benchmark]
    [BenchmarkCategory("Rows", "Deserialize")]
    public List<RowBenchmarkModel>? Rows_Deserialize_Generated() =>
        ToonSerializer.Deserialize<List<RowBenchmarkModel>>(_rowsToon, GeneratedOptions);

    [Benchmark]
    [BenchmarkCategory("Rows", "Deserialize")]
    public List<RowBenchmarkModel>? Rows_Deserialize_Json() => JsonSerializer.Deserialize<List<RowBenchmarkModel>>(_rowsJson);

    [Benchmark]
    [BenchmarkCategory("Rows", "Text")]
    public ToonDocument Rows_ParseOnly() => new ToonParser().Parse(_rowsToon);

    [Benchmark]
    [BenchmarkCategory("Rows", "Text")]
    public string Rows_EncodeOnly() => new ToonEncoder().Encode(_rowsDocument);

    #endregion
}
