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
- **System.Text.Json-compatible API** for zero learning curve

**TOON Format** is a human-readable data format optimized for:
- **AI/LLM prompts** - Up to 40% fewer tokens than JSON
- **Configuration files** - Clean, readable syntax
- **Data exchange** - Human and machine friendly

> **TOON Specification:** ToonNet implements [TOON spec v3.3.2](https://github.com/toon-format/spec/blob/v3.3.2/SPEC.md)
> and passes all 378 of its official encode/decode conformance fixtures, which run in CI
> ([details](docs/TOON_SPEC_v3_COMPLIANCE.md#toonnet-implementation-status)). The optional key folding and path expansion
> features (§13.4) are not implemented. TOON spec v4 is not supported yet.

---

## 🤖 Why Developers Choose ToonNet

ToonNet delivers **production-grade performance** with three critical advantages:

1. **🎯 40% Token Reduction** - Fewer tokens = Lower AI API costs
2. **⚡ Extreme Performance** - 2-4x faster than competitors with near-zero allocations
3. **🔧 Zero Learning Curve** - System.Text.Json-compatible API

### ⚡ Performance First - Built for Production

ToonNet is **obsessively optimized** for high-throughput, low-latency production environments:

**Benchmark-Proven Speed (Apple M3 Max, .NET 8.0):**
```
Payload Size    Speed Improvement    Memory Saved    GC Pressure
───────────────────────────────────────────────────────────────
100 Bytes       1.16x faster         100% (0 alloc)  ZERO ✅
1 KB            1.98x faster         100% (0 alloc)  ZERO ✅
10 KB           2.37x faster         100% (0 alloc)  ZERO ✅
100 KB          4.40x faster         99.99% saved    ZERO ✅
```

**Real-World Impact:**
- **Stream operations:** 1.61x faster, 50% less memory
- **Large payloads (100KB+):** Up to **4.4x speed boost** ⚡
- **GC collections:** **ZERO** (ArrayPool eliminates allocations)
- **Deadlock risk:** **ELIMINATED** (proper ConfigureAwait usage)

**Architecture Excellence:**
- **Expression Trees** - Compiled property accessors (10-100x faster than reflection)
- **ArrayPool<T>** - Reusable memory buffers, zero heap allocations
- **SIMD Vectorization** - Hardware-accelerated parallel processing
- **Source Generators** - Compile-time code generation for AOT compatibility
- **Thread-Safe Caching** - `ConcurrentDictionary` for concurrent scenarios
- **ConfigureAwait(false)** - No deadlocks in WPF/WinForms/legacy environments

```csharp
// Hot path performance (after warmup)
var toon = ToonSerializer.Serialize(largeObject);  
// 100KB payload: ~3.7μs, 2 bytes allocated (vs 16.4μs, 133KB with GetBytes)
// That's 340% faster with 99.99% less memory! 🚀
```

> **Performance Guarantee:** All numbers are **real BenchmarkDotNet measurements**, not estimates. ToonNet is benchmarked on every release to prevent regressions.

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

ToonNet is **engineered for extreme performance** in production environments:

**Zero-Allocation Hot Paths:**
- **ArrayPool<T>** - Reusable byte buffers eliminate heap allocations (99.99% reduction)
- **Expression Trees** - Compiled property accessors (10-100x faster than reflection)
- **Source Generators** - Compile-time code generation for zero-allocation serialization
- **Metadata Caching** - Thread-safe `ConcurrentDictionary` for type metadata
- **SIMD Operations** - Hardware-accelerated string processing
- **No runtime reflection** overhead after first access

**Latest Optimizations (v1.3.0):**
```csharp
// ArrayPool optimization - Near-zero allocations
using var stream = new MemoryStream();
await ToonSerializer.SerializeToStreamAsync(data, stream);
// 10KB: 901ns, 13KB allocated (vs 1,454ns, 27KB with old approach)
// Result: 1.61x faster, 50% less memory, ZERO GC pressure ✅
```

**Production-Ready Async:**
- **ConfigureAwait(false)** - Eliminates deadlock risk in all environments
- **Cancellation Support** - Full CancellationToken propagation
- **80KB Buffers** - Large file I/O optimization (20x larger than default)
- **Concurrent Operations** - Thread-safe by design

**Architecture highlights:**
- **Compiled getters/setters** - Expression trees compiled to IL, not reflection calls
- **Memory pooling** - ArrayPool<byte> for stream operations
- **Span<T> and Memory<T>** - Modern .NET APIs for reduced allocations
- **Source generator option** - AOT-compatible, zero-allocation code generation

**Thread-Safety:**
- **Concurrent use:** `ToonSerializer` methods are safe to call from multiple threads.
- **Shared caches:** Type metadata and naming caches use `ConcurrentDictionary` for safe concurrent access.
- **Cache lifetime:** Metadata entries are created on demand and retained for the process lifetime (no eviction).
- **Options caution:** Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.

> **When to use ToonNet:** High-throughput APIs, real-time systems, AI/LLM applications, microservices with tight latency budgets. Benchmark-proven 2-4x faster than traditional serializers.

---

## 📦 Packages

ToonNet is modular - install only what you need:

| Package | Description | NuGet | Downloads | Status |
|---------|-------------|-------|-----------|--------|
| **ToonNet.Core** | Core serialization API - C# ↔ TOON (uses expression trees) | [![NuGet](https://img.shields.io/nuget/v/ToonNet.Core.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Core/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.Core.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Core/) | ✅ Stable |
| **ToonNet.Extensions.Json** | JSON ↔ TOON conversion | [![NuGet](https://img.shields.io/nuget/v/ToonNet.Extensions.Json.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Extensions.Json/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.Extensions.Json.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Extensions.Json/) | ✅ Stable |
| **ToonNet.Extensions.Yaml** | YAML ↔ TOON conversion | [![NuGet](https://img.shields.io/nuget/v/ToonNet.Extensions.Yaml.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Extensions.Yaml/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.Extensions.Yaml.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Extensions.Yaml/) | ✅ Stable |
| **ToonNet.AspNetCore** | ASP.NET Core middleware & formatters | [![NuGet](https://img.shields.io/nuget/v/ToonNet.AspNetCore.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.AspNetCore/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.AspNetCore.svg?style=flat)](https://www.nuget.org/packages/ToonNet.AspNetCore/) | ✅ Stable |
| **ToonNet.AspNetCore.Mvc** | MVC input/output formatters | [![NuGet](https://img.shields.io/nuget/v/ToonNet.AspNetCore.Mvc.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.AspNetCore.Mvc/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.AspNetCore.Mvc.svg?style=flat)](https://www.nuget.org/packages/ToonNet.AspNetCore.Mvc/) | ✅ Stable |
| **ToonNet.SourceGenerators** | Compile-time code generation (AOT-compatible, zero-allocation) | [![NuGet](https://img.shields.io/nuget/v/ToonNet.SourceGenerators.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.SourceGenerators/) | [![Downloads](https://img.shields.io/nuget/dt/ToonNet.SourceGenerators.svg?style=flat)](https://www.nuget.org/packages/ToonNet.SourceGenerators/) | ✅ Stable |

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
Interests[3]: AI,Machine Learning,Photography
RecentPurchases[2]{Product,Amount}:
  Camera Lens,450
  ML Course,99.99
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
- **Memory:** Constant O(1) regardless of dataset size (only batch size × item size in memory)
- **Throughput:** Batched writes reduce I/O overhead by ~2-3x compared to unbuffered
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

// Result: 40% fewer tokens = 40% lower AI API costs
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

