# Performance Tuning

Optimize ToonNet serialization and deserialization performance.

## Key Performance Features

1. **Cached metadata and compiled accessors**: Type metadata is read with reflection once per type and cached;
   property getters, setters and constructors are compiled, and primitive properties are read and written through
   typed delegates without boxing
2. **Low-allocation text handling**: The parser reads values from spans and rents its line table from `ArrayPool`;
   the encoder writes into a pooled character buffer, and stream and file output is written as UTF-8 in pooled chunks
   without an intermediate string
3. **Source generator (optional)**: [`ToonNet.SourceGenerators`](source-generators) generates
   `Serialize`/`Deserialize` methods at compile time
4. **Streaming**: Write and read large datasets one document at a time
5. **Reusable Options**: Cache `ToonSerializerOptions` instances

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
await ToonSerializer.SerializeStreamAsync(
    dbContext.Users.AsAsyncEnumerable(),
    "users.toon"
);

// ✅ Better: Stream with batching for optimal throughput
await ToonSerializer.SerializeStreamAsync(
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
  which `DeserializeStreamAsync` reads back one by one

**Read back efficiently:**
```csharp
// ❌ Bad: Load entire file
var users = ToonSerializer.Deserialize<List<User>>(File.ReadAllText("users.toon"));

// ✅ Good: Stream incrementally
await foreach (var user in ToonSerializer.DeserializeStreamAsync<User>("users.toon"))
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
without the reflection-based metadata path (other types, and options with converters, fall back to
`ToonSerializer`). The generated code produces the same TOON as `ToonSerializer`.

`ToonSerializer` writes objects straight to text and reads them straight from it either way; with generated code the
property access, key encoding and matching, and primitive conversions are compiled in, which makes it faster than
the reflection-based path (about 10-30% for lists of 10,000 objects in the repository's benchmarks).

## Performance Comparison

The repository's benchmarks (`benchmark/ToonNet.Benchmarks`) measure generated and reflection-based
serialization, the encoder, the parser and streaming. `TypedSerializationBenchmarks` serializes and deserializes
lists of 100 and 10,000 objects with both serializer paths and with System.Text.Json as a reference point. TOON
text is produced through a document model and indentation-aware parsing, so expect it to be slower than
System.Text.Json; run the benchmarks on your own hardware and payloads for current numbers:

```bash
dotnet run -c Release --project benchmark/ToonNet.Benchmarks -- --filter "*TypedSerialization*"
```

## Benchmarking

Use BenchmarkDotNet to measure performance:

### Basic Serialization Benchmark

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

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
    public async Task SerializeStreamAsync_Incremental()
    {
        await ToonSerializer.SerializeStreamAsync(
            GenerateItemsAsync(ItemCount),
            "test.toon"
        );
    }

    [Benchmark]
    public async Task SerializeStreamAsync_Batched()
    {
        await ToonSerializer.SerializeStreamAsync(
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
