# ToonNet.Extensions.Json

JSON interoperability extension for ToonNet. Provides seamless bidirectional conversion between JSON and TOON formats.

## 📦 Installation

```bash
dotnet add package ToonNet.Extensions.Json
```

## 🚀 Quick Start

### JSON → TOON

```csharp
using ToonNet.Extensions.Json;

var json = """
{
  "name": "Alice",
  "age": 30,
  "tags": ["dev", "admin"]
}
""";

// Convert JSON to TOON
var toonDoc = ToonJsonConverter.FromJson(json);

// Access data
var root = (ToonObject)toonDoc.Root;
var name = ((ToonString)root["name"]).Value; // "Alice"
var age = ((ToonNumber)root["age"]).Value;   // 30
```

### TOON → JSON

```csharp
using ToonNet.Core.Models;
using ToonNet.Extensions.Json;

// Create TOON document
var toonObj = new ToonObject
{
    ["name"] = new ToonString("Bob"),
    ["age"] = new ToonNumber(25),
    ["active"] = new ToonBoolean(true)
};
var doc = new ToonDocument(toonObj);

// Convert to JSON
var json = ToonJsonConverter.ToJson(doc, writeIndented: true);
```

## 🔄 Round-trip Conversion

```csharp
// JSON → TOON → JSON (preserves structure)
var originalJson = """{"user":{"name":"Charlie","id":123}}""";
var toonDoc = ToonJsonConverter.FromJson(originalJson);
var newJson = ToonJsonConverter.ToJson(toonDoc);
```

## 🎯 Use Cases

- **Data Migration**: Convert existing JSON APIs to TOON format
- **Interoperability**: Work with systems that use JSON
- **Testing**: Compare JSON and TOON representations
- **Configuration**: Load JSON configs as TOON documents

## 📋 Requirements

- .NET 8.0 or later
- ToonNet.Core
- System.Text.Json (built-in)

## 🔗 Related Packages

- **ToonNet.Core** - Core TOON parsing and encoding
- **ToonNet.Extensions.Yaml** - YAML interoperability

## 📄 License

MIT
