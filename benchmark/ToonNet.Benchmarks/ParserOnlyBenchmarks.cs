using BenchmarkDotNet.Attributes;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Benchmarks;

/// <summary>
///     Parser-only benchmarks (excluding lexer to isolate parsing performance).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 20)]
public class ParserOnlyBenchmarks
{
    private string _simpleObject = null!;
    private string _arrayInline = null!;
    private string _arrayList = null!;
    private string _nestedObject = null!;
    private string _mixedContent = null!;
    
    private ToonParser _parser = null!;

    [GlobalSetup]
    public void Setup()
    {
        _parser = new ToonParser();
        
        // Simple object
        _simpleObject = @"
name: John Doe
age: 30
email: john@example.com
active: true
score: 95.5
";

        // Inline array
        _arrayInline = @"
tags: javascript, python, rust, go, csharp
numbers: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10
";

        // List-style array
        _arrayList = @"
users:
  - Alice
  - Bob
  - Charlie
  - David
  - Eve
  - Frank
  - Grace
  - Henry
";

        // Nested object
        _nestedObject = @"
user:
  name: Alice
  profile:
    email: alice@example.com
    address:
      street: 123 Main St
      city: Springfield
      country:
        name: USA
        code: US
";

        // Mixed content
        _mixedContent = @"
company: TechCorp
employees[3]:
  - id: 1
    name: Alice
    roles: admin, developer
    profile:
      email: alice@techcorp.com
      verified: true
  - id: 2
    name: Bob
    roles: developer
    profile:
      email: bob@techcorp.com
      verified: false
  - id: 3
    name: Charlie
    roles: designer, manager
    profile:
      email: charlie@techcorp.com
      verified: true
";
    }

    [Benchmark]
    public ToonDocument Parse_SimpleObject()
    {
        return _parser.Parse(_simpleObject);
    }

    [Benchmark]
    public ToonDocument Parse_InlineArray()
    {
        return _parser.Parse(_arrayInline);
    }

    [Benchmark]
    public ToonDocument Parse_ListStyleArray()
    {
        return _parser.Parse(_arrayList);
    }

    [Benchmark]
    public ToonDocument Parse_NestedObject()
    {
        return _parser.Parse(_nestedObject);
    }

    [Benchmark]
    public ToonDocument Parse_MixedContent()
    {
        return _parser.Parse(_mixedContent);
    }

    [Benchmark]
    public ToonValue? ParseAndAccess_SimpleObject()
    {
        var doc = _parser.Parse(_simpleObject);
        var root = (ToonObject)doc.Root;
        var name = root["name"];
        var age = root["age"];
        return root["email"];
    }

    [Benchmark]
    public ToonValue? ParseAndAccess_NestedObject()
    {
        var doc = _parser.Parse(_nestedObject);
        var root = (ToonObject)doc.Root;
        var user = (ToonObject)root["user"]!;
        var profile = (ToonObject)user["profile"]!;
        var address = (ToonObject)profile["address"]!;
        var country = (ToonObject)address["country"]!;
        return country["code"];
    }

    [Benchmark]
    public int ParseAndIterate_ArrayCount()
    {
        var doc = _parser.Parse(_mixedContent);
        var root = (ToonObject)doc.Root;
        var employees = (ToonArray)root["employees"]!;
        return employees.Items.Count;
    }
}
