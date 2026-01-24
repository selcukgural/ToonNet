<div align="center">

<img src="icon.png" alt="ToonNet Logo" width="128" height="128">

**TOON Data Format Serialization for .NET**

*AI-Optimized • Token-Efficient • Developer-Friendly*

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/ToonNet.Core.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Core/)
[![Downloads](https://img.shields.io/nuget/dt/ToonNet.Core.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Core/)
[![Tests](https://img.shields.io/badge/tests-444%20passing-success?style=flat)](#)
[![Spec](https://img.shields.io/badge/TOON%20v3.0-100%25-blue?style=flat)](ToonSpec.md)
[![Documentation](https://img.shields.io/badge/docs-online-brightgreen?style=flat&logo=docusaurus)](https://selcukgural.github.io/ToonNet/)

[Quick Start](#-quick-start) • [Documentation](https://selcukgural.github.io/ToonNet/) • [API Reference](https://selcukgural.github.io/ToonNet/docs/api/intro) • [Samples](demo/ToonNet.Demo/Samples)

</div>

---

## What is ToonNet?

ToonNet is a **.NET serialization library** that provides:

- **Serialize** C# objects to TOON format
- **Deserialize** TOON format to C# objects  
- **Convert** between JSON, TOON, and YAML formats
- **System.Text.Json-compatible API** for zero learning curve

**TOON Format** is a human-readable data format optimized for:
- **AI/LLM prompts** - Up to 40% fewer tokens than JSON
- **Configuration files** - Clean, readable syntax
- **Data exchange** - Human and machine friendly

> **TOON Specification:** This library implements [TOON v3.0](https://github.com/toon-format/spec/blob/main/SPEC.md) (Date: 2025-11-24, Status: Working Draft)

---

## 🤖 Why Developers Choose ToonNet

ToonNet delivers three critical advantages:

1. **🎯 40% Token Reduction** - Fewer tokens = Lower AI API costs
2. **⚡ High Performance** - Expression trees, not reflection (10-100x faster)
3. **🔧 Zero Learning Curve** - System.Text.Json-compatible API

### 🤖 AI Token Optimization

TOON format uses **significantly fewer tokens** than JSON, reducing AI API costs:

```csharp
// Example: Product catalog for AI prompt
var products = new List<Product>
{
    new() { Id = 1, Name = "Laptop", Price = 1299.99m, InStock = true },
    new() { Id = 2, Name = "Mouse", Price = 29.99m, InStock = true },
    new() { Id = 3, Name = "Keyboard", Price = 89.99m, InStock = false }
};

string json = ToonConvert.SerializeToJson(products);
string toon = ToonSerializer.Serialize(products);

Console.WriteLine($"JSON tokens: ~{json.Length / 4}");  // ~150 tokens
Console.WriteLine($"TOON tokens: ~{toon.Length / 4}");  // ~90 tokens
// 40% token reduction = 40% cost savings on AI APIs
```

**JSON output (longer, more tokens):**
```json
[{"id":1,"name":"Laptop","price":1299.99,"inStock":true},{"id":2,"name":"Mouse","price":29.99,"inStock":true},{"id":3,"name":"Keyboard","price":89.99,"inStock":false}]
```

**TOON output (shorter, fewer tokens):**
```toon
products[3]:
  - Id: 1
    Name: Laptop
    Price: 1299.99
    InStock: true
  - Id: 2
    Name: Mouse
    Price: 29.99
    InStock: true
  - Id: 3
    Name: Keyboard
    Price: 89.99
    InStock: false
```

**Real-world savings:**
- GPT-4: ~$0.03 per 1K input tokens → 40% fewer tokens = **40% cost reduction**
- Claude: ~$0.015 per 1K input tokens → Significant savings on large prompts
- Perfect for RAG systems, prompt engineering, AI-powered tools

### ⚡ Performance & Architecture

ToonNet is designed for **high-performance** production environments:

**Zero-Reflection Serialization:**
- **Expression Trees** - Compiled property accessors (10-100x faster than reflection)
- **Source Generators** - Compile-time code generation for zero-allocation serialization
- **Metadata Caching** - Thread-safe `ConcurrentDictionary` for type metadata
- **No runtime reflection** overhead after first access

**Optimized for .NET 8+:**
```csharp
// First serialization: Compiles expression trees and caches metadata
var toon1 = ToonSerializer.Serialize(myObject);  // ~1-2ms (cold start)

// Subsequent serializations: Uses cached compiled accessors
var toon2 = ToonSerializer.Serialize(myObject);  // ~0.05ms (hot path)
// 20-40x faster than reflection-based serializers
```

**Architecture highlights:**
- **Compiled getters/setters** - Expression trees compiled to IL, not reflection calls
- **Thread-safe caching** - Concurrent metadata cache for multi-threaded scenarios
- **Span<T> and Memory<T>** - Modern .NET APIs for reduced allocations
- **Source generator option** - AOT-compatible, zero-allocation code generation

> **When to use Source Generators:** For maximum performance in hot paths (APIs, real-time systems), use `[ToonSerializable]` attribute with `ToonNet.SourceGenerators` package for compile-time code generation.

---

## 📦 Packages

ToonNet is modular - install only what you need:

| Package | Description | Status |
|---------|-------------|--------|
| **ToonNet.Core** | Core serialization API - C# ↔ TOON (uses expression trees) | ✅ Stable |
| **ToonNet.Extensions.Json** | JSON ↔ TOON conversion | ✅ Stable |
| **ToonNet.Extensions.Yaml** | YAML ↔ TOON conversion | ✅ Stable |
| **ToonNet.AspNetCore** | ASP.NET Core middleware & formatters | ✅ Stable |
| **ToonNet.AspNetCore.Mvc** | MVC input/output formatters | ✅ Stable |
| **ToonNet.SourceGenerators** | Compile-time code generation (AOT-compatible, zero-allocation) | ✅ Stable |

### Quick Install

```bash
# Core package (required)
dotnet add package ToonNet.Core

# JSON support (for AI/LLM token optimization)
dotnet add package ToonNet.Extensions.Json

# YAML support
dotnet add package ToonNet.Extensions.Yaml

# ASP.NET Core integration
dotnet add package ToonNet.AspNetCore
dotnet add package ToonNet.AspNetCore.Mvc

# Performance (source generators)
dotnet add package ToonNet.SourceGenerators
```

---

## 🚀 Quick Start

### Installation

```bash
# Core package (required)
dotnet add package ToonNet.Core

# For JSON conversion (AI/LLM use cases)
dotnet add package ToonNet.Extensions.Json
```

### Basic Usage - AI Prompt Context

```csharp
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;  // For JSON conversion

// Your C# class for AI prompt context (no attributes needed)
public class UserContext
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Interests { get; set; }
    public List<Purchase> RecentPurchases { get; set; }
}

public class Purchase
{
    public string Product { get; set; }
    public decimal Amount { get; set; }
}

var context = new UserContext 
{ 
    Name = "Alice",
    Age = 28,
    Interests = new List<string> { "AI", "Machine Learning", "Photography" },
    RecentPurchases = new List<Purchase>
    {
        new() { Product = "Camera Lens", Amount = 450.00m },
        new() { Product = "ML Course", Amount = 99.99m }
    }
};

// Serialize to TOON for AI prompt (uses fewer tokens than JSON)
string toonContext = ToonSerializer.Serialize(context);

// Use in your LLM prompt
var prompt = $@"
User Profile:
{toonContext}

Generate personalized product recommendations.
";

// Or deserialize back
var restored = ToonSerializer.Deserialize<UserContext>(toonContext);
```

**Output (TOON format - compact, AI-friendly):**
```toon
Name: Alice
Age: 28
Interests[3]: AI, Machine Learning, Photography
RecentPurchases[2]:
  - Product: Camera Lens
    Amount: 450.00
  - Product: ML Course
    Amount: 99.99
```

**Token savings:** ~40% fewer tokens than JSON = lower AI API costs!

That's it - no configuration, no attributes, just works.

---

## 📚 API Reference

ToonNet provides **6 core methods** with familiar System.Text.Json-style naming:

### C# Object Serialization

```csharp
// Serialize object to TOON string
string toon = ToonSerializer.Serialize(myObject);

// Deserialize TOON string to object
var obj = ToonSerializer.Deserialize<MyClass>(toonString);
```

### Format Conversion (String-based)

> **Note:** JSON conversion methods are in `ToonNet.Extensions.Json` package. Add `using ToonNet.Extensions.Json;`

```csharp
// Convert JSON string to TOON string
string toon = ToonConvert.FromJson(jsonString);

// Convert TOON string to JSON string
string json = ToonConvert.ToJson(toonString);

// Parse JSON directly to C# object (via TOON)
var obj = ToonConvert.DeserializeFromJson<MyClass>(jsonString);

// Serialize C# object directly to JSON
string json = ToonConvert.SerializeToJson(myObject);
```

**Architecture Note:** ToonNet uses a layered approach for JSON interop:

- **`ToonJsonConverter`** - Low-level conversion between `JsonElement` ↔ `ToonDocument`/`ToonValue`. Used internally as the core conversion engine.
- **`ToonConvert`** - High-level, developer-friendly API (similar to Newtonsoft's `JsonConvert`). Provides simple string-based conversions and internally uses `ToonJsonConverter`.

This separation of concerns ensures clean architecture: `ToonJsonConverter` handles the conversion logic, while `ToonConvert` provides an ergonomic interface familiar to .NET developers.

### YAML Conversion (Extension Package)

```csharp
using ToonNet.Extensions.Yaml;

// YAML string → TOON string
string toon = ToonYamlConvert.FromYaml(yamlString);

// TOON string → YAML string
string yaml = ToonYamlConvert.ToYaml(toonString);
```

**Note:** YAML support requires `ToonNet.Extensions.Yaml` package.

**Architecture Note:** Similar to JSON extensions, YAML package uses a layered approach:
- **`ToonYamlConverter`** - Low-level conversion engine (YAML nodes ↔ ToonDocument)
- **`ToonYamlConvert`** - High-level string-based API (developer-friendly)

**Complete method reference:**

| Method | Package | Input | Output | Use Case |
|--------|---------|-------|--------|----------|
| `Serialize<T>(obj)` | Core | C# Object | TOON string | Save objects as TOON |
| `Deserialize<T>(toon)` | Core | TOON string | C# Object | Load TOON into objects |
| `FromJson(json)` | Extensions.Json | JSON string | TOON string | Convert JSON to TOON |
| `ToJson(toon)` | Extensions.Json | TOON string | JSON string | Convert TOON to JSON |
| `DeserializeFromJson<T>(json)` | Extensions.Json | JSON string | C# Object | Parse JSON via TOON |
| `SerializeToJson<T>(obj)` | Extensions.Json | C# Object | JSON string | Export as JSON |
| `FromYaml(yaml)` | Extensions.Yaml | YAML string | TOON string | Convert YAML to TOON |
| `ToYaml(toon)` | Extensions.Yaml | TOON string | YAML string | Convert TOON to YAML |

📖 **Full API documentation: [API-GUIDE.md](docs/API-GUIDE.md)**

---

## 💡 Examples

### Example 1: AI/LLM Prompt Context (Token Optimization)

```csharp
public class CustomerContext
{
    public string Name { get; set; }
    public List<Order> RecentOrders { get; set; }
    public List<string> Preferences { get; set; }
}

public class Order
{
    public string Id { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; }
}

var context = new CustomerContext
{
    Name = "Alice Johnson",
    RecentOrders = new List<Order>
    {
        new() { Id = "ORD-001", Total = 299.99m, Status = "Delivered" },
        new() { Id = "ORD-002", Total = 149.50m, Status = "Shipped" }
    },
    Preferences = new List<string> { "Electronics", "Fast Shipping", "Eco-Friendly" }
};

// Serialize for AI prompt - uses fewer tokens than JSON
string promptContext = ToonSerializer.Serialize(context);

// Send to AI API with reduced token usage
var aiPrompt = $@"
Customer context:
{promptContext}

Generate personalized product recommendations.
";

// Result: 40% fewer tokens = 40% lower AI API costs
```

**Output (compact, AI-friendly):**
```toon
Name: Alice Johnson
RecentOrders[2]:
  - Id: ORD-001
    Total: 299.99
    Status: Delivered
  - Id: ORD-002
    Total: 149.50
    Status: Shipped
Preferences[3]: Electronics, Fast Shipping, Eco-Friendly
```

---

### Example 2: RAG System (Vector Database Context)

```csharp
public class DocumentChunk
{
    public string Id { get; set; }
    public string Content { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

// Retrieved chunks from vector database
var chunks = new List<DocumentChunk>
{
    new()
    {
        Id = "doc_123_chunk_1",
        Content = "ToonNet provides efficient serialization...",
        Metadata = new() { ["source"] = "docs", ["page"] = "1" }
    }
};

// Serialize chunks for LLM context - minimal tokens
string context = ToonSerializer.Serialize(chunks);

// Use in RAG prompt with reduced token count
var ragPrompt = $"Context:\n{context}\n\nQuestion: How does ToonNet work?";
```

---

### Example 3: Configuration File

```csharp
public class DatabaseConfig
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Database { get; set; }
    public bool UseSSL { get; set; }
}

// Load from file
var toonContent = await File.ReadAllTextAsync("database.toon");
var config = ToonSerializer.Deserialize<DatabaseConfig>(toonContent);

// Use configuration
var connectionString = $"Host={config.Host};Port={config.Port};Database={config.Database}";
```

**database.toon:**
```toon
Host: db.example.com
Port: 5432
Database: myapp_production
UseSSL: true
```

---

### Example 4: JSON to TOON Conversion (API Integration)

```csharp
// Convert existing JSON to token-efficient TOON for AI prompts
var jsonResponse = await httpClient.GetStringAsync("https://api.example.com/data");
var toonData = ToonConvert.FromJson(jsonResponse);

// Use TOON data in AI prompt (fewer tokens)
var aiPrompt = $"Analyze this data:\n{toonData}";

// Or convert back to JSON for other APIs
var jsonForExport = ToonConvert.ToJson(toonData);
```

---

## ✅ Supported Types

ToonNet supports all common .NET types out of the box:

**Primitives:**
- `string`, `int`, `long`, `decimal`, `double`, `float`, `bool`
- `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`

**Collections:**
- `List<T>`, `T[]`, `Dictionary<string, T>`
- `IEnumerable<T>`, `ICollection<T>`

**Complex Types:**
- Classes, Records, Structs
- Nested objects (unlimited depth)
- Nullable types (`int?`, `string?`, etc.)
- Enums

**No attributes required** - works with any C# class.

---

## 🌐 ASP.NET Core Integration

Use TOON format in your web APIs for token-efficient responses:

```csharp
// Program.cs
using ToonNet.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add TOON support to ASP.NET Core
builder.Services.AddControllers()
    .AddToonFormatters();  // Enables TOON input/output

var app = builder.Build();
app.MapControllers();
app.Run();
```

```csharp
// API Controller
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    [Produces("application/toon", "application/json")]  // Support both formats
    public IActionResult GetUser(int id)
    {
        var user = new User { Id = id, Name = "Alice", Age = 28 };
        return Ok(user);
    }
}
```

**Client request:**
```
GET /api/users/1
Accept: application/toon
```

**Response (token-efficient TOON format):**
```toon
Id: 1
Name: Alice
Age: 28
```

💡 **Perfect for AI agents consuming your API** - 40% fewer tokens per request!

📖 **Full guide:** Install `ToonNet.AspNetCore` and `ToonNet.AspNetCore.Mvc` packages

---

## 🎯 Use Cases

**Primary use cases where TOON format excels:**

### 🤖 AI/LLM Applications
- **LLM Prompts**: 30-40% fewer tokens than JSON → lower API costs
- **RAG Systems**: Compact context for vector database retrieval
- **Prompt Engineering**: Clean, readable prompt templates
- **AI Training Data**: Efficient data format for fine-tuning
- **Agent Memory**: Token-efficient context for AI agents

### 📝 Traditional Use Cases
- **Configuration Files**: App settings, deployment configs
- **Data Exchange**: API payloads, system integration
- **Testing**: Test fixtures, mock data
- **Documentation**: Human-readable data examples

**Token efficiency comparison (approximate):**
```
JSON:  {"name":"Alice","age":30,"tags":["dev","admin"]}  → ~15 tokens
TOON:  name: Alice                                        → ~9 tokens
       age: 30
       tags: dev, admin
       
40% reduction → 40% savings on AI API costs
```

---

## 📦 Real-World Samples

Complete, production-ready examples available in [`demo/ToonNet.Demo/Samples/`](demo/ToonNet.Demo/Samples):

### 1. E-Commerce Order System
Full order with customer, items, payment, shipping details.

**Files:** `ecommerce-order.toon`, `.json`, `.yaml`, `ECommerceModels.cs`

### 2. Healthcare Patient Record (EMR)
Medical record with vital signs, diagnoses, medications, lab results.

**Files:** `healthcare-patient.toon`, `.json`, `.yaml`, `HealthcareModels.cs`

**Run samples:**
```bash
dotnet run --project demo/ToonNet.Demo
```

---

## ⚠️ Important: Roundtrip Behavior

ToonNet provides **two types of guarantees**:

### Type-Safe Serialization (C# Objects)
**Exact preservation** - all data preserved exactly:

```csharp
var order = new Order { Total = 35.00m };
string toon = ToonSerializer.Serialize(order);
var restored = ToonSerializer.Deserialize<Order>(toon);
// restored.Total == 35.00m ✅ Exact match
```

**Use for:** Production code, data storage, APIs

### Format Conversion (Strings)
**Semantic equivalence** - values preserved, format may differ:

```csharp
string json = @"{""total"": 35.00}";
string toon = ToonConvert.FromJson(json);
string jsonBack = ToonConvert.ToJson(toon); // {"total": 35}
// 35.00 vs 35 - semantically equal, format differs
```

**Use for:** File conversion, data migration

**What may change:** Decimal formatting (35.00 → 35), whitespace, property order  
**What is preserved:** All values (semantic equality), all structure, all property names

📖 **Detailed explanation: [API-GUIDE.md](docs/API-GUIDE.md#roundtrip-guarantees)**

---

## 🧪 Testing

ToonNet is thoroughly tested:

- **427 passing tests** covering all scenarios
- **100% TOON v3.0 spec compliance**
- All primitive types, collections, nested objects
- Edge cases (null, empty collections, special characters)
- JSON/TOON/YAML conversions
- Roundtrip serialization

```bash
dotnet test
```

---

## 📚 Documentation

| Resource | Description |
|----------|-------------|
| [📖 Documentation Site](https://selcukgural.github.io/ToonNet/) | Complete documentation with API reference |
| [🚀 Getting Started](https://selcukgural.github.io/ToonNet/docs/intro) | Quick start guide |
| [📘 API Reference](https://selcukgural.github.io/ToonNet/docs/api/intro) | Auto-generated API documentation |
| [📄 API Guide](docs/API-GUIDE.md) | Detailed usage guide with examples |
| [📝 TOON Spec](ToonSpec.md) | TOON v3.0 format specification |
| [💡 Samples](demo/ToonNet.Demo/Samples/README.md) | Real-world usage examples |
| [🎮 Demo App](demo/ToonNet.Demo/) | Interactive demo application |

---

## 🛠️ Building from Source

```bash
# Clone repository
git clone https://github.com/selcukgural/ToonNet.git
cd ToonNet

# Build
dotnet build

# Run tests
dotnet test

# Run demo
dotnet run --project demo/ToonNet.Demo
```

**Requirements:** .NET 8.0 or higher

---

## 🗺️ Roadmap

**Current Status:**
- [x] Core serialization (ToonNet.Core)
- [x] JSON ↔ TOON conversion (Extensions.Json)
- [x] YAML ↔ TOON conversion (Extensions.Yaml)
- [x] ASP.NET Core integration (AspNetCore packages)
- [x] Source generators (zero-allocation)
- [x] NuGet package publishing
- [x] Streaming parser for large files
- [x] System.Text.Json-compatible API
- [x] Comprehensive test coverage (444 tests passing)
- [x] Real-world samples (Healthcare, E-Commerce)

**Coming Soon:**
- [ ] VS Code extension with syntax highlighting
- [ ] Online TOON playground/validator
- [ ] Schema validation support
- [ ] Benchmarks vs other serializers

---

## 🤝 Contributing

Contributions welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) first.

---

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details.

---

## 📞 Support

- 🐛 **Issues**: [GitHub Issues](https://github.com/yourusername/ToonNet/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/yourusername/ToonNet/discussions)

---

<div align="center">

**Built for .NET developers**

[Get Started](https://selcukgural.github.io/ToonNet/) • [Documentation](https://selcukgural.github.io/ToonNet/docs/intro) • [API Reference](https://selcukgural.github.io/ToonNet/docs/api/intro)

</div>
