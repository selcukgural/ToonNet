# ToonNet.Benchmarks

**Performance benchmarks for ToonNet serialization**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![BenchmarkDotNet](https://img.shields.io/badge/BenchmarkDotNet-v0.15.0-blue)](#)

---

## 📊 What is ToonNet.Benchmarks?

ToonNet.Benchmarks is a [BenchmarkDotNet](https://benchmarkdotnet.org/) console project with performance tests for ToonNet:

- ⚡ **Parser Benchmarks** - TOON parsing performance (`ParserOnlyBenchmarks`)
- 🔄 **Encoder Benchmarks** - TOON encoding performance (`EncoderOnlyBenchmarks`)
- 🎯 **Generated vs. `ToonSerializer`** - source-generated methods compared with `ToonSerializer` (`SimpleBenchmarks`, `MediumBenchmarks`, `ComplexBenchmarks`)
- ⏱️ **Async & Streaming Benchmarks** - async, file, stream and `SerializeStreamAsync` APIs (`AsyncBenchmarks`, `StreamingSerializationBenchmarks`)
- 🧠 **Memory Benchmarks** - allocation tracking (`MemoryPressureBenchmarks`, `ArrayPoolOptimizationBenchmarks`)
- 📈 **Scalability Tests** - large documents, deep nesting (`LargeDocumentBenchmarks`, `DeepNestingBenchmarks`)

All benchmark classes use `[MemoryDiagnoser]`, so allocations are reported for every run.

---

## 🚀 Quick Start

### Running Benchmarks

`Program.cs` currently runs a single class, `ArrayPoolOptimizationBenchmarks`, through `BenchmarkRunner.Run<T>()`.
Command-line arguments such as `--filter` are **not** forwarded to BenchmarkDotNet.

```bash
# From the repository root
dotnet run -c Release --project benchmark/ToonNet.Benchmarks
```

To run another class, change the type argument in `Program.cs`, for example:

```csharp
var summary = BenchmarkRunner.Run<ToonNet.Benchmarks.ParserOnlyBenchmarks>(config);
```

> If you want to select benchmarks from the command line, replace the `BenchmarkRunner.Run<...>` call with
> `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config)`. The filters below assume
> that change (`dotnet run -c Release --project benchmark/ToonNet.Benchmarks -- --filter "*ParserOnly*"`).

---

## 📋 Benchmark Categories

| Class | File | Filter (with `BenchmarkSwitcher`) |
|-------|------|-----------------------------------|
| `ParserOnlyBenchmarks` | `ParserOnlyBenchmarks.cs` | `*ParserOnly*` |
| `EncoderOnlyBenchmarks` | `EncoderOnlyBenchmarks.cs` | `*EncoderOnly*` |
| `SimpleBenchmarks`, `MediumBenchmarks`, `ComplexBenchmarks` | `Benchmarks.cs` | `*SimpleBenchmarks*`, `*MediumBenchmarks*`, `*ComplexBenchmarks*` |
| `AsyncBenchmarks` | `AsyncBenchmarks.cs` | `*AsyncBenchmarks*` |
| `StreamingSerializationBenchmarks` | `StreamingSerializationBenchmarks.cs` | `*StreamingSerialization*` |
| `MemoryPressureBenchmarks` | `MemoryPressureBenchmarks.cs` | `*MemoryPressure*` |
| `LargeDocumentBenchmarks` | `LargeDocumentBenchmarks.cs` | `*LargeDocument*` |
| `DeepNestingBenchmarks` | `DeepNestingBenchmarks.cs` | `*DeepNesting*` |
| `ArrayPoolOptimizationBenchmarks`, `ToonSerializerOptimizationBenchmarks`, `ConfigureAwaitBenchmarks` | `OptimizationBenchmarks.cs` | `*Optimization*`, `*ConfigureAwait*` |

### 1. Parser Only Benchmarks

Tests TOON parsing performance (string → `ToonDocument`) with `ToonParser`:

- `Parse_SimpleObject`, `Parse_InlineArray`, `Parse_ListStyleArray`, `Parse_NestedObject`, `Parse_MixedContent`
- `ParseAndAccess_*` and `ParseAndIterate_ArrayCount` - parsing plus reading values

### 2. Encoder Only Benchmarks

Tests TOON encoding performance (`ToonDocument` → string) with `ToonEncoder`:

- `Encode_SimpleObject`, `Encode_InlineArray`, `Encode_NestedObject`, `Encode_LargeArray`, `Encode_DeepNesting`
- `Encode_StringLength_*` - encoding plus measuring the output length

### 3. Generated vs. `ToonSerializer` Benchmarks

`SimpleBenchmarks`, `MediumBenchmarks` and `ComplexBenchmarks` (in `Benchmarks.cs`) use the `[ToonSerializable]`
models in `Models/BenchmarkModels.cs` and compare:

- `SerializeGenerated` / `DeserializeGenerated` - the source-generated static `Serialize`/`Deserialize` methods
- `SerializeReflection` / `DeserializeReflection` - `ToonSerializer.Serialize` / `ToonSerializer.Deserialize<T>`

> **Note:** these are not like-for-like. `SerializeGenerated` returns a `ToonDocument` (no text encoding), while
> `SerializeReflection` returns the encoded TOON string. The deserialize benchmarks also include a serialize step.

### 4. Async & Streaming Benchmarks

- `AsyncBenchmarks` - `SerializeAsync`, `DeserializeAsync`, async parse/encode, file and stream round trips
- `StreamingSerializationBenchmarks` - `SerializeCollectionToFileAsync` (baseline) vs. `SerializeStreamAsync` with
  different batch sizes and the explicit `---` separator, for 1,000 / 10,000 / 100,000 items (`[Params]`)

### 5. Memory Pressure Benchmarks

Tests allocation-heavy workloads:

- Parse/encode/round trip of a large document, many documents in a loop
- Creating many parser and encoder instances
- `SerializeStreamAsync` vs. `SerializeCollectionToFileAsync` for 10,000 items

### 6. Large Document & Deep Nesting Benchmarks

- `LargeDocumentBenchmarks` - parse, encode and round trip of ~10 KB, ~100 KB and ~1 MB documents
- `DeepNestingBenchmarks` - parse at depth 10/25/50/75, encode at depth 10/50, round trip at depth 25/50

### 7. Optimization Benchmarks

`OptimizationBenchmarks.cs` contains micro-benchmarks for implementation choices: `Encoding.GetBytes` vs. `ArrayPool`
buffers (`ArrayPoolOptimizationBenchmarks`), stream/file/collection serialization (`ToonSerializerOptimizationBenchmarks`)
and async calls with `ConfigureAwait(false)` (`ConfigureAwaitBenchmarks`).

---

## 📊 Sample Results

No current results are published. The encoder and parser were rewritten for TOON spec v3.3.2 after the last recorded
run, so earlier numbers no longer describe the library. Run the benchmarks on your own hardware (see
[Quick Start](#-quick-start)) and compare results from the same machine only.

The files in `BenchmarkDotNet.Artifacts/results/` are **historical**: they were produced in February 2026 with
BenchmarkDotNet v0.15.0 on .NET 8.0.11 (Apple M3 Max, macOS 26.2), before the encoder/parser rewrite. Do not quote
them as the performance of the current version.

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

Point `Program.cs` at the new class (`BenchmarkRunner.Run<MyCustomBenchmark>(config)`), then:

```bash
dotnet run -c Release --project benchmark/ToonNet.Benchmarks
```

---

## 📈 Comparing with Other Libraries

### JSON Comparison

The project does not contain a format comparison benchmark. To add one, reference the JSON libraries you want to
compare (`System.Text.Json` is part of .NET; `Newtonsoft.Json` needs a package reference) and write a class like this:

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

The snippets below are templates for new benchmarks, not classes that exist in the project.

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

With the default BenchmarkDotNet configuration, each benchmark class produces an HTML, CSV and GitHub Markdown report
in `BenchmarkDotNet.Artifacts/results/` (relative to the working directory of the run):

```bash
BenchmarkDotNet.Artifacts/
├── results/
│   ├── ToonNet.Benchmarks.ParserOnlyBenchmarks-report.html
│   ├── ToonNet.Benchmarks.ParserOnlyBenchmarks-report.csv
│   └── ToonNet.Benchmarks.ParserOnlyBenchmarks-report-github.md
```

### View Results

```bash
# Open HTML report
open BenchmarkDotNet.Artifacts/results/*-report.html

# View Markdown table
cat BenchmarkDotNet.Artifacts/results/*-report-github.md
```

---

## 🔗 Related Packages

**Core:**
- [`ToonNet.Core`](../../src/ToonNet.Core) - Core serialization
- [`ToonNet.SourceGenerators`](../../src/ToonNet.SourceGenerators) - Source generators

**Extensions:**
- [`ToonNet.Extensions.Json`](../../src/ToonNet.Extensions.Json) - JSON conversion
- [`ToonNet.Extensions.Yaml`](../../src/ToonNet.Extensions.Yaml) - YAML conversion

**Testing:**
- [`ToonNet.Tests`](../../tests/ToonNet.Tests) - Functional tests

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete guide
- [Performance Guide](../../README.md#-performance--architecture) - Performance features
- [Source Generators](../../src/ToonNet.SourceGenerators/README.md) - Generated `Serialize`/`Deserialize` methods

---

## 📋 Requirements

- .NET 8.0 SDK or later (the project targets `net8.0`)
- BenchmarkDotNet 0.15.0 (package reference in `ToonNet.Benchmarks.csproj`)
- Release configuration (benchmarks should run in Release mode)

---

## 🤝 Contributing

Want to add benchmarks? Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

**Guidelines:**
- Use `[MemoryDiagnoser]` for allocation tracking
- Include `[SimpleJob]` or `[ShortRunJob]` for quick tests
- Add `[Arguments]` for parameterized benchmarks
- Document what the benchmark measures; don't commit numbers without the BenchmarkDotNet environment header

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

**Part of the [ToonNet](../../README.md) serialization library family.**
