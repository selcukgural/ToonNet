using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using ToonNet.Core.Serialization;

namespace ToonNet.Benchmarks;

/// <summary>
/// Benchmarks comparing ArrayPool vs GetBytes for memory allocation optimization.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 3, iterationCount: 10)]
public class ArrayPoolOptimizationBenchmarks
{
    private string _smallPayload = null!;
    private string _mediumPayload = null!;
    private string _largePayload = null!;
    private string _veryLargePayload = null!;

    [GlobalSetup]
    public void Setup()
    {
        var person = new TestPerson
        {
            Name = "John Doe",
            Age = 30,
            Email = "john.doe@example.com",
            Address = "123 Main Street, Springfield, USA",
            PhoneNumber = "+1-555-0123"
        };

        // Small: ~100 bytes
        _smallPayload = ToonSerializer.Serialize(person);

        // Medium: ~1KB
        var mediumList = Enumerable.Range(0, 10).Select(i => person).ToList();
        _mediumPayload = ToonSerializer.Serialize(mediumList);

        // Large: ~10KB
        var largeList = Enumerable.Range(0, 100).Select(i => person).ToList();
        _largePayload = ToonSerializer.Serialize(largeList);

        // Very Large: ~100KB
        var veryLargeList = Enumerable.Range(0, 1000).Select(i => person).ToList();
        _veryLargePayload = ToonSerializer.Serialize(veryLargeList);
    }

    #region GetBytes (Old Approach)

    [Benchmark(Baseline = true, Description = "GetBytes - Small (100B)")]
    public byte[] GetBytes_Small()
    {
        return Encoding.UTF8.GetBytes(_smallPayload);
    }

    [Benchmark(Description = "GetBytes - Medium (1KB)")]
    public byte[] GetBytes_Medium()
    {
        return Encoding.UTF8.GetBytes(_mediumPayload);
    }

    [Benchmark(Description = "GetBytes - Large (10KB)")]
    public byte[] GetBytes_Large()
    {
        return Encoding.UTF8.GetBytes(_largePayload);
    }

    [Benchmark(Description = "GetBytes - VeryLarge (100KB)")]
    public byte[] GetBytes_VeryLarge()
    {
        return Encoding.UTF8.GetBytes(_veryLargePayload);
    }

    #endregion

    #region ArrayPool (New Approach)

    [Benchmark(Description = "ArrayPool - Small (100B)")]
    public int ArrayPool_Small()
    {
        return UseArrayPool(_smallPayload);
    }

    [Benchmark(Description = "ArrayPool - Medium (1KB)")]
    public int ArrayPool_Medium()
    {
        return UseArrayPool(_mediumPayload);
    }

    [Benchmark(Description = "ArrayPool - Large (10KB)")]
    public int ArrayPool_Large()
    {
        return UseArrayPool(_largePayload);
    }

    [Benchmark(Description = "ArrayPool - VeryLarge (100KB)")]
    public int ArrayPool_VeryLarge()
    {
        return UseArrayPool(_veryLargePayload);
    }

    private static int UseArrayPool(string payload)
    {
        var encoding = Encoding.UTF8;
        var maxByteCount = encoding.GetMaxByteCount(payload.Length);
        var buffer = ArrayPool<byte>.Shared.Rent(maxByteCount);

        try
        {
            return encoding.GetBytes(payload, 0, payload.Length, buffer, 0);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    #endregion

    #region Stream Write Comparison

    [Benchmark(Description = "StreamWrite_GetBytes - Large")]
    public async Task<long> StreamWrite_GetBytes_Large()
    {
        using var stream = new MemoryStream();
        var bytes = Encoding.UTF8.GetBytes(_largePayload);
        await stream.WriteAsync(bytes);
        return stream.Length;
    }

    [Benchmark(Description = "StreamWrite_ArrayPool - Large")]
    public async Task<long> StreamWrite_ArrayPool_Large()
    {
        using var stream = new MemoryStream();
        
        var encoding = Encoding.UTF8;
        var maxByteCount = encoding.GetMaxByteCount(_largePayload.Length);
        var buffer = ArrayPool<byte>.Shared.Rent(maxByteCount);

        try
        {
            var bytesWritten = encoding.GetBytes(_largePayload, 0, _largePayload.Length, buffer, 0);
            await stream.WriteAsync(buffer.AsMemory(0, bytesWritten));
            return stream.Length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    #endregion

    private class TestPerson
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}

/// <summary>
/// Benchmarks for measuring actual ToonSerializer performance improvements.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 3, iterationCount: 10)]
public class ToonSerializerOptimizationBenchmarks
{
    private TestData _testData = null!;
    private string _tempFilePath = null!;

    [GlobalSetup]
    public void Setup()
    {
        _testData = new TestData
        {
            Id = Guid.NewGuid(),
            Name = "Performance Test Data",
            Timestamp = DateTime.UtcNow,
            Items = Enumerable.Range(0, 100).Select(i => new Item
            {
                Name = $"Item {i}",
                Value = i * 10,
                IsActive = i % 2 == 0
            }).ToList()
        };

        _tempFilePath = Path.GetTempFileName();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Benchmark(Description = "SerializeToStream - With ArrayPool")]
    public async Task SerializeToStream_WithArrayPool()
    {
        using var stream = new MemoryStream();
        await ToonSerializer.SerializeToStreamAsync(_testData, stream);
    }

    [Benchmark(Description = "SerializeToFile - With ArrayPool")]
    public async Task SerializeToFile_WithArrayPool()
    {
        await ToonSerializer.SerializeToFileAsync(_testData, _tempFilePath);
    }

    [Benchmark(Description = "SerializeCollection - With ArrayPool")]
    public async Task SerializeCollection_WithArrayPool()
    {
        using var stream = new MemoryStream();
        await ToonSerializer.SerializeCollectionToStreamAsync(_testData.Items, stream);
    }

    private class TestData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<Item> Items { get; set; } = new();
    }

    private class Item
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsActive { get; set; }
    }
}

/// <summary>
/// Benchmarks for ConfigureAwait performance impact.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 3, iterationCount: 10)]
public class ConfigureAwaitBenchmarks
{
    private readonly TestModel _model = new()
    {
        Name = "Test Model",
        Value = 42,
        Description = "ConfigureAwait performance test"
    };

    [Benchmark(Baseline = true, Description = "SerializeAsync - With ConfigureAwait")]
    public async Task<string> SerializeAsync_WithConfigureAwait()
    {
        return await ToonSerializer.SerializeAsync(_model);
    }

    [Benchmark(Description = "DeserializeAsync - With ConfigureAwait")]
    public async Task<TestModel?> DeserializeAsync_WithConfigureAwait()
    {
        var toon = await ToonSerializer.SerializeAsync(_model);
        return await ToonSerializer.DeserializeAsync<TestModel>(toon);
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
