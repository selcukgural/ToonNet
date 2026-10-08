# ToonNet.Extensions.Json

**JSON ↔ TOON format conversion extension**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/ToonNet.Extensions.Json.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Extensions.Json/)
[![Downloads](https://img.shields.io/nuget/dt/ToonNet.Extensions.Json.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Extensions.Json/)
[![Status](https://img.shields.io/badge/status-stable-success)](#)

---

## 📦 What is ToonNet.Extensions.Json?

ToonNet.Extensions.Json provides **seamless bidirectional conversion** between JSON and TOON formats:

- ✅ **JSON → TOON** - Convert JSON strings/documents to TOON format
- ✅ **TOON → JSON** - Convert TOON strings/documents to JSON format
- ✅ **System.Text.Json integration** - Familiar API patterns
- ✅ **Preserves structure** - Round-trip conversions keep objects, arrays and values (numbers keep their exact value)
- ✅ **Developer-friendly** - Static `ToonConvert` (string-based) and `ToonJsonConverter` (document-based) helpers

**Perfect for:**
- 🤖 **AI/LLM Applications** - Convert JSON APIs to token-efficient TOON
- 🔄 **Data Migration** - Transform existing JSON data to TOON format
- 🔗 **Interoperability** - Work with JSON-based systems
- 📊 **API Integration** - Accept JSON, process as TOON, return JSON

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

string toonString = ToonConvert.FromJson(jsonString);

// Output (TOON format):
// name: Alice
// age: 30
// hobbies[2]: reading,coding

// TOON → JSON string conversion
string jsonBack = ToonConvert.ToJson(toonString);
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
string json = ToonConvert.SerializeToJson(person);

// Deserialize JSON to C# object (plain System.Text.Json, case-sensitive by default)
var personBack = ToonConvert.DeserializeFromJson<Person>(json);

// One-step: JSON string → C# object via TOON.
// TOON keys must match the property names; the camelCase policy maps "name" to Name.
var person2 = ToonConvert.ParseJson<Person>(jsonString,
    new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase });
```

---

## 📖 API Reference

### String Format Conversion

```csharp
// JSON string → TOON string
string toon = ToonConvert.FromJson(jsonString);
string toon = ToonConvert.FromJson(jsonString, options);  // ToonOptions

// TOON string → JSON string
string json = ToonConvert.ToJson(toonString);
string json = ToonConvert.ToJson(toonString, writerOptions);  // JsonWriterOptions
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
string json = ToonJsonConverter.ToJson(document, writerOptions);  // JsonWriterOptions

// ToonValue → JSON string
string json = ToonJsonConverter.ToJson(toonValue);
string json = ToonJsonConverter.ToJson(toonValue, writerOptions);  // JsonWriterOptions
```

### Object Serialization

`SerializeToJson` and `DeserializeFromJson` are thin wrappers over `System.Text.Json.JsonSerializer`
(TOON is not involved; `SerializeToJson` indents when no options are passed).

```csharp
// C# object → JSON string
string json = ToonConvert.SerializeToJson<T>(obj);
string json = ToonConvert.SerializeToJson<T>(obj, options);  // JsonSerializerOptions

// JSON string → C# object
T obj = ToonConvert.DeserializeFromJson<T>(jsonString);
T obj = ToonConvert.DeserializeFromJson<T>(jsonString, options);  // JsonSerializerOptions

// JSON string → TOON → C# object (one step)
T obj = ToonConvert.ParseJson<T>(jsonString);
T obj = ToonConvert.ParseJson<T>(jsonString, options);  // ToonSerializerOptions
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
string toonData = ToonConvert.FromJson(apiResponse);

// Use in LLM prompt
string prompt = $"""
You are a product analyst. Here is the product catalog:

{toonData}

Recommend the best products for a software developer.
""";

// Uniform arrays of objects become TOON tables, which usually take fewer tokens than JSON
```

### Example 2: Data Migration

```csharp
// Load existing JSON configuration
string jsonConfig = File.ReadAllText("appsettings.json");

// Convert to TOON format
string toonConfig = ToonConvert.FromJson(jsonConfig);

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
string toonString = ToonConvert.FromJson(originalJson);
string roundtripJson = ToonConvert.ToJson(toonString);

// Normalize both for comparison
var original = JsonSerializer.Deserialize<object>(originalJson);
var roundtrip = JsonSerializer.Deserialize<object>(roundtripJson);

// Semantic equivalence preserved (format may differ)
// roundtripJson is {"discount":35,"active":true}
```

---

## 🔄 Format Conversion Behavior

### Type Mapping

| JSON Type | TOON Type | Notes |
|-----------|-----------|-------|
| `object` | `ToonObject` | Key-value pairs |
| `array` | `ToonArray` | Ordered items |
| `string` | `ToonString` | UTF-8 text |
| `number` | `ToonNumber` | Exact `decimal` when it fits, otherwise `double` |
| `true/false` | `ToonBoolean` | Boolean values |
| `null` | `ToonNull` | Null/undefined |

### Semantic Equivalence

**Important:** Format conversions preserve **semantic equivalence**, not exact formatting:

```csharp
// JSON: {"price": 35.00}
// TOON: price: 35
// JSON (roundtrip): {"price":35}  ← Format differs, value identical
```

JSON numbers are converted with the TOON number rules: the exact value is kept when it fits in a `decimal`
(so `12345678901234567890` and `1299.99` round-trip unchanged), smaller or larger magnitudes use the nearest
`double`, and a number that is not finite as a `double` (e.g. `1e400`) is kept as a string. As TOON spec §2
requires, numbers are written in canonical form, so `35.00` becomes `35` and `1.0` becomes `1` (same value). `ToonJsonConverter.FromJson(string)` uses
`JsonDocument.Parse` with its default maximum depth of 64.

See [Roundtrip Guarantees](../../docs/API-GUIDE.md) for details.

---

## 🔒 Thread-Safety

- `ToonSerializer` and JSON conversion methods are safe to call concurrently across threads.
- Shared metadata/name caches use `ConcurrentDictionary` for concurrent access.
- Cache entries are created on demand and retained for the process lifetime (no eviction).
- Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.

---

## 🔗 Related Packages

**Core:**
- [`ToonNet.Core`](../ToonNet.Core) - Core serialization (required)

**Other Extensions:**
- [`ToonNet.Extensions.Yaml`](../ToonNet.Extensions.Yaml) - YAML ↔ TOON conversion

**Web Integration:**
- [`ToonNet.AspNetCore`](../ToonNet.AspNetCore) - TOON configuration provider and DI integration
- [`ToonNet.AspNetCore.Mvc`](../ToonNet.AspNetCore.Mvc) - MVC formatters

**Development:**
- [`ToonNet.Demo`](../../demo/ToonNet.Demo) - Sample applications with JSON examples
- [`ToonNet.Tests`](../../tests/ToonNet.Tests) - JSON conversion test suite

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete ToonNet guide
- [API Guide](../../docs/API-GUIDE.md) - Detailed API reference
- [Samples](../../demo/ToonNet.Demo/Samples) - Real-world JSON examples

---

## 🧪 Testing

```bash
# Run JSON conversion tests
cd tests/ToonNet.Tests
dotnet test --filter "FullyQualifiedName~ToonJsonConverter"
```

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

## 🤝 Contributing

Contributions welcome! Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

---

**Part of the [ToonNet](../../README.md) serialization library family.**
