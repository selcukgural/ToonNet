using BenchmarkDotNet.Attributes;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

namespace ToonNet.Benchmarks;

/// <summary>
///     Encoder-only benchmarks (isolate encoding performance).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 20)]
public class EncoderOnlyBenchmarks
{
    private ToonDocument _simpleObject = null!;
    private ToonDocument _arrayInline = null!;
    private ToonDocument _nestedObject = null!;
    private ToonDocument _largeArray = null!;
    private ToonDocument _deepNesting = null!;
    
    private ToonEncoder _encoder = null!;

    [GlobalSetup]
    public void Setup()
    {
        _encoder = new ToonEncoder();
        
        // Simple object
        var simpleObj = new ToonObject();
        simpleObj["name"] = new ToonString("John Doe");
        simpleObj["age"] = new ToonNumber(30);
        simpleObj["email"] = new ToonString("john@example.com");
        simpleObj["active"] = new ToonBoolean(true);
        simpleObj["score"] = new ToonNumber(95.5);
        _simpleObject = new ToonDocument(simpleObj);

        // Inline array
        var arrayObj = new ToonObject();
        arrayObj["tags"] = new ToonArray(new List<ToonValue>
        {
            new ToonString("javascript"),
            new ToonString("python"),
            new ToonString("rust"),
            new ToonString("go"),
            new ToonString("csharp")
        });
        _arrayInline = new ToonDocument(arrayObj);

        // Nested object
        var country = new ToonObject();
        country["name"] = new ToonString("USA");
        country["code"] = new ToonString("US");
        
        var address = new ToonObject();
        address["street"] = new ToonString("123 Main St");
        address["city"] = new ToonString("Springfield");
        address["country"] = country;
        
        var profile = new ToonObject();
        profile["email"] = new ToonString("alice@example.com");
        profile["address"] = address;
        
        var user = new ToonObject();
        user["name"] = new ToonString("Alice");
        user["profile"] = profile;
        
        var rootNested = new ToonObject();
        rootNested["user"] = user;
        _nestedObject = new ToonDocument(rootNested);

        // Large array (100 items)
        var items = new List<ToonValue>();
        for (int i = 0; i < 100; i++)
        {
            var item = new ToonObject();
            item["id"] = new ToonNumber(i);
            item["name"] = new ToonString($"User_{i}");
            item["email"] = new ToonString($"user{i}@example.com");
            item["active"] = new ToonBoolean(i % 2 == 0);
            items.Add(item);
        }
        
        var largeArrayRoot = new ToonObject();
        largeArrayRoot["users"] = new ToonArray(items);
        _largeArray = new ToonDocument(largeArrayRoot);

        // Deep nesting (20 levels)
        ToonObject CreateNestedObject(int depth)
        {
            if (depth == 0)
            {
                var final = new ToonObject();
                final["final"] = new ToonString("Last level");
                return final;
            }

            var nested = new ToonObject();
            nested["level"] = new ToonNumber(depth);
            nested["data"] = new ToonString($"Value at level {depth}");
            nested["nested"] = CreateNestedObject(depth - 1);
            return nested;
        }
        
        _deepNesting = new ToonDocument(CreateNestedObject(20));
    }

    [Benchmark]
    public string Encode_SimpleObject()
    {
        return _encoder.Encode(_simpleObject);
    }

    [Benchmark]
    public string Encode_InlineArray()
    {
        return _encoder.Encode(_arrayInline);
    }

    [Benchmark]
    public string Encode_NestedObject()
    {
        return _encoder.Encode(_nestedObject);
    }

    [Benchmark]
    public string Encode_LargeArray()
    {
        return _encoder.Encode(_largeArray);
    }

    [Benchmark]
    public string Encode_DeepNesting()
    {
        return _encoder.Encode(_deepNesting);
    }

    [Benchmark]
    public int Encode_StringLength_SimpleObject()
    {
        var result = _encoder.Encode(_simpleObject);
        return result.Length;
    }

    [Benchmark]
    public int Encode_StringLength_LargeArray()
    {
        var result = _encoder.Encode(_largeArray);
        return result.Length;
    }
}
