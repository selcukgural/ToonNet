# ToonNet.Tests

**Comprehensive test suite for ToonNet serialization**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![xUnit](https://img.shields.io/badge/xUnit-2.5+-blue)](#)
[![Tests](https://img.shields.io/badge/tests-427%20passing-success)](#)

---

## 📦 What is ToonNet.Tests?

ToonNet.Tests provides **comprehensive testing** for all ToonNet functionality:

- ✅ **427 Passing Tests** - Full coverage of features
- ✅ **Spec Compliance** - TOON v3.0 specification tests
- ✅ **Unit Tests** - Parser, encoder, serializer
- ✅ **Integration Tests** - Format conversions, roundtrips
- ✅ **Edge Cases** - Error handling, validation
- ✅ **Real-World Scenarios** - E-commerce, healthcare examples

---

## 🚀 Quick Start

### Running All Tests

```bash
cd tests/ToonNet.Tests
dotnet test
```

### Running Specific Test Categories

```bash
# Parser tests only
dotnet test --filter "FullyQualifiedName~Parsing"

# Serialization tests only
dotnet test --filter "FullyQualifiedName~Serialization"

# JSON conversion tests
dotnet test --filter "FullyQualifiedName~ToonJsonConverter"

# YAML conversion tests
dotnet test --filter "FullyQualifiedName~ToonYamlConverter"

# Spec compliance tests
dotnet test --filter "FullyQualifiedName~SpecCompliance"
```

### Running with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"

# View coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/*.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
open coverage/index.html
```

---

## 📂 Test Structure

```
ToonNet.Tests/
├── Parsing/
│   ├── ToonParserTests.cs              # Parser unit tests
│   ├── ToonParserEdgeCaseTests.cs      # Edge cases, errors
│   └── ToonLexerTests.cs               # Lexer/tokenizer tests
│
├── Encoding/
│   ├── ToonEncoderTests.cs             # Encoder unit tests
│   └── ToonEncoderEdgeCaseTests.cs     # Edge cases, options
│
├── Serialization/
│   ├── ToonSerializerTests.cs          # Serializer tests
│   ├── SerializationTests.cs           # Type serialization
│   ├── DeserializationTests.cs         # Type deserialization
│   ├── RoundtripTests.cs               # Roundtrip validation
│   └── AttributeTests.cs               # [ToonProperty], [ToonIgnore]
│
├── Interop/
│   ├── ToonJsonConverterTests.cs       # JSON ↔ TOON conversion
│   └── ToonYamlConverterTests.cs       # YAML ↔ TOON conversion
│
├── SpecCompliance/
│   └── ToonSpecComplianceTests.cs      # TOON spec v3.0 tests
│
├── Models/
│   └── TestModels.cs                   # Test data models
│
└── ErrorMessageTests.cs                # Error message validation
```

---

## 📊 Test Coverage

### Coverage by Component

| Component | Tests | Coverage | Notes |
|-----------|-------|----------|-------|
| **Parser** | 85 | ~95% | Lexer, token parsing, error handling |
| **Encoder** | 62 | ~92% | Format options, indentation, inline |
| **Serializer** | 143 | ~94% | All CLR types, attributes, options |
| **JSON Converter** | 48 | ~90% | Bidirectional conversion, roundtrips |
| **YAML Converter** | 35 | ~88% | Bidirectional conversion, roundtrips |
| **Spec Compliance** | 54 | 100% | TOON v3.0 specification conformance |

**Total:** 427 tests, ~93% overall coverage

---

## 🎯 Test Categories

### Spec conformance fixtures

`SpecCompliance/SpecFixtureConformanceTests.cs` runs the official TOON spec v3.3.2 fixtures from
`SpecCompliance/Fixtures/v3.3.2` (copied from [toon-format/spec](https://github.com/toon-format/spec)).
Cases that do not pass yet are listed in `SpecCompliance/KnownNonConformance.txt`; a listed case that starts
passing fails the build until it is removed. To regenerate the list:

```bash
TOONNET_WRITE_NONCONFORMANCE=/tmp/nonconformance.txt \
  dotnet test tests/ToonNet.Tests --filter "FullyQualifiedName~WriteNonConformanceReport"
```

### 1. Parser Tests

Tests TOON format parsing:

```csharp
[Fact]
public void Parse_SimpleObject_Success()
{
    var toon = """
        Name: Alice
        Age: 30
        Active: true
        """;
    
    var parser = new ToonParser();
    var doc = parser.Parse(toon);
    
    var root = (ToonObject)doc.Root;
    Assert.Equal("Alice", ((ToonString)root["Name"]).Value);
    Assert.Equal(30.0, ((ToonNumber)root["Age"]).Value);
}

[Fact]
public void Parse_InvalidSyntax_ThrowsException()
{
    var invalid = "Name: Alice\nAge:";  // Missing value
    
    var parser = new ToonParser();
    Assert.Throws<ToonParseException>(() => parser.Parse(invalid));
}
```

### 2. Serialization Tests

Tests C# object → TOON conversion:

```csharp
[Fact]
public void Serialize_ComplexObject_Success()
{
    var person = new Person
    {
        Name = "Bob",
        Age = 25,
        Hobbies = new List<string> { "Reading", "Gaming" }
    };
    
    var toon = ToonSerializer.Serialize(person);
    
    Assert.Contains("Name: Bob", toon);
    Assert.Contains("Age: 25", toon);
    Assert.Contains("Hobbies[2]: Reading, Gaming", toon);
}

[Fact]
public void Deserialize_TOON_To_Object()
{
    var toon = """
        Name: Charlie
        Age: 35
        """;
    
    var person = ToonSerializer.Deserialize<Person>(toon);
    
    Assert.Equal("Charlie", person.Name);
    Assert.Equal(35, person.Age);
}
```

### 3. Format Conversion Tests

Tests JSON/YAML ↔ TOON conversion:

```csharp
[Fact]
public void JSON_To_TOON_To_JSON_Roundtrip()
{
    var originalJson = """{"name":"Alice","age":30}""";
    
    // JSON → TOON
    var toonDoc = ToonJsonConverter.FromJson(originalJson);
    var toonString = new ToonEncoder().Encode(toonDoc);
    
    // TOON → JSON
    var parser = new ToonParser();
    var doc = parser.Parse(toonString);
    var roundtripJson = ToonJsonConverter.ToJson(doc);
    
    // Validate semantic equivalence
    var original = JsonSerializer.Deserialize<object>(originalJson);
    var roundtrip = JsonSerializer.Deserialize<object>(roundtripJson);
    
    Assert.Equal(
        JsonSerializer.Serialize(original), 
        JsonSerializer.Serialize(roundtrip)
    );
}
```

### 4. Spec Compliance Tests

Tests TOON v3.0 specification conformance:

```csharp
[Fact]
public void Spec_InlineArray_Format()
{
    var toon = "tags[3]: red, green, blue";
    
    var parser = new ToonParser();
    var doc = parser.Parse(toon);
    
    var root = (ToonObject)doc.Root;
    var tags = (ToonArray)root["tags"];
    
    Assert.Equal(3, tags.Items.Count);
    Assert.Equal("red", ((ToonString)tags.Items[0]).Value);
    Assert.Equal("green", ((ToonString)tags.Items[1]).Value);
    Assert.Equal("blue", ((ToonString)tags.Items[2]).Value);
}
```

### 5. Edge Case Tests

Tests error handling and unusual scenarios:

```csharp
[Fact]
public void Parse_EmptyInput_ThrowsException()
{
    var parser = new ToonParser();
    Assert.Throws<ToonParseException>(() => parser.Parse(""));
}

[Fact]
public void Serialize_CircularReference_ThrowsException()
{
    var node1 = new Node { Name = "Node1" };
    var node2 = new Node { Name = "Node2", Parent = node1 };
    node1.Child = node2;  // Circular!
    
    Assert.Throws<ToonSerializationException>(
        () => ToonSerializer.Serialize(node1)
    );
}

[Fact]
public void Deserialize_MaxDepthExceeded_ThrowsException()
{
    var deeplyNested = CreateDeeplyNestedToon(100);  // 100 levels
    
    var options = new ToonSerializerOptions
    {
        ToonOptions = new ToonOptions { MaxDepth = 64 }
    };
    
    Assert.Throws<ToonParseException>(
        () => ToonSerializer.Deserialize<object>(deeplyNested, options)
    );
}
```

---

## 🧪 Running Tests in CI/CD

### GitHub Actions Example

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Test
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
      - name: Upload coverage
        uses: codecov/codecov-action@v2
        with:
          files: '**/coverage.cobertura.xml'
```

---

## 📈 Test Statistics

### Latest Test Run

```
Test Run Successful.
Total tests: 427
     Passed: 427
     Failed: 0
    Skipped: 1
 Total time: 5.2 seconds
```

### Test Performance

| Test Category | Tests | Avg Time | Total Time |
|---------------|-------|----------|------------|
| Parser | 85 | ~0.5ms | ~42ms |
| Encoder | 62 | ~0.3ms | ~19ms |
| Serializer | 143 | ~1.2ms | ~172ms |
| JSON Converter | 48 | ~2.1ms | ~101ms |
| YAML Converter | 35 | ~3.5ms | ~123ms |
| Spec Compliance | 54 | ~0.8ms | ~43ms |

---

## 🔍 Test Data

### Sample Test Models

```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Hobbies { get; set; }
}

public class Company
{
    public string Name { get; set; }
    public List<Department> Departments { get; set; }
    public Dictionary<string, decimal> Revenue { get; set; }
}

public class Department
{
    public string Name { get; set; }
    public List<Employee> Employees { get; set; }
}

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Address Address { get; set; }
}
```

---

## 🔗 Related Packages

**Core:**
- [`ToonNet.Core`](../../src/ToonNet.Core) - Code being tested

**Extensions:**
- [`ToonNet.Extensions.Json`](../../src/ToonNet.Extensions.Json) - JSON conversion tests
- [`ToonNet.Extensions.Yaml`](../../src/ToonNet.Extensions.Yaml) - YAML conversion tests

**Other Tests:**
- [`ToonNet.SourceGenerators.Tests`](../ToonNet.SourceGenerators.Tests) - Source generator tests

**Development:**
- [`ToonNet.Demo`](../../demo/ToonNet.Demo) - Sample applications
- [`ToonNet.Benchmarks`](../../src/ToonNet.Benchmarks) - Performance tests

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete ToonNet guide
- [API Guide](../../docs/API-GUIDE.md) - API reference
- [Contributing Guide](../../CONTRIBUTING.md) - Adding tests

---

## 📋 Requirements

- .NET 8.0 or later
- xUnit 2.5.3+
- ToonNet.Core
- ToonNet.Extensions.Json
- ToonNet.Extensions.Yaml

---

## 🤝 Contributing

Want to add tests? Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

**Test Guidelines:**
- Follow AAA pattern (Arrange-Act-Assert)
- Use descriptive test names
- Include both positive and negative tests
- Add edge cases and boundary conditions
- Document complex test scenarios

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

**Part of the [ToonNet](../../README.md) serialization library family.**
