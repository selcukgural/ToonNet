using BenchmarkDotNet.Attributes;
using ToonNet.Core.Serialization;

namespace ToonNet.Benchmarks;

/// <summary>
///     Benchmarks for streaming serialization vs traditional collection serialization.
///     Measures memory efficiency, throughput, and scalability with large datasets.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class StreamingSerializationBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int ItemCount { get; set; }

    [Benchmark(Baseline = true)]
    public async Task SerializeCollectionToFile_IEnumerable()
    {
        // Baseline: Traditional collection serialization (loads all items into memory)
        var tempFile = Path.GetTempFileName();
        try
        {
            var items = GenerateTestDataSync(ItemCount).ToList(); // MaterializeS all items
            await ToonSerializer.SerializeCollectionToFileAsync(items, tempFile);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Benchmark]
    public async Task SerializeStreamAsync_IAsyncEnumerable()
    {
        // New: Streaming serialization (minimal memory footprint)
        var tempFile = Path.GetTempFileName();
        try
        {
            await ToonSerializer.SerializeStreamAsync(GenerateTestDataAsync(ItemCount), tempFile);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Benchmark]
    public async Task SerializeStreamAsync_WithBatching_BatchSize50()
    {
        // Streaming with default batch size (50)
        var tempFile = Path.GetTempFileName();
        try
        {
            await ToonSerializer.SerializeStreamAsync(
                GenerateTestDataAsync(ItemCount),
                tempFile,
                options: null,
                writeOptions: new ToonMultiDocumentWriteOptions { BatchSize = 50 }
            );
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Benchmark]
    public async Task SerializeStreamAsync_WithBatching_BatchSize100()
    {
        // Streaming with larger batch size (100)
        var tempFile = Path.GetTempFileName();
        try
        {
            await ToonSerializer.SerializeStreamAsync(
                GenerateTestDataAsync(ItemCount),
                tempFile,
                options: null,
                writeOptions: new ToonMultiDocumentWriteOptions { BatchSize = 100 }
            );
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Benchmark]
    public async Task SerializeStreamAsync_ExplicitSeparator()
    {
        // Streaming with explicit separator mode
        var tempFile = Path.GetTempFileName();
        try
        {
            await ToonSerializer.SerializeStreamAsync(
                GenerateTestDataAsync(ItemCount),
                tempFile,
                options: null,
                writeOptions: ToonMultiDocumentWriteOptions.ExplicitSeparator
            );
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private static IEnumerable<TestModel> GenerateTestDataSync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new TestModel
            {
                Id = i,
                Name = $"User_{i}",
                Email = $"user{i}@example.com",
                Age = 20 + (i % 50),
                Score = 50.0 + (i % 100),
                IsActive = i % 2 == 0,
                Department = $"Dept_{i % 10}"
            };
        }
    }

    private static async IAsyncEnumerable<TestModel> GenerateTestDataAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await Task.Yield(); // Simulate async data source (DB query, API call, etc.)
            yield return new TestModel
            {
                Id = i,
                Name = $"User_{i}",
                Email = $"user{i}@example.com",
                Age = 20 + (i % 50),
                Score = 50.0 + (i % 100),
                IsActive = i % 2 == 0,
                Department = $"Dept_{i % 10}"
            };
        }
    }

    private sealed class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public double Score { get; set; }
        public bool IsActive { get; set; }
        public string Department { get; set; } = string.Empty;
    }
}

