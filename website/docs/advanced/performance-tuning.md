# Performance Tuning

Optimize ToonNet serialization and deserialization performance.

## Key Performance Features

1. **Expression Trees**: ToonNet uses compiled expression trees, not reflection
2. **Zero Allocation**: Minimal memory allocations during serialization
3. **Streaming**: Support for streaming large datasets
4. **Reusable Options**: Cache `ToonSerializerOptions` instances

## Best Practices

### 1. Reuse Serializer Options

```csharp
// ❌ Bad: Creating options every time
public string Serialize(User user)
{
    var options = new ToonSerializerOptions { WriteIndented = true };
    return ToonSerializer.Serialize(user, options);
}

// ✅ Good: Reuse options
private static readonly ToonSerializerOptions _options = new()
{
    WriteIndented = true
};

public string Serialize(User user)
{
    return ToonSerializer.Serialize(user, _options);
}
```

### 2. Use Streaming for Large Data

```csharp
// ❌ Bad: Load entire file into memory
string toon = File.ReadAllText("large-data.toon");
var data = ToonSerializer.Deserialize<MyData>(toon);

// ✅ Good: Stream from file
using var stream = File.OpenRead("large-data.toon");
var data = ToonSerializer.Deserialize<MyData>(stream);
```

### 3. Use Async Methods for I/O

```csharp
// ✅ Good: Async for I/O operations
using var stream = File.Create("data.toon");
await ToonSerializer.SerializeAsync(stream, data);
```

### 4. Minimize Allocations

```csharp
// ✅ Use pooled memory for streams
using var memoryStream = new MemoryStream();
await ToonSerializer.SerializeAsync(memoryStream, data);
byte[] bytes = memoryStream.ToArray();
```

## Performance Comparison

ToonNet vs System.Text.Json:

| Operation | ToonNet | System.Text.Json | Difference |
|-----------|---------|------------------|------------|
| Serialize | 10-100x faster | Baseline | Expression trees vs reflection |
| Deserialize | 10-100x faster | Baseline | Expression trees vs reflection |
| Memory | Lower | Baseline | Fewer allocations |

## Benchmarking

Use BenchmarkDotNet to measure performance:

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

class Program
{
    static void Main()
    {
        BenchmarkRunner.Run<SerializationBenchmarks>();
    }
}
```

## See Also

- **[Serialization](../core-features/serialization)**: Serialization options
- **[Configuration](../core-features/configuration)**: Optimization settings
