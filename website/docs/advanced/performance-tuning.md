# Performance Tuning

Optimize ToonNet serialization and deserialization performance.

## Key Performance Features

1. **Cached metadata and compiled accessors**: Type metadata is read with reflection once per type and cached;
   property getters, setters and constructors are compiled, and primitive properties are read and written through
   typed delegates without boxing
2. **Low-allocation text handling**: The parser reads values from spans and rents its line table from `ArrayPool`;
   the encoder writes into a pooled character buffer, and stream and file output is written as UTF-8 in pooled chunks
   without an intermediate string
3. **UTF-8 in and out**: `Deserialize` over `ReadOnlySpan<byte>`, `ReadOnlySpan<char>` and `Stream` decodes into a
   pooled buffer that the parser reads in place; `SerializeToUtf8Bytes` and `Serialize` to an `IBufferWriter<byte>` or
   a `Stream` encode straight from the pooled buffer. Neither creates an intermediate string
4. **Source generator (optional)**: [`ToonNet.SourceGenerators`](source-generators) generates
   `Serialize`/`Deserialize` methods at compile time
5. **Streaming**: Write and read large datasets one document at a time
6. **Reusable Options**: Cache `ToonSerializerOptions` instances

## Best Practices

### 1. Reuse Serializer Options

```csharp
// ❌ Bad: Creating options every time
public string Serialize(User user)
{
    var options = new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase };
    return ToonSerializer.Serialize(user, options);
}

// ✅ Good: Reuse options
private static readonly ToonSerializerOptions _options = new()
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase
};

public string Serialize(User user)
{
    return ToonSerializer.Serialize(user, _options);
}
```

**Thread-safety note:** Reuse options across calls, but do not mutate a single `ToonSerializerOptions`
instance concurrently across threads.

### 2. Use Streaming for Large Datasets

**For datasets that don't fit in memory**, use streaming serialization:

```csharp
// ❌ Bad: Load all data into memory first
var allUsers = await dbContext.Users.ToListAsync();  // OOM risk with millions of records
await ToonSerializer.SerializeCollectionToFileAsync(allUsers, "users.toon");

// ✅ Good: Stream incrementally (memory bounded by the batch, not the dataset)
await ToonSerializer.SerializeDocumentsAsync(
    dbContext.Users.AsAsyncEnumerable(),
    "users.toon"
);

// ✅ Better: Stream with batching for optimal throughput
await ToonSerializer.SerializeDocumentsAsync(
    dbContext.Users.AsAsyncEnumerable(),
    "users.toon",
    options: null,
    writeOptions: new ToonMultiDocumentWriteOptions { BatchSize = 100 }
);
```

**What this changes:**
- **Memory:** Only the current batch of serialized documents is buffered (`BatchSize`, default 50), instead of the
  whole collection
- **Throughput:** Larger batches mean fewer write calls; measure the right size for your item size
- **Format:** Documents are separated by a blank line (or `---` with `ToonMultiDocumentWriteOptions.ExplicitSeparator`),
  which `DeserializeDocumentsAsync` reads back one by one

**Read back efficiently:**
```csharp
// ❌ Bad: read every document into a list first (a multi-document file is not one array: Deserialize<List<User>>
// of the whole text would fail to parse)
var users = new List<User?>();
await foreach (var u in ToonSerializer.DeserializeDocumentsAsync<User>("users.toon"))
{
    users.Add(u);
}

// ✅ Good: Stream incrementally
await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<User>("users.toon"))
{
    await ProcessUserAsync(user);  // Documents are read and deserialized one at a time
}
```

### 3. Use Async Methods for I/O

```csharp
// ✅ Good: Async for I/O operations
await ToonSerializer.SerializeToFileAsync(data, "data.toon");

await using var stream = File.Create("data.toon");
await ToonSerializer.SerializeToStreamAsync(data, stream);
```

### 4. Use the Source Generator for Hot Paths

