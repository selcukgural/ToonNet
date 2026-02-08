# Streaming Serialization

Learn how to efficiently serialize and deserialize large datasets using ToonNet's streaming API.

## Overview

When working with millions of records, traditional serialization approaches can exhaust memory. ToonNet's streaming API provides:

- **Constant Memory Usage**: O(1) memory regardless of dataset size
- **Batched I/O**: 2-3x faster throughput with configurable batch sizes
- **Cancellation Support**: Full `CancellationToken` propagation
- **Separator Modes**: Choose between blank-line or explicit separator formats

## When to Use Streaming

Use streaming serialization when:
- Processing **millions of records** that don't fit in memory
- Exporting **database tables** incrementally
- Building **ETL pipelines** with large datasets
- Processing **multi-GB log files**
- Avoiding **OutOfMemoryException** in production

**Memory comparison (1M records):**
- Traditional: ~2GB memory (materialize all items)
- Streaming: ~50MB memory (constant, batch-based)

## Basic Streaming Serialization

### Serialize Database Records

```csharp
using ToonNet.Core.Serialization;

// Export millions of users without loading all into memory
await ToonSerializer.SerializeStreamAsync(
    items: dbContext.Users.AsAsyncEnumerable(),
    filePath: "users_export.toon",
    cancellationToken: cancellationToken
);
```

### Deserialize Incrementally

```csharp
// Process large files without memory pressure
await foreach (var user in ToonSerializer.DeserializeStreamAsync<User>("users_export.toon"))
{
    await ProcessUserAsync(user);  // Only one user in memory at a time
}
```

## Advanced Configuration

### Separator Modes

Choose between two separator modes:

#### 1. BlankLine (Default)

Documents separated by blank lines - compatible with legacy code.

```csharp
await ToonSerializer.SerializeStreamAsync(
    items,
    "data.toon",
    writeOptions: ToonMultiDocumentWriteOptions.BlankLine
);
```

**Output:**
```toon
Name: Alice
Age: 25

Name: Bob
Age: 30

Name: Charlie
Age: 35
```

#### 2. ExplicitSeparator

Documents separated by `---` - deterministic, YAML-like format.

```csharp
await ToonSerializer.SerializeStreamAsync(
    items,
    "data.toon",
    writeOptions: ToonMultiDocumentWriteOptions.ExplicitSeparator
);
```

**Output:**
```toon
Name: Alice
Age: 25
---
Name: Bob
Age: 30
---
Name: Charlie
Age: 35
```

### Batch Size Configuration

Control the batch size for optimal throughput:

```csharp
var writeOptions = new ToonMultiDocumentWriteOptions
{
    Mode = ToonMultiDocumentSeparatorMode.BlankLine,
    BatchSize = 100  // Buffer 100 items before writing
};

await ToonSerializer.SerializeStreamAsync(
    items: GenerateLargeDatasetAsync(),
    filePath: "export.toon",
    options: serializerOptions,
    writeOptions: writeOptions,
    cancellationToken: cts.Token
);
```

**Batch size guidelines:**
- **Small items (<1KB)**: Use 100-200 for best throughput
- **Medium items (1-10KB)**: Use 50-100 (default: 50)
- **Large items (>10KB)**: Use 10-50 to limit memory spikes

## Complete Examples

### Database Export

Export an entire database table incrementally:

```csharp
using Microsoft.EntityFrameworkCore;
using ToonNet.Core.Serialization;

public async Task ExportUsersAsync(CancellationToken cancellationToken)
{
    using var dbContext = new MyDbContext();
    
    await ToonSerializer.SerializeStreamAsync(
        items: dbContext.Users
            .AsNoTracking()  // No change tracking for read-only export
            .AsAsyncEnumerable(),
        filePath: "users_backup.toon",
        cancellationToken: cancellationToken
    );
    
    Console.WriteLine("Export completed successfully!");
}
```

### ETL Pipeline

Transform and export data incrementally:

```csharp
public async Task RunEtlPipelineAsync(CancellationToken cancellationToken)
{
    await ToonSerializer.SerializeStreamAsync(
        items: ExtractTransformAsync(),
        filePath: "transformed_data.toon",
        writeOptions: new ToonMultiDocumentWriteOptions { BatchSize = 100 },
        cancellationToken: cancellationToken
    );
}

private async IAsyncEnumerable<TransformedData> ExtractTransformAsync()
{
    await foreach (var raw in ReadRawDataAsync())
    {
        // Transform each record
        var transformed = new TransformedData
        {
            Id = raw.Id,
            ProcessedAt = DateTime.UtcNow,
            // ... transformation logic
        };
        
        yield return transformed;
    }
}
```

### Log Processing

Process multi-GB log files without memory issues:

```csharp
public async Task AnalyzeLogsAsync(string logFilePath)
{
    int errorCount = 0;
    int warningCount = 0;
    
    await foreach (var logEntry in ToonSerializer.DeserializeStreamAsync<LogEntry>(logFilePath))
    {
        switch (logEntry.Level)
        {
            case "ERROR":
                errorCount++;
                await AlertAsync(logEntry);
                break;
            case "WARNING":
                warningCount++;
                break;
        }
    }
    
    Console.WriteLine($"Errors: {errorCount}, Warnings: {warningCount}");
}
```

### Progress Reporting

Report progress during long-running operations:

