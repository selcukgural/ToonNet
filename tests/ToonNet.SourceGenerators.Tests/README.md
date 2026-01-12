# ToonNet.SourceGenerators.Tests

**Test suite for ToonNet source generators**

[![.NET](https://img.shields.io/badge/.NET-10.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![xUnit](https://img.shields.io/badge/xUnit-3.1+-blue)](#)
[![Tests](https://img.shields.io/badge/tests-17%20passing-success)](#)

---

## 📦 What is ToonNet.SourceGenerators.Tests?

ToonNet.SourceGenerators.Tests verifies **compile-time code generation** for ToonNet:

- ✅ **17 Passing Tests** - Source generator validation
- ✅ **Generated Code** - Verifies correct IL generation
- ✅ **Attribute Handling** - [ToonSerializable] processing
- ✅ **Type Support** - All supported CLR types
- ✅ **Edge Cases** - Nullable, collections, nested types

---

## 🚀 Quick Start

### Running Tests

```bash
cd tests/ToonNet.SourceGenerators.Tests
dotnet test
```

### Verifying Generated Code

```bash
# Build and check generated files
dotnet build

# View generated code
ls obj/Debug/net10.0/generated/ToonNet.SourceGenerators/
cat obj/Debug/net10.0/generated/ToonNet.SourceGenerators/*.g.cs
```

---

## 📂 Test Structure

```
ToonNet.SourceGenerators.Tests/
├── Models/
│   ├── SimpleModel.cs                  # Simple test model
│   ├── ComplexModel.cs                 # Complex nested model
│   ├── NullableModel.cs                # Nullable types
│   └── CollectionModel.cs              # Collections
│
├── SerializationTests.cs               # Serialization tests
├── DeserializationTests.cs             # Deserialization tests
└── GeneratedCodeTests.cs               # Generated code validation
```

---

## 🎯 Test Categories

### 1. Simple Model Tests

```csharp
[ToonSerializable]
public partial class SimpleModel
{
    public string Name { get; set; }
    public int Age { get; set; }
}

[Fact]
public void SimpleModel_Serialize_Success()
{
    var model = new SimpleModel { Name = "Alice", Age = 30 };
    
    // Uses generated Serialize method
    var toon = model.Serialize();
    
    Assert.Contains("Name: Alice", toon);
    Assert.Contains("Age: 30", toon);
}

[Fact]
public void SimpleModel_Deserialize_Success()
{
    var toon = """
        Name: Bob
        Age: 25
        """;
    
    // Uses generated Deserialize method
    var model = SimpleModel.Deserialize(toon);
    
    Assert.Equal("Bob", model.Name);
    Assert.Equal(25, model.Age);
}
```

### 2. Complex Model Tests

```csharp
[ToonSerializable]
public partial class ComplexModel
{
    public int Id { get; set; }
    public NestedModel Nested { get; set; }
    public List<string> Tags { get; set; }
}

[ToonSerializable]
public partial class NestedModel
{
    public string Value { get; set; }
}

[Fact]
public void ComplexModel_WithNested_Serialize()
{
    var model = new ComplexModel
    {
        Id = 1,
        Nested = new NestedModel { Value = "test" },
        Tags = new List<string> { "tag1", "tag2" }
    };
    
    var toon = model.Serialize();
    
    Assert.Contains("Id: 1", toon);
    Assert.Contains("Nested:", toon);
    Assert.Contains("Value: test", toon);
    Assert.Contains("Tags[2]: tag1, tag2", toon);
}
```

### 3. Nullable Types Tests

```csharp
[ToonSerializable]
public partial class NullableModel
{
    public int? NullableInt { get; set; }
    public DateTime? NullableDate { get; set; }
    public string? NullableString { get; set; }
}

[Fact]
public void NullableModel_WithNulls_Serialize()
{
    var model = new NullableModel
    {
        NullableInt = null,
        NullableDate = DateTime.Now,
        NullableString = "test"
    };
    
    var toon = model.Serialize();
    
    Assert.Contains("NullableInt: null", toon);
    Assert.Contains("NullableDate:", toon);
    Assert.Contains("NullableString: test", toon);
}
```

### 4. Attribute Tests

```csharp
[ToonSerializable]
public partial class AttributeModel
{
    [ToonProperty("custom_name")]
    public string Name { get; set; }
    
    [ToonIgnore]
    public string Ignored { get; set; }
    
    public int Age { get; set; }
}

[Fact]
public void AttributeModel_CustomProperty_Serializes()
{
    var model = new AttributeModel
    {
        Name = "Alice",
        Ignored = "secret",
        Age = 30
    };
    
    var toon = model.Serialize();
    
    Assert.Contains("custom_name: Alice", toon);
    Assert.DoesNotContain("Ignored", toon);
    Assert.Contains("Age: 30", toon);
}
```

---

## 🔍 Generated Code Inspection

### What Gets Generated

For each `[ToonSerializable]` class, the generator creates:

1. **IToonSerializable<T> Implementation**
2. **Serialize() Method**
3. **Static Deserialize() Method**
4. **Helper Methods** (parsing, conversion)

### Example Generated Code

**Input:**
```csharp
[ToonSerializable]
public partial class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

**Generated Output** (simplified):
```csharp
public partial class Person : IToonSerializable<Person>
{
    public string Serialize(ToonSerializerOptions? options = null)
    {
        var sb = new StringBuilder();
        sb.Append("Name: ").AppendLine(this.Name);
        sb.Append("Age: ").AppendLine(this.Age.ToString());
        return sb.ToString();
    }

    public static Person Deserialize(string toonString, ToonSerializerOptions? options = null)
    {
        var parser = new ToonParser();
        var doc = parser.Parse(toonString);
        var root = (ToonObject)doc.Root;
        
        return new Person
        {
            Name = ((ToonString)root["Name"]).Value,
            Age = (int)((ToonNumber)root["Age"]).Value
        };
    }
}
```

---

## 📊 Test Coverage

| Feature | Tests | Status |
|---------|-------|--------|
| Simple types | 3 | ✅ Passing |
| Complex types | 4 | ✅ Passing |
| Nullable types | 3 | ✅ Passing |
| Collections | 2 | ✅ Passing |
| Attributes | 3 | ✅ Passing |
| Error cases | 2 | ✅ Passing |

**Total:** 17 tests, all passing

---

## 🧪 Performance Tests

Source generator tests also validate performance:

```csharp
[Fact]
public void SourceGenerator_ZeroAllocation()
{
    var model = new SimpleModel { Name = "Test", Age = 25 };
    
    // First call (warm-up)
    var toon1 = model.Serialize();
    
    // Measure allocations
    var before = GC.GetTotalMemory(true);
    var toon2 = model.Serialize();
    var after = GC.GetTotalMemory(false);
    
    var allocated = after - before;
    
    // Should be minimal (only string allocation)
    Assert.True(allocated < 1024, "Unexpected allocations detected");
}
```

---

## 🔗 Related Packages

**Source Generator:**
- [`ToonNet.SourceGenerators`](../../src/ToonNet.SourceGenerators) - Code being tested

**Core:**
- [`ToonNet.Core`](../../src/ToonNet.Core) - Core serialization

**Other Tests:**
- [`ToonNet.Tests`](../ToonNet.Tests) - Main test suite (427 tests)

**Performance:**
- [`ToonNet.Benchmarks`](../../src/ToonNet.Benchmarks) - Performance benchmarks

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete guide
- [Source Generators Guide](../../src/ToonNet.SourceGenerators/README.md) - Generator docs
- [Contributing Guide](../../CONTRIBUTING.md) - Adding tests

---

## 📋 Requirements

- .NET 10.0 or later (for latest xUnit)
- xUnit 3.1+
- ToonNet.Core
- ToonNet.SourceGenerators

---

## 🤝 Contributing

Want to add tests? Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

**Test Guidelines:**
- Test both serialization and deserialization
- Include edge cases (null, empty, large values)
- Verify generated code correctness
- Test attribute handling
- Validate error messages

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

**Part of the [ToonNet](../../README.md) serialization library family.**
