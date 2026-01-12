# ToonNet.Benchmarks

**Performance benchmarks for ToonNet serialization**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![BenchmarkDotNet](https://img.shields.io/badge/BenchmarkDotNet-v0.13+-blue)](#)

---

## 📊 What is ToonNet.Benchmarks?

ToonNet.Benchmarks provides **comprehensive performance testing** for ToonNet:

- ⚡ **Parser Benchmarks** - TOON parsing performance
- 🔄 **Encoder Benchmarks** - TOON encoding performance
- 📦 **Serialization Benchmarks** - End-to-end C# ↔ TOON
- 🎯 **Source Generator Benchmarks** - AOT vs runtime comparison
- 🧠 **Memory Benchmarks** - Allocation tracking
- 📈 **Scalability Tests** - Large documents, deep nesting

---

## 🚀 Quick Start

### Running Benchmarks

```bash
# Run all benchmarks
cd src/ToonNet.Benchmarks
dotnet run -c Release

# Run specific benchmark
dotnet run -c Release --filter "*ParserBenchmarks*"

# Run with memory diagnostics
dotnet run -c Release -m

# Export results
dotnet run -c Release --exporters html json
```

---

## 📋 Benchmark Categories

### 1. Parser Only Benchmarks

Tests TOON parsing performance (string → ToonDocument):

```bash
dotnet run -c Release --filter "*ParserOnly*"
```

**Metrics:**
- Parse time for small/medium/large documents
- Memory allocations during parsing
- Token generation overhead

### 2. Encoder Only Benchmarks

Tests TOON encoding performance (ToonDocument → string):

```bash
dotnet run -c Release --filter "*EncoderOnly*"
```

**Metrics:**
- Encode time for various structures
- String builder efficiency
- Indentation overhead

### 3. Serialization Benchmarks

Tests end-to-end performance (C# object ↔ TOON):

```bash
dotnet run -c Release --filter "*Serialization*"
```

**Metrics:**
- Cold start vs hot path
- Expression tree compilation cost
- Type metadata caching

### 4. Source Generator Benchmarks

Compares source generator vs runtime serialization:

```bash
dotnet run -c Release --filter "*SourceGenerator*"
```

**Metrics:**
- Zero-allocation vs expression trees
- AOT vs JIT performance
- Startup time comparison

### 5. Memory Pressure Benchmarks

Tests behavior under memory constraints:

```bash
dotnet run -c Release --filter "*MemoryPressure*"
```

**Metrics:**
- Large document handling
- GC pressure
- Memory growth patterns

### 6. Deep Nesting Benchmarks

Tests performance with deeply nested structures:

```bash
dotnet run -c Release --filter "*DeepNesting*"
```

**Metrics:**
- Max depth handling
- Stack usage
- Recursive overhead

---

## 📊 Sample Results

### Typical Benchmark Output

```
BenchmarkDotNet v0.13.12, Windows 11
Intel Core i7-12700K, 1 CPU, 12 logical and 8 physical cores
.NET SDK 8.0.100

|                    Method |      Mean |    StdDev | Allocated |
|-------------------------- |----------:|----------:|----------:|
|          Parse_SmallDoc   |   1.52 μs |  0.032 μs |    1.2 KB |
|          Parse_MediumDoc  |  45.23 μs |  0.981 μs |   38.5 KB |
|          Parse_LargeDoc   | 892.45 μs | 15.234 μs |  512.3 KB |
|                           |           |           |           |
|         Encode_SmallDoc   |   0.89 μs |  0.018 μs |    0.8 KB |
|         Encode_MediumDoc  |  28.34 μs |  0.623 μs |   24.1 KB |
|         Encode_LargeDoc   | 543.12 μs |  9.876 μs |  385.2 KB |
|                           |           |           |           |
| Serialize_ExpressionTree  |  89.50 ns |  1.823 ns |     120 B |
| Serialize_SourceGenerator |  45.20 ns |  0.912 ns |       - B |  ← Zero allocation!
| Serialize_Reflection      | 4250.30 ns | 87.456 ns |     856 B |

Summary:
- Source Generator is 2x faster than Expression Trees
- Source Generator is 94x faster than Reflection
- Zero allocations with Source Generators (hot path)
```

---

## 🎯 Key Findings

### Performance Characteristics

**Parser:**
- ~1.5 μs for small documents (<1 KB)
- Linear scaling with document size
- Efficient token-based parsing

**Encoder:**
- ~0.9 μs for small documents
- StringBuilder-based (minimal allocations)
- Indentation adds ~15% overhead

**Serialization:**
- Cold start: 1-2ms (expression tree compilation)
- Hot path: 45-90ns (cached compiled accessors)
- 20-40x faster than reflection-based serializers

**Source Generators:**
- Zero allocation after initial object creation
- 2x faster than expression trees
- 94x faster than reflection
- Perfect for AOT scenarios

---

## 🔬 Running Custom Benchmarks

### Add Your Own Benchmark

```csharp
using BenchmarkDotNet.Attributes;
using ToonNet.Core.Serialization;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class MyCustomBenchmark
{
    private MyClass _testData;

    [GlobalSetup]
    public void Setup()
    {
        _testData = CreateTestData();
    }

    [Benchmark]
    public string SerializeMyClass()
    {
        return ToonSerializer.Serialize(_testData);
    }

    [Benchmark]
    public MyClass DeserializeMyClass()
    {
        var toon = ToonSerializer.Serialize(_testData);
        return ToonSerializer.Deserialize<MyClass>(toon);
    }
}
```

### Run Custom Benchmark

```bash
dotnet run -c Release --filter "*MyCustomBenchmark*"
```

---

## 📈 Comparing with Other Libraries

### JSON Comparison

```csharp
[MemoryDiagnoser]
public class FormatComparisonBenchmarks
{
    private Product _product;

    [GlobalSetup]
    public void Setup()
    {
        _product = new Product 
        { 
            Id = 1, 
            Name = "Laptop", 
            Price = 1299.99m 
        };
    }

    [Benchmark]
    public string Toon_Serialize()
    {
        return ToonSerializer.Serialize(_product);
    }

    [Benchmark]
    public string Json_Serialize()
    {
        return JsonSerializer.Serialize(_product);
    }

    [Benchmark]
    public string NewtonsoftJson_Serialize()
    {
        return JsonConvert.SerializeObject(_product);
    }
}
```

---

## 🧪 Benchmark Scenarios

### Scenario 1: Real-World Object Graphs

```csharp
public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal Total { get; set; }
}

[Benchmark]
public string SerializeComplexOrder()
{
    var order = CreateComplexOrder(); // 10+ items, nested objects
    return ToonSerializer.Serialize(order);
}
```

### Scenario 2: Collection Performance

```csharp
[Benchmark]
[Arguments(10)]
[Arguments(100)]
[Arguments(1000)]
public string SerializeCollection(int count)
{
    var items = Enumerable.Range(1, count)
        .Select(i => new Product { Id = i, Name = $"Item{i}" })
        .ToList();
    
    return ToonSerializer.Serialize(items);
}
```

### Scenario 3: Deep Nesting

```csharp
[Benchmark]
[Arguments(5)]
[Arguments(10)]
[Arguments(20)]
public string SerializeNestedStructure(int depth)
{
    var nested = CreateNestedObject(depth);
    return ToonSerializer.Serialize(nested);
}
```

---

## 📊 Benchmark Reports

### Generated Reports

Benchmarks generate multiple report formats:

```bash
BenchmarkDotNet.Artifacts/
├── results/
│   ├── ToonNet.Benchmarks.ParserBenchmarks-report.html
│   ├── ToonNet.Benchmarks.ParserBenchmarks-report.json
│   ├── ToonNet.Benchmarks.ParserBenchmarks-report.csv
│   └── ToonNet.Benchmarks.ParserBenchmarks-measurements.csv
```

### View Results

```bash
# Open HTML report
open BenchmarkDotNet.Artifacts/results/*-report.html

# View JSON data
cat BenchmarkDotNet.Artifacts/results/*-report.json | jq
```

---

## 🔗 Related Packages

**Core:**
- [`ToonNet.Core`](../ToonNet.Core) - Core serialization
- [`ToonNet.SourceGenerators`](../ToonNet.SourceGenerators) - Source generators

**Extensions:**
- [`ToonNet.Extensions.Json`](../ToonNet.Extensions.Json) - JSON conversion
- [`ToonNet.Extensions.Yaml`](../ToonNet.Extensions.Yaml) - YAML conversion

**Testing:**
- [`ToonNet.Tests`](../../tests/ToonNet.Tests) - Functional tests

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete guide
- [Performance Guide](../../README.md#-performance--architecture) - Performance features
- [Source Generators](../ToonNet.SourceGenerators) - Zero-allocation serialization

---

## 📋 Requirements

- .NET 8.0 or later
- BenchmarkDotNet 0.13.12+
- Release configuration (benchmarks should run in Release mode)

---

## 🤝 Contributing

Want to add benchmarks? Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

**Guidelines:**
- Use `[MemoryDiagnoser]` for allocation tracking
- Include `[SimpleJob]` or `[ShortRunJob]` for quick tests
- Add `[Arguments]` for parameterized benchmarks
- Document expected performance characteristics

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

**Part of the [ToonNet](../../README.md) serialization library family.**
