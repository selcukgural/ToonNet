# ToonNet.Tests

**Comprehensive test suite for ToonNet serialization**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![xUnit](https://img.shields.io/badge/xUnit-2.5+-blue)](#)

---

## 📦 What is ToonNet.Tests?

ToonNet.Tests provides **comprehensive testing** for all ToonNet functionality:

- ✅ **Spec Compliance** - official TOON spec v3.3.2 conformance fixtures plus hand-written spec tests
- ✅ **Unit Tests** - Parser, encoder, serializer, `ToonValue`/`ToonDocument` models
- ✅ **Integration Tests** - JSON/YAML conversions, ASP.NET Core configuration and MVC formatters, async/streaming APIs
- ✅ **Edge Cases** - Error handling, depth limits, input and options validation

Source generator tests live in a separate project, [`ToonNet.SourceGenerators.Tests`](../ToonNet.SourceGenerators.Tests).

---

## 🚀 Quick Start

### Running All Tests

```bash
# This project only
cd tests/ToonNet.Tests
dotnet test

# Every test project in the solution (from the repository root)
dotnet test ToonNet.slnx
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

# Spec compliance tests (fixtures + hand-written)
dotnet test --filter "FullyQualifiedName~SpecCompliance"

# ASP.NET Core integration tests
dotnet test --filter "FullyQualifiedName~AspNetCore"
```

### Running with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"

# View coverage report (coverlet.collector is already referenced by the project)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/*.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
open coverage/index.html
```

---

## 📂 Test Structure

```
ToonNet.Tests/
├── AspNetCore/
│   ├── MvcTests.cs                              # ToonInputFormatter / ToonOutputFormatter
│   └── ToonNetServiceCollectionExtensionsTests.cs
│
├── AsyncApi/
│   ├── ToonSerializerAsyncTests.cs              # Async, file and streaming APIs
│   └── ConfigureAwaitTests.cs
│
├── Configuration/
│   └── ToonConfigurationProviderTests.cs        # TOON configuration provider
│
├── Coverage/
│   ├── EncoderCoverageTests.cs
│   ├── ParserCoverageTests.cs
│   └── SerializerCoverageTests.cs
│
├── Encoding/
│   ├── ToonEncoderTests.cs                      # Encoder unit tests
│   └── ToonEncoderEdgeCaseTests.cs              # Edge cases, options
│
├── Interop/
│   ├── ToonJsonConverterTests.cs                # JSON ↔ TOON conversion
│   └── ToonYamlConverterTests.cs                # YAML ↔ TOON conversion
│
├── Models/
│   ├── ToonDocumentTests.cs
│   ├── ToonValueTests.cs
│   └── ToonValueImplicitConversionTests.cs
│
├── Parsing/
│   ├── ToonParserTests.cs                       # Parser unit tests
│   ├── ToonParserEdgeCaseTests.cs               # Edge cases, errors
│   └── ToonParserDepthLimitTests.cs             # MaxDepth and stack-depth limits
│
├── Serialization/
│   ├── ToonSerializerBasicTests.cs
│   ├── ToonSerializerTypeSupportTests.cs        # CLR type mapping
│   ├── ToonSerializerOutputFormatTests.cs       # Exact TOON output
│   ├── ToonSerializerValueApiTests.cs           # SerializeToValue / DeserializeFromValue
│   ├── ToonSerializerDebugTests.cs
│   └── ToonConverterTests.cs                    # Custom converters
│
├── SpecCompliance/
│   ├── SpecFixtureConformanceTests.cs           # Official spec v3.3.2 fixtures
│   ├── ToonSpecComplianceTests.cs               # Hand-written spec tests
│   ├── KnownNonConformance.txt                  # Fixture cases that do not pass yet
│   └── Fixtures/v3.3.2/{encode,decode}/         # Fixture JSON files
│
├── Validation/
│   ├── InputValidationTests.cs
│   ├── ToonOptionsValidationTests.cs
│   ├── ToonSerializerOptionsValidationTests.cs
│   └── ToonValidatorTests.cs
│
└── ErrorMessageTests.cs                         # Error message validation
```

---

## 📊 Test Coverage

The suite runs on pushes and pull requests to `master` in [CI](../../.github/workflows/ci.yml). Test counts change often, so
they are not listed here; `dotnet test ToonNet.slnx` prints the current totals. Code coverage is not measured in CI;
use the coverage command above to measure it locally.

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

Tests TOON format parsing. `ToonParser` is internal; the test project can use it through `InternalsVisibleTo`
(application code uses `ToonDocument.Parse`):

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
public void Parse_ArrayLengthMismatch_ThrowsException()
{
    var invalid = "tags[3]: a,b";  // Header declares 3 values, 2 given (strict mode)
    
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
    Assert.Contains("Hobbies[2]: Reading,Gaming", toon);
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

Hand-written tests for TOON spec v3.3.2 rules (in addition to the fixtures above):

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
public void Parse_EmptyInput_ReturnsEmptyObject()
{
    var parser = new ToonParser();
    var doc = parser.Parse("");  // An empty document is an empty object (spec)

    Assert.Empty(((ToonObject)doc.Root).Properties);
}

[Fact]
public void Serialize_CircularReference_ThrowsException()
{
    var node1 = new Node { Name = "Node1" };
    var node2 = new Node { Name = "Node2", Parent = node1 };
    node1.Child = node2;  // Circular!
    
    // The message includes the property path of the cycle
    Assert.Throws<ToonEncodingException>(
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

[`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) builds the solution, runs all test projects, packs the
NuGet packages (validation only) and builds the docs site on pushes and pull requests to `master`. The manual
[`publish.yml`](../../.github/workflows/publish.yml) workflow runs the tests again before publishing to NuGet.

---

## 🔍 Test Data

### Sample Test Models

Most tests declare small models next to the tests that use them (for example the nested classes in
`Coverage/SerializerCoverageTests.cs`). Typical shapes:

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
- [`ToonNet.Benchmarks`](../../benchmark/ToonNet.Benchmarks) - Performance tests

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete ToonNet guide
- [API Guide](../../docs/API-GUIDE.md) - API reference
- [Contributing Guide](../../CONTRIBUTING.md) - Adding tests

---

## 📋 Requirements

- .NET 8.0 or later
- xUnit 2.5.3
- ToonNet.Core
- ToonNet.Extensions.Json
- ToonNet.Extensions.Yaml
- ToonNet.AspNetCore, ToonNet.AspNetCore.Mvc (uses the `Microsoft.AspNetCore.App` framework reference)

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