```csharp
public async Task ExportWithProgressAsync(IProgress<int> progress, CancellationToken cancellationToken)
{
    int count = 0;
    
    await ToonSerializer.SerializeStreamAsync(
        items: TrackProgressAsync(progress, ref count),
        filePath: "export.toon",
        cancellationToken: cancellationToken
    );
}

private async IAsyncEnumerable<User> TrackProgressAsync(IProgress<int> progress, ref int count)
{
    await foreach (var user in dbContext.Users.AsAsyncEnumerable())
    {
        yield return user;
        
        if (++count % 1000 == 0)
        {
            progress.Report(count);
        }
    }
}
```

## Roundtrip Compatibility

Ensure write and read options match for successful roundtrips:

```csharp
var writeOptions = ToonMultiDocumentWriteOptions.ExplicitSeparator;
var readOptions = ToonMultiDocumentReadOptions.ExplicitSeparator;

// Write
await ToonSerializer.SerializeStreamAsync(items, "data.toon", writeOptions: writeOptions);

// Read
await foreach (var item in ToonSerializer.DeserializeStreamAsync<Item>("data.toon", readOptions: readOptions))
{
    ProcessItem(item);
}
```

## Performance Benchmarks

Real-world performance measurements (Apple M3 Max, .NET 8.0):

### 1,000 Records
- **Traditional**: 5ms, 2MB allocated
- **Streaming (batch=50)**: 4ms, 100KB allocated (95% less memory)

### 10,000 Records
- **Traditional**: 45ms, 20MB allocated
- **Streaming (batch=50)**: 22ms, 800KB allocated (2x faster, 96% less memory)

### 100,000 Records
- **Traditional**: 450ms, 200MB allocated
- **Streaming (batch=50)**: 150ms, 6MB allocated (3x faster, 97% less memory)

### 1,000,000 Records
- **Traditional**: OOM risk, ~2GB+
- **Streaming (batch=50)**: 1.5s, 50MB allocated (constant memory)

## API Reference

### SerializeStreamAsync Methods

```csharp
// File-based, default options
ValueTask SerializeStreamAsync<T>(
    IAsyncEnumerable<T> items,
    string filePath,
    ToonSerializerOptions? options = null,
    CancellationToken cancellationToken = default);

// File-based, custom write options
ValueTask SerializeStreamAsync<T>(
    IAsyncEnumerable<T> items,
    string filePath,
    ToonSerializerOptions? options,
    ToonMultiDocumentWriteOptions writeOptions,
    CancellationToken cancellationToken = default);

// Stream-based, default options
ValueTask SerializeStreamAsync<T>(
    IAsyncEnumerable<T> items,
    Stream stream,
    ToonSerializerOptions? options = null,
    CancellationToken cancellationToken = default);

// Stream-based, custom write options
ValueTask SerializeStreamAsync<T>(
    IAsyncEnumerable<T> items,
    Stream stream,
    ToonSerializerOptions? options,
    ToonMultiDocumentWriteOptions writeOptions,
    CancellationToken cancellationToken = default);
```

### DeserializeStreamAsync Methods

```csharp
// File-based, default separator
IAsyncEnumerable<T?> DeserializeStreamAsync<T>(
    string filePath,
    ToonSerializerOptions? options = null,
    CancellationToken cancellationToken = default);

// File-based, custom separator
IAsyncEnumerable<T?> DeserializeStreamAsync<T>(
    string filePath,
    ToonSerializerOptions? options,
    ToonMultiDocumentReadOptions multiDocumentOptions,
    CancellationToken cancellationToken = default);

// Stream-based, default separator
IAsyncEnumerable<T?> DeserializeStreamAsync<T>(
    StreamReader reader,
    ToonSerializerOptions? options = null,
    CancellationToken cancellationToken = default);

// Stream-based, custom separator
IAsyncEnumerable<T?> DeserializeStreamAsync<T>(
    StreamReader reader,
    ToonSerializerOptions? options,
    ToonMultiDocumentReadOptions multiDocumentOptions,
    CancellationToken cancellationToken = default);
```

## Best Practices

1. **Always use cancellation tokens** for long-running operations
2. **Tune batch size** based on item size and available memory
3. **Use `AsNoTracking()`** for Entity Framework read-only queries
4. **Match separator modes** for write/read roundtrips
5. **Monitor memory usage** in production with metrics
6. **Use explicit separators** for new projects (more deterministic)
7. **Buffer appropriately** - larger batches = better throughput, more memory

## Troubleshooting

### Issue: OutOfMemoryException

**Cause**: Batch size too large for item size.

**Solution**: Reduce batch size:
```csharp
writeOptions: new ToonMultiDocumentWriteOptions { BatchSize = 10 }
```

### Issue: Slow throughput

**Cause**: Batch size too small, excessive I/O.

**Solution**: Increase batch size:
```csharp
writeOptions: new ToonMultiDocumentWriteOptions { BatchSize = 200 }
```

### Issue: Parse errors on deserialization

**Cause**: Separator mode mismatch.

**Solution**: Match write and read options:
```csharp
// Write
await SerializeStreamAsync(..., ToonMultiDocumentWriteOptions.ExplicitSeparator);

// Read
await DeserializeStreamAsync(..., ToonMultiDocumentReadOptions.ExplicitSeparator);
```

## See Also

- **[Serialization](serialization)**: Core serialization guide
- **[Deserialization](deserialization)**: Core deserialization guide
- **[Performance Tuning](../advanced/performance-tuning)**: Optimization strategies
- **[API Guide](../api-guide)**: Complete API reference

