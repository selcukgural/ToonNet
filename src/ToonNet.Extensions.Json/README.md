# ToonNet.Extensions.Json

**JSON ↔ TOON format conversion extension**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Package](https://img.shields.io/badge/package-ToonNet.Extensions.Json-blue)](#)
[![Status](https://img.shields.io/badge/status-stable-success)](#)

---

## 📦 What is ToonNet.Extensions.Json?

ToonNet.Extensions.Json provides **seamless bidirectional conversion** between JSON and TOON formats:

- ✅ **JSON → TOON** - Convert JSON strings/documents to TOON format
- ✅ **TOON → JSON** - Convert TOON strings/documents to JSON format
- ✅ **System.Text.Json integration** - Familiar API patterns
- ✅ **Preserves structure** - Round-trip conversions maintain data integrity
- ✅ **Developer-friendly** - Extension methods on ToonSerializer

**Perfect for:**
- 🤖 **AI/LLM Applications** - Convert JSON APIs to token-efficient TOON
- 🔄 **Data Migration** - Transform existing JSON data to TOON format
- 🔗 **Interoperability** - Work with JSON-based systems
- 📊 **API Integration** - Accept JSON, process as TOON, return JSON

> **Note:** This package was moved from Core in v2.0 for cleaner architecture. See [migration guide](../../README.md#-breaking-changes-v20).

---

## 🚀 Quick Start

### Installation

```bash
# Core package (required)
dotnet add package ToonNet.Core

# JSON extension
dotnet add package ToonNet.Extensions.Json
```

### Basic Usage - String Conversion

```csharp
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;

// JSON → TOON string conversion
string jsonString = """
{
  "name": "Alice",
  "age": 30,
  "hobbies": ["reading", "coding"]
}
""";

string toonString = ToonSerializerExtensions.FromJson(jsonString);

// Output (TOON format):
// name: Alice
// age: 30
// hobbies[2]: reading, coding

// TOON → JSON string conversion
string jsonBack = ToonSerializerExtensions.ToJson(toonString);
```

### Object Serialization via JSON

```csharp
using ToonNet.Extensions.Json;

public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Hobbies { get; set; }
}

var person = new Person 
{ 
    Name = "Bob", 
    Age = 25, 
    Hobbies = new List<string> { "gaming", "music" }
};

// Serialize C# object to JSON
string json = ToonSerializerExtensions.SerializeToJson(person);

// Deserialize JSON to C# object
var personBack = ToonSerializerExtensions.DeserializeFromJson<Person>(json);

// One-step: JSON string → C# object via TOON
var person2 = ToonSerializerExtensions.ParseJson<Person>(jsonString);
```

---

## 📖 API Reference

### String Format Conversion

```csharp
// JSON string → TOON string
string toon = ToonSerializerExtensions.FromJson(jsonString);
string toon = ToonSerializerExtensions.FromJson(jsonString, toonOptions);

// TOON string → JSON string
string json = ToonSerializerExtensions.ToJson(toonString);
string json = ToonSerializerExtensions.ToJson(toonString, jsonOptions);
```

### Document Conversion (Low-level)

```csharp
using ToonNet.Extensions.Json;

// JSON string → ToonDocument
ToonDocument doc = ToonJsonConverter.FromJson(jsonString);

// JsonElement → ToonDocument
ToonDocument doc = ToonJsonConverter.FromJson(jsonElement);

// ToonDocument → JSON string
string json = ToonJsonConverter.ToJson(document);
string json = ToonJsonConverter.ToJson(document, writeIndented: true);

// ToonValue → JSON string
string json = ToonJsonConverter.ToJson(toonValue);
string json = ToonJsonConverter.ToJson(toonValue, writeIndented: false);
```

### Object Serialization

```csharp
// C# object → JSON string
string json = ToonSerializerExtensions.SerializeToJson<T>(obj);
string json = ToonSerializerExtensions.SerializeToJson<T>(obj, jsonOptions);

// JSON string → C# object
T obj = ToonSerializerExtensions.DeserializeFromJson<T>(jsonString);
T obj = ToonSerializerExtensions.DeserializeFromJson<T>(jsonString, options);

// JSON string → TOON → C# object (one step)
T obj = ToonSerializerExtensions.ParseJson<T>(jsonString);
T obj = ToonSerializerExtensions.ParseJson<T>(jsonString, options);
```

---

## 🎯 Real-World Examples

### Example 1: AI/LLM Token Optimization

```csharp
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;

// Receive JSON from API
string apiResponse = await httpClient.GetStringAsync("/api/products");

// Convert to TOON (fewer tokens for LLM)
string toonData = ToonSerializerExtensions.FromJson(apiResponse);

// Use in LLM prompt
string prompt = $"""
You are a product analyst. Here is the product catalog:

{toonData}

Recommend the best products for a software developer.
""";

// TOON is ~40% fewer tokens than JSON!
```

### Example 2: Data Migration

```csharp
// Load existing JSON configuration
string jsonConfig = File.ReadAllText("appsettings.json");

// Convert to TOON format
string toonConfig = ToonSerializerExtensions.FromJson(jsonConfig);

// Save as TOON (more human-readable)
File.WriteAllText("appsettings.toon", toonConfig);

// Later: Load TOON and convert back if needed
string toonContent = File.ReadAllText("appsettings.toon");
var config = ToonSerializer.Deserialize<AppSettings>(toonContent);
```

### Example 3: Roundtrip Verification

```csharp
// Original JSON
string originalJson = """{"discount": 35.00, "active": true}""";

// JSON → TOON → JSON
string toonString = ToonSerializerExtensions.FromJson(originalJson);
string roundtripJson = ToonSerializerExtensions.ToJson(toonString);

// Normalize both for comparison
var original = JsonSerializer.Deserialize<object>(originalJson);
var roundtrip = JsonSerializer.Deserialize<object>(roundtripJson);

// Semantic equivalence preserved (format may differ)
// 35.00 → 35 is semantically equal (JSON spec compliant)
```

---

## 🔄 Format Conversion Behavior

### Type Mapping

| JSON Type | TOON Type | Notes |
|-----------|-----------|-------|
| `object` | `ToonObject` | Key-value pairs |
| `array` | `ToonArray` | Ordered items |
| `string` | `ToonString` | UTF-8 text |
| `number` | `ToonNumber` | Float64 precision |
| `true/false` | `ToonBoolean` | Boolean values |
| `null` | `ToonNull` | Null/undefined |

### Semantic Equivalence

**Important:** Format conversions preserve **semantic equivalence**, not exact formatting:

```csharp
// JSON: {"price": 35.00}
// TOON: price: 35.00
// JSON (roundtrip): {"price": 35}  ← Format differs, value identical
```

This is standard behavior across all serialization libraries (System.Text.Json, Newtonsoft.Json). See [Roundtrip Guarantees](../../docs/API-GUIDE.md) for details.

---

## 🔗 Related Packages

**Core:**
- [`ToonNet.Core`](../ToonNet.Core) - Core serialization (required)

**Other Extensions:**
- [`ToonNet.Extensions.Yaml`](../ToonNet.Extensions.Yaml) - YAML ↔ TOON conversion

**Web Integration:**
- [`ToonNet.AspNetCore`](../ToonNet.AspNetCore) - ASP.NET Core middleware
- [`ToonNet.AspNetCore.Mvc`](../ToonNet.AspNetCore.Mvc) - MVC formatters

**Development:**
- [`ToonNet.Demo`](../../demo/ToonNet.Demo) - Sample applications with JSON examples
- [`ToonNet.Tests`](../../tests/ToonNet.Tests) - JSON conversion test suite

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete ToonNet guide
- [API Guide](../../docs/API-GUIDE.md) - Detailed API reference
- [Migration Guide](../../README.md#-breaking-changes-v20) - v1.x → v2.0 migration
- [Samples](../../demo/ToonNet.Demo/Samples) - Real-world JSON examples

---

## 🧪 Testing

```bash
# Run JSON conversion tests
cd tests/ToonNet.Tests
dotnet test --filter "FullyQualifiedName~ToonJsonConverter"

# Run specific test categories
dotnet test --filter "Category=JsonConversion"
```

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

## 🤝 Contributing

Contributions welcome! Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

---

**Part of the [ToonNet](../../README.md) serialization library family.**

