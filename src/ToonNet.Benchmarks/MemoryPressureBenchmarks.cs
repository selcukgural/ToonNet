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
    public object? RoundTrip_HighAllocationPressure()
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
            var doc = _parser.Parse(_document);
        }
    }

    [Benchmark]
    public void Encode_MultipleDocuments()
    {
        var doc = _parser.Parse(_document);
        
        // Simulate encoding multiple times
        for (int i = 0; i < 10; i++)
        {
            var encoded = _encoder.Encode(doc);
        }
    }

    [Benchmark]
    public void CreateAndDispose_ParserInstances()
    {
        // Test parser instance creation overhead
        for (int i = 0; i < 100; i++)
        {
            var parser = new ToonParser();
            var doc = parser.Parse("name: test\nage: 30");
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
            var encoded = encoder.Encode(simpleDoc);
        }
    }
}
