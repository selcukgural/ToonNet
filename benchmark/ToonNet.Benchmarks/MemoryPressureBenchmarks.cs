using BenchmarkDotNet.Attributes;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

namespace ToonNet.Benchmarks;

/// <summary>
///     Memory pressure benchmarks (allocation-heavy scenarios).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class MemoryPressureBenchmarks
{
    private ToonParser _parser = null!;
    private ToonEncoder _encoder = null!;
    private string _document = null!;

    [GlobalSetup]
    public void Setup()
    {
        _parser = new ToonParser();
        _encoder = new ToonEncoder();
        
        // Generate document with many strings (allocation pressure)
        var items = Enumerable.Range(0, 1000).Select(i => new
        {
            Id = i,
            Name = $"User with a longer name to increase allocation pressure {i}",
            Email = $"very.long.email.address.for.user{i}@example-domain-with-long-name.com",
            Description = $"This is a longer description field that will allocate more memory during parsing and serialization. User ID: {i}",
            Tags = new[] 
            { 
                $"tag_with_long_name_{i % 10}", 
                $"another_long_tag_{i % 5}",
                $"third_tag_for_pressure_{i % 3}"
            }
        });
        
        _document = ToonSerializer.Serialize(items);
    }

    [Benchmark]
    public ToonDocument Parse_HighAllocationPressure()
    {
        return _parser.Parse(_document);
    }

    [Benchmark]
    public string Encode_HighAllocationPressure()
    {
        var doc = _parser.Parse(_document);
        return _encoder.Encode(doc);
    }

    [Benchmark]
    public ToonDocument RoundTrip_HighAllocationPressure()
    {
        var doc = _parser.Parse(_document);
        var encoded = _encoder.Encode(doc);
        return _parser.Parse(encoded);
    }

    [Benchmark]
    public void Parse_MultipleDocuments()
    {
        // Simulate parsing multiple documents in sequence
        for (int i = 0; i < 10; i++)
        {
            _ = _parser.Parse(_document);
        }
    }

    [Benchmark]
    public void Encode_MultipleDocuments()
    {
        var doc = _parser.Parse(_document);
        
        // Simulate encoding multiple times
        for (int i = 0; i < 10; i++)
        {
            _ = _encoder.Encode(doc);
        }
    }

    [Benchmark]
    public void CreateAndDispose_ParserInstances()
    {
        // Test parser instance creation overhead
        for (int i = 0; i < 100; i++)
        {
            var parser = new ToonParser();
            _ = parser.Parse("name: test\nage: 30");
        }
    }

    [Benchmark]
    public void CreateAndDispose_EncoderInstances()
    {
        var simpleObj = new ToonObject();
        simpleObj["name"] = new ToonString("test");
        simpleObj["age"] = new ToonNumber(30);
        var simpleDoc = new ToonDocument(simpleObj);
        
        // Test encoder instance creation overhead
        for (int i = 0; i < 100; i++)
        {
            var encoder = new ToonEncoder();
            _ = encoder.Encode(simpleDoc);
        }
    }

    [Benchmark]
    public async Task SerializeStreamAsync_10K_Items()
    {
        // Test streaming serialization with 10K items
        var tempFile = Path.GetTempFileName();
        try
        {
            await ToonSerializer.SerializeStreamAsync(GenerateTestDataAsync(10_000), tempFile);
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
    public async Task SerializeCollectionToFileAsync_10K_Items()
    {
        // Test collection serialization with 10K items (baseline comparison)
        var tempFile = Path.GetTempFileName();
        try
        {
            var items = Enumerable.Range(0, 10_000).Select(i => new
            {
                Id = i,
                Name = $"User_{i}",
                Email = $"user{i}@example.com"
            }).ToList();
            
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

    private static async IAsyncEnumerable<object> GenerateTestDataAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return new
            {
                Id = i,
                Name = $"User_{i}",
                Email = $"user{i}@example.com"
            };
        }
    }
}