`[ToonSerializable]` with [`ToonNet.SourceGenerators`](source-generators) converts supported property types
without the reflection-based metadata path (other types, and types that a converter in the options applies to, use
the reflection-based path). The generated code produces the same TOON as `ToonSerializer`.

`ToonSerializer` writes objects straight to text and reads them straight from it either way; with generated code the
property access, key encoding and matching, and primitive conversions are compiled in, which makes it faster than
the reflection-based path. In the repository's benchmarks on lists of 10,000 objects
([RESULTS.md](https://github.com/selcukgural/ToonNet/blob/master/benchmark/ToonNet.Comparison/RESULTS.md)),
serializing with generated code takes 5–30% less time and deserializing 6–14% less (least for keyed tables).

## Performance Comparison

`benchmark/ToonNet.Comparison` compares ToonNet with the official .NET TOON library (Toon.Format) and with
System.Text.Json on four data shapes (flat records, nested records with arrays, nested field groups and keyed
tables). ToonNet writes the same text as Toon.Format several times faster and with a fraction of the memory, and
is close to System.Text.Json; the measured numbers are in [ToonNet vs Toon.Format](../comparison). Run the comparison and
the parity check on your own hardware:

```bash
dotnet run -c Release --project benchmark/ToonNet.Comparison -- parity
dotnet run -c Release --project benchmark/ToonNet.Comparison -- --filter "*"
```

`benchmark/ToonNet.Benchmarks` measures ToonNet's internals (encoder, parser, streaming, generated and
reflection-based paths).

## Benchmarking

Use BenchmarkDotNet to measure performance:

### Basic Serialization Benchmark

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ToonNet.Core.Serialization;

[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private Person _person;
    private ToonSerializerOptions _options;
    
    [GlobalSetup]
    public void Setup()
    {
        _person = new Person { Name = "Alice", Age = 30 };
        _options = new ToonSerializerOptions();
    }
    
    [Benchmark]
    public string SerializeWithOptions()
    {
        return ToonSerializer.Serialize(_person, _options);
    }
    
    [Benchmark]
    public string SerializeWithoutOptions()
    {
        return ToonSerializer.Serialize(_person);
    }
}
```

### Streaming Benchmark (Large Datasets)

```csharp
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class StreamingBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int ItemCount { get; set; }

    [Benchmark(Baseline = true)]
    public async Task SerializeCollectionToFile_Materialized()
    {
        var items = GenerateItems(ItemCount).ToList();  // Load all into memory
        await ToonSerializer.SerializeCollectionToFileAsync(items, "test.toon");
    }

    [Benchmark]
    public async Task SerializeDocumentsAsync_Incremental()
    {
        await ToonSerializer.SerializeDocumentsAsync(
            GenerateItemsAsync(ItemCount),
            "test.toon"
        );
    }

    [Benchmark]
    public async Task SerializeDocumentsAsync_Batched()
    {
        await ToonSerializer.SerializeDocumentsAsync(
            GenerateItemsAsync(ItemCount),
            "test.toon",
            options: null,
            writeOptions: new ToonMultiDocumentWriteOptions { BatchSize = 100 }
        );
    }

    private IEnumerable<TestItem> GenerateItems(int count)
    {
        for (int i = 0; i < count; i++)
            yield return new TestItem { Id = i, Name = $"Item{i}" };
    }

    private async IAsyncEnumerable<TestItem> GenerateItemsAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return new TestItem { Id = i, Name = $"Item{i}" };
        }
    }
}

class Program
{
    static void Main()
    {
        BenchmarkRunner.Run<SerializationBenchmarks>();
        BenchmarkRunner.Run<StreamingBenchmarks>();
    }
}
```

Compare the `Allocated` column for the materialized and streaming variants to see how much memory streaming
saves for your item type.

## See Also

- **[Serialization](../core-features/serialization)**: Serialization options
- **[Configuration](../core-features/configuration)**: Optimization settings
