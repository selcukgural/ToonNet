using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Benchmarks;

/// <summary>
///     Benchmarks for deeply nested documents (stress test for MaxDepth).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class DeepNestingBenchmarks
{
    private string _depth10 = null!;
    private string _depth25 = null!;
    private string _depth50 = null!;
    private string _depth75 = null!;
    
    private ToonParser _parser = null!;
    private ToonEncoder _encoder = null!;

    [GlobalSetup]
    public void Setup()
    {
        _parser = new ToonParser();
        _encoder = new ToonEncoder();
        
        _depth10 = GenerateNestedDocument(10);
        _depth25 = GenerateNestedDocument(25);
        _depth50 = GenerateNestedDocument(50);
        _depth75 = GenerateNestedDocument(75);
    }

    private static string GenerateNestedDocument(int depth)
    {
        var indent = "";
        var lines = new List<string>();
        
        // Build nested structure
        for (int i = 0; i < depth; i++)
        {
            lines.Add($"{indent}level{i}:");
            indent += "  ";
            lines.Add($"{indent}data: Value at level {i}");
            lines.Add($"{indent}index: {i}");
            lines.Add($"{indent}nested:");
            indent += "  ";
        }
        
        // Add final value
        lines.Add($"{indent}final: Last level");
        
        return string.Join("\n", lines);
    }

    [Benchmark]
    public ToonDocument Parse_Depth10()
    {
        return _parser.Parse(_depth10);
    }

    [Benchmark]
    public ToonDocument Parse_Depth25()
    {
        return _parser.Parse(_depth25);
    }

    [Benchmark]
    public ToonDocument Parse_Depth50()
    {
        return _parser.Parse(_depth50);
    }

    [Benchmark]
    public ToonDocument Parse_Depth75()
    {
        return _parser.Parse(_depth75);
    }

    [Benchmark]
    public string Encode_Depth10()
    {
        var doc = _parser.Parse(_depth10);
        return _encoder.Encode(doc);
    }

    [Benchmark]
    public string Encode_Depth50()
    {
        var doc = _parser.Parse(_depth50);
        return _encoder.Encode(doc);
    }

    [Benchmark]
    public object? RoundTrip_Depth25()
    {
        var doc = _parser.Parse(_depth25);
        var encoded = _encoder.Encode(doc);
        return _parser.Parse(encoded);
    }

    [Benchmark]
    public object? RoundTrip_Depth50()
    {
        var doc = _parser.Parse(_depth50);
        var encoded = _encoder.Encode(doc);
        return _parser.Parse(encoded);
    }
}
