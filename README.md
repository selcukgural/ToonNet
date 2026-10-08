<div align="center">

<img src="icon.png" alt="ToonNet Logo" width="128" height="128">

**TOON Data Format Serialization for .NET**

*AI-Optimized • Token-Efficient • Developer-Friendly*

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/ToonNet.Core.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Core/)
[![Downloads](https://img.shields.io/nuget/dt/ToonNet.Core.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Core/)
[![CI](https://github.com/selcukgural/ToonNet/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/selcukgural/ToonNet/actions/workflows/ci.yml)
[![Spec](https://img.shields.io/badge/TOON%20spec%20v3.3.2-378%2F378%20fixtures-brightgreen?style=flat)](docs/TOON_SPEC_v3_COMPLIANCE.md#toonnet-implementation-status)
[![Documentation](https://img.shields.io/badge/docs-online-brightgreen?style=flat&logo=docusaurus)](https://selcukgural.github.io/ToonNet/)

[Quick Start](#-quick-start) • [Documentation](https://selcukgural.github.io/ToonNet/) • [API Reference](https://selcukgural.github.io/ToonNet/docs/api/intro) • [Samples](demo/ToonNet.Demo/Samples)

</div>

---

## What is ToonNet?

ToonNet is a **.NET serialization library** that provides:

- **Serialize** C# objects to TOON format
- **Deserialize** TOON format to C# objects  
- **Convert** between JSON, TOON, and YAML formats
- **System.Text.Json-style API** (`Serialize`/`Deserialize`) that feels familiar

**TOON Format** is a human-readable data format optimized for:
- **AI/LLM prompts** - Fewer tokens than JSON, especially for arrays of uniform objects
- **Configuration files** - Clean, readable syntax
- **Data exchange** - Human and machine friendly

> **TOON Specification:** ToonNet implements [TOON spec v3.3.2](https://github.com/toon-format/spec/blob/v3.3.2/SPEC.md)
> and passes all 378 of its official encode/decode conformance fixtures, which run in CI
> ([details](docs/TOON_SPEC_v3_COMPLIANCE.md#toonnet-implementation-status)). The optional key folding and path expansion
> features (§13.4) are not implemented. TOON spec v4 is not supported yet.

---

## 🤖 Why Developers Choose ToonNet

ToonNet focuses on three things:

1. **🎯 Fewer tokens** - Tabular TOON for uniform data is much smaller than JSON, which lowers LLM input cost
2. **📐 Spec conformance** - Encoder and decoder follow TOON spec v3.3.2 and are tested against the official fixtures
3. **🔧 Familiar API** - `Serialize`/`Deserialize` methods in the style of System.Text.Json

### 🤖 AI Token Optimization

For uniform data, TOON is much more compact than JSON, which usually means fewer tokens in LLM prompts:

```csharp
// Example: Product catalog for AI prompt
var products = new List<Product>
{
    new() { Id = 1, Name = "Laptop", Price = 1299.99m, InStock = true },
    new() { Id = 2, Name = "Mouse", Price = 29.99m, InStock = true },
    new() { Id = 3, Name = "Keyboard", Price = 89.99m, InStock = false }
};

string json = JsonSerializer.Serialize(products);   // compact JSON: 167 characters
string toon = ToonSerializer.Serialize(products);   // TOON: 97 characters (~42% fewer)
```

**JSON output (compact):**
```json
[{"Id":1,"Name":"Laptop","Price":1299.99,"InStock":true},{"Id":2,"Name":"Mouse","Price":29.99,"InStock":true},{"Id":3,"Name":"Keyboard","Price":89.99,"InStock":false}]
```

**TOON output** – uniform objects become a table with the field names declared once:
```toon
[3]{Id,Name,Price,InStock}:
  1,Laptop,1299.99,true
  2,Mouse,29.99,true
  3,Keyboard,89.99,false
```

The saving depends on the shape of the data: it is largest for arrays of uniform objects (tabular form) and
smaller for deeply nested or irregular data. Measure with your own payloads and tokenizer.

**Where it helps:**
- Input-token cost scales with prompt size, so fewer tokens means proportionally lower cost
- RAG context, tool results and other tabular data passed to LLMs

### ⚡ Performance & Architecture

**How it is built:**
- **Expression Trees** - Property getters/setters are compiled once per type instead of using reflection on every call
- **Metadata Caching** - Thread-safe `ConcurrentDictionary` caches for type metadata and property names
- **Source Generator** - Optional static `Serialize`/`Deserialize` methods with the same output as `ToonSerializer`
- **ArrayPool<byte>** - `SerializeToStreamAsync` encodes the TOON text into a pooled buffer before writing to the stream

**Async I/O:**
- **ConfigureAwait(false)** - Awaited I/O calls do not capture the synchronization context
- **Cancellation Support** - `CancellationToken` on all async methods
- **80 KB file buffers** - File-based methods open the file with an 81,920-byte buffer
- **Streaming** - `SerializeStreamAsync`/`DeserializeStreamAsync` process one document at a time

**Thread-Safety:**
- **Concurrent use:** `ToonSerializer` methods are safe to call from multiple threads.
- **Shared caches:** Type metadata and naming caches use `ConcurrentDictionary` for safe concurrent access.
- **Cache lifetime:** Metadata entries are created on demand and retained for the process lifetime (no eviction).
- **Options caution:** Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.

> **Benchmarks:** The BenchmarkDotNet project lives in [`benchmark/ToonNet.Benchmarks`](benchmark/ToonNet.Benchmarks).
> The committed results predate the v3.3.2 encoder/parser rewrite, so run the benchmarks yourself for current numbers.

---

## 📦 Packages

ToonNet is modular - install only what you need:

| Package | Description | NuGet | Downloads | Status |
|---------|-------------|-------|-----------|--------|
| **ToonNet.Core** | Core serialization API - C# ↔ TOON (uses expression trees) | [![NuGet](https://img.shields.io/nuget/v/ToonNet.Core.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Core/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.Core.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Core/) | ✅ Stable |
| **ToonNet.Extensions.Json** | JSON ↔ TOON conversion | [![NuGet](https://img.shields.io/nuget/v/ToonNet.Extensions.Json.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Extensions.Json/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.Extensions.Json.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Extensions.Json/) | ✅ Stable |
| **ToonNet.Extensions.Yaml** | YAML ↔ TOON conversion | [![NuGet](https://img.shields.io/nuget/v/ToonNet.Extensions.Yaml.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Extensions.Yaml/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.Extensions.Yaml.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Extensions.Yaml/) | ✅ Stable |
| **ToonNet.AspNetCore** | ASP.NET Core dependency injection & TOON configuration provider | [![NuGet](https://img.shields.io/nuget/v/ToonNet.AspNetCore.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.AspNetCore/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.AspNetCore.svg?style=flat)](https://www.nuget.org/packages/ToonNet.AspNetCore/) | ✅ Stable |
| **ToonNet.AspNetCore.Mvc** | MVC input/output formatters & `ToonResult` | [![NuGet](https://img.shields.io/nuget/v/ToonNet.AspNetCore.Mvc.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.AspNetCore.Mvc/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.AspNetCore.Mvc.svg?style=flat)](https://www.nuget.org/packages/ToonNet.AspNetCore.Mvc/) | ✅ Stable |
| **ToonNet.SourceGenerators** | Generates static `Serialize`/`Deserialize` methods for `[ToonSerializable]` types | [![NuGet](https://img.shields.io/nuget/v/ToonNet.SourceGenerators.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.SourceGenerators/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.SourceGenerators.svg?style=flat)](https://www.nuget.org/packages/ToonNet.SourceGenerators/) | ✅ Stable |

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

# Generated Serialize/Deserialize methods (source generator)
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
Interests[3]: AI,Machine Learning,Photography
RecentPurchases[2]{Product,Amount}:
  Camera Lens,450
  ML Course,99.99
```

**Token savings:** the field names of `RecentPurchases` are written once instead of per item, so the prompt is smaller than the equivalent JSON.

That's it - no configuration, no attributes, just works.

---

## 📚 API Reference

ToonNet's core methods use familiar System.Text.Json-style naming:

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

// Parse JSON to a C# object (System.Text.Json)
var obj = ToonConvert.DeserializeFromJson<MyClass>(jsonString);

// Convert JSON to TOON, then deserialize with ToonSerializer
var obj2 = ToonConvert.ParseJson<MyClass>(jsonString);

// Serialize C# object to JSON (System.Text.Json, indented by default)
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
| `DeserializeFromJson<T>(json)` | Extensions.Json | JSON string | C# Object | Parse JSON (System.Text.Json) |
| `ParseJson<T>(json)` | Extensions.Json | JSON string | C# Object | Parse JSON via TOON |
| `SerializeToJson<T>(obj)` | Extensions.Json | C# Object | JSON string | Export as JSON |
| `FromYaml(yaml)` | Extensions.Yaml | YAML string | TOON string | Convert YAML to TOON |
| `ToYaml(toon)` | Extensions.Yaml | TOON string | YAML string | Convert TOON to YAML |

### Async & Streaming API

For large datasets (millions of records, database exports, ETL pipelines), ToonNet provides memory-efficient streaming serialization:

```csharp
// Stream large dataset from database without loading all into memory
await ToonSerializer.SerializeStreamAsync(
    items: dbContext.Users.AsAsyncEnumerable(),
    filePath: "users_export.toon",
    cancellationToken: cts.Token
);

// Read back with incremental deserialization
await foreach (var user in ToonSerializer.DeserializeStreamAsync<User>("users_export.toon"))
{
    ProcessUser(user);  // Memory-efficient: only one user in memory at a time
}

// Advanced: Custom separator mode and batch size
await ToonSerializer.SerializeStreamAsync(
    items: GenerateLargeDatasetAsync(),
    filePath: "export.toon",
    options: null,
    writeOptions: new ToonMultiDocumentWriteOptions
    {
        Mode = ToonMultiDocumentSeparatorMode.ExplicitSeparator,  // Use "---" separator
        DocumentSeparator = "---",
        BatchSize = 100  // Buffer 100 items before writing (improves throughput)
    },
    cancellationToken: cts.Token
);
```

**Use Cases:**
- **Database exports** - Stream millions of records without OOM
- **ETL pipelines** - Process large files incrementally
- **Log processing** - Parse multi-GB log files
- **Data migration** - Convert large datasets with minimal memory footprint

**Performance:**
- **Memory:** Independent of the number of items (roughly batch size × item size in memory)
- **Throughput:** Writes are batched (`BatchSize`, default 50 items) to reduce I/O calls
- **Cancellation:** Full CancellationToken support for long-running operations

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

// Result: fewer input tokens than the same data as JSON
```

**Output (compact, AI-friendly):**
```toon
Name: Alice Johnson
RecentOrders[2]{Id,Total,Status}:
  ORD-001,299.99,Delivered
  ORD-002,149.5,Shipped
Preferences[3]: Electronics,Fast Shipping,Eco-Friendly
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

