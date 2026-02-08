using BenchmarkDotNet.Attributes;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

namespace ToonNet.Benchmarks;

/// <summary>
///     Benchmarks for large documents (10KB, 100KB, 1MB).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class LargeDocumentBenchmarks
{
    private string _smallDocument = null!; // ~10KB
    private string _mediumDocument = null!; // ~100KB
    private string _largeDocument = null!; // ~1MB
    
    private ToonParser _parser = null!;
    private ToonEncoder _encoder = null!;

    [GlobalSetup]
    public void Setup()
    {
        _parser = new ToonParser();
        _encoder = new ToonEncoder();
        
        // Generate 10KB document (~100 objects with 10 properties each)
        _smallDocument = GenerateDocument(100);
        
        // Generate 100KB document (~1000 objects)
        _mediumDocument = GenerateDocument(1000);
        
        // Generate 1MB document (~10000 objects)
        _largeDocument = GenerateDocument(10000);
    }

    private static string GenerateDocument(int objectCount)
    {
        var items = new List<object>();
        for (int i = 0; i < objectCount; i++)
        {
            items.Add(new
            {
                Id = i,
                Name = $"User_{i}",
                Email = $"user{i}@example.com",
                Age = 20 + (i % 50),
                Score = 50.0 + (i % 50),
                IsActive = i % 2 == 0,
                Department = $"Dept_{i % 10}",
                Salary = 50000 + (i % 50000),
                Rating = 3.0 + (i % 3),
                Tags = new[] { $"tag{i % 5}", $"tag{i % 7}" }
            });
        }
        
        return ToonSerializer.Serialize(items);
    }

    [Benchmark]
    public ToonDocument Parse_10KB()
    {
        return _parser.Parse(_smallDocument);
    }

    [Benchmark]
    public ToonDocument Parse_100KB()
    {
        return _parser.Parse(_mediumDocument);
    }

    [Benchmark]
    public ToonDocument Parse_1MB()
    {
        return _parser.Parse(_largeDocument);
    }

    [Benchmark]
    public string Encode_10KB()
    {
        var doc = _parser.Parse(_smallDocument);
        return _encoder.Encode(doc);
    }

    [Benchmark]
    public string Encode_100KB()
    {
        var doc = _parser.Parse(_mediumDocument);
        return _encoder.Encode(doc);
    }

    [Benchmark]
    public string Encode_1MB()
    {
        var doc = _parser.Parse(_largeDocument);
        return _encoder.Encode(doc);
    }

    [Benchmark]
    public object? RoundTrip_10KB()
    {
        var doc = _parser.Parse(_smallDocument);
        var encoded = _encoder.Encode(doc);
        return _parser.Parse(encoded);
    }

    [Benchmark]
    public object? RoundTrip_100KB()
    {
        var doc = _parser.Parse(_mediumDocument);
        var encoded = _encoder.Encode(doc);
        return _parser.Parse(encoded);
    }
}
