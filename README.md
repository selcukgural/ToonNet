![ToonNet](https://raw.githubusercontent.com/selcukgural/ToonNet/master/icon.png)

**TOON Data Format Serialization for .NET**

*AI-Optimized • Token-Efficient • Developer-Friendly*

[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/ToonNet.Core.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Core/)
[![Downloads](https://img.shields.io/nuget/dt/ToonNet.Core.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Core/)
[![CI](https://github.com/selcukgural/ToonNet/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/selcukgural/ToonNet/actions/workflows/ci.yml)
[![Spec](https://img.shields.io/badge/TOON%20spec%20v4.4-564%2F564%20fixtures-brightgreen?style=flat)](https://github.com/selcukgural/ToonNet/blob/master/docs/TOON_SPEC_COMPLIANCE.md#toonnet-implementation-status)
[![Documentation](https://img.shields.io/badge/docs-online-brightgreen?style=flat&logo=docusaurus)](https://selcukgural.github.io/ToonNet/)

[Quick Start](#-quick-start) • [Documentation](https://selcukgural.github.io/ToonNet/) • [API Reference](https://selcukgural.github.io/ToonNet/docs/api-guide) • [Samples](https://github.com/selcukgural/ToonNet/tree/master/demo/ToonNet.Demo/Samples)

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

> **TOON Specification:** ToonNet implements [TOON spec v4.4](https://github.com/toon-format/spec/blob/v4.4.0/SPEC.md)
> completely, including comments, nested field groups and keyed tables, and passes all 564 of its official
> encode/decode conformance fixtures, which run in CI
> ([details](https://github.com/selcukgural/ToonNet/blob/master/docs/TOON_SPEC_COMPLIANCE.md#toonnet-implementation-status)).
> Its output is identical to the official .NET library's, and it is many times faster
> ([comparison](#toonnet-vs-the-official-toonformat-library)).

---

## 🤖 Why Developers Choose ToonNet

ToonNet focuses on three things:

1. **🎯 Fewer tokens** - Tabular TOON for uniform data is much smaller than JSON, which lowers LLM input cost
2. **📐 Spec conformance** - Encoder and decoder follow TOON spec v4.4, pass every official fixture and write the
   same text as the official library
3. **🔧 Familiar API** - `Serialize`/`Deserialize` methods in the style of System.Text.Json
4. **🔎 Query, validate, edit, stream** - JSONPath (RFC 9535) with string, type and aggregate functions, over trees
   and straight over text; `DeserializeAt` for one part of a large document; Schematron-style rules; JSON Merge Patch;
   and helpers for prompts and model answers. Most of this is not in other TOON libraries.

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

Nested objects of the same shape stay in the table as **nested field groups**, and a dictionary (or object) of
uniform objects becomes a **keyed table** (TOON spec v4):

```toon
orders[2]{id,customer{name,country},total}:
  1,Ada,DK,99
  2,Bob,UK,149
servers[2:]{host,port}:
  alpha: a.example.com,8080
  beta: b.example.com,9090
```

The saving depends on the shape of the data: it is largest for arrays of uniform objects (tabular form) and
smaller for deeply nested or irregular data. Measure with your own payloads and tokenizer.

**Where it helps:**
- Input-token cost scales with prompt size, so fewer tokens means proportionally lower cost
- RAG context, tool results and other tabular data passed to LLMs

### ⚡ Performance & Architecture

**How it is built:**
- **Straight to text** - `ToonSerializer` writes objects to TOON text and reads them from it directly, without an
  intermediate document; the tabular forms (including nested field groups and keyed tables) are planned from the
  objects and written in one pass
- **Source Generator** - `[ToonSerializable]` types get generated writers and readers that `ToonSerializer` uses
  automatically: keys are encoded and matched at compile time, primitives are written without boxing
- **Compiled accessors** - For other types, property getters/setters and constructors are compiled once per type;
  primitive properties are read and written through typed delegates, so their values are not boxed
- **Cursor parser** - One quote-aware scan per line, values read from spans, the line table and buffers rented from
  `ArrayPool`, repeated keys and headers shared instead of allocated again
- **UTF-8 in and out** - Bytes, spans, streams and `IBufferWriter<byte>` (a `PipeWriter`) are read and written through
  pooled buffers, without intermediate strings: reading bytes allocates 18–46% less than going through a string, and
  `SerializeToUtf8Bytes` is 4–13% faster with a third of the allocation
- **NativeAOT and trimming** - `ToonNet.Core` is annotated; `ToonSerializer.Serialize(catalog, Catalog.ToonTypeInfo)`
  and the other overloads that take a generated type info need no reflection and keep the fast path (tested with
  NativeAOT in CI)
- **.NET 8 and .NET 10** - Builds for both; `ToonNet.Core` has no package dependencies

**Async I/O:**
- **ConfigureAwait(false)** - Awaited I/O calls do not capture the synchronization context
- **Cancellation Support** - `CancellationToken` on all async methods
- **80 KB file buffers** - File-based methods open the file with an 81,920-byte buffer
- **Streaming** - `SerializeDocumentsAsync`/`DeserializeDocumentsAsync` process one document at a time

**Thread-Safety:**
- **Concurrent use:** `ToonSerializer` methods are safe to call from multiple threads.
- **Shared caches:** Type metadata and factory caches use `ConcurrentDictionary`; per-property name caches are filled
  idempotently, so concurrent use is safe.
- **Cache lifetime:** Metadata entries are created on demand and retained for the process lifetime (no eviction).
- **Options caution:** Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.

> **Benchmarks:** See [ToonNet vs the official Toon.Format library](#toonnet-vs-the-official-toonformat-library) for
> measured numbers. The BenchmarkDotNet projects live in
> [`benchmark/ToonNet.Comparison`](https://github.com/selcukgural/ToonNet/tree/master/benchmark/ToonNet.Comparison)
> (against Toon.Format and System.Text.Json) and
> [`benchmark/ToonNet.Benchmarks`](https://github.com/selcukgural/ToonNet/tree/master/benchmark/ToonNet.Benchmarks)
> (ToonNet internals).

---

## ToonNet vs the official Toon.Format library

[Toon.Format](https://www.nuget.org/packages/Toon.Format) is the official .NET implementation of TOON, from the
authors of the spec. Both libraries implement spec v4.4 and **write the same text**; ToonNet does it **8–27x faster
when serializing and 2.4–7x faster when deserializing, with 6–55x less memory**, and adds a source generator, NativeAOT
support, exact decimals and ASP.NET Core integration.

### Same output

- Both pass all **564** official conformance fixtures.
- **20,000 random JSON documents** (tricky strings and keys, numbers, nested and uniform objects, keyed objects, all
  three delimiters) encode to **byte-identical** text, and each library reads the other's text back to the same JSON.
- Typed data – flat records, nested objects with arrays, nested field groups and keyed tables – serializes to
  identical text and reads back to the same values.

`tests/ToonNet.Compatibility.Tests` checks this on every build. To run the full report yourself:

```bash
dotnet run -c Release --project benchmark/ToonNet.Comparison -- parity
```

The remaining differences are in how .NET values are mapped to the data model, which spec §3 leaves to
implementations:

| Value | ToonNet | Toon.Format 1.0.0 |
|-------|---------|-------------------|
| enum `DayOfWeek.Friday` | `Day: Friday` | `Day: 5` |
| `decimal` 12345678901234567.89 | `12345678901234567.89` | `12345678901234568` (rounded through `double`) |
| `decimal` 123456789012345678901234567 | `1.23456789012345678901234567e+26` | `1.2345678901234568e+26` |
| `ulong.MaxValue` | `18446744073709551615` | `18446744073709552000` (cannot be read back into a `ulong`) |

Toon.Format converts numbers to `double`, so values beyond its precision do not survive a round trip (spec §2 asks
that `decode(encode(x))` equals `x`); ToonNet keeps every digit of `decimal`, `long` and `ulong` values.

### Performance

10,000 items, Apple M3 Max, .NET 10.0.5, BenchmarkDotNet 0.15.8. "ToonNet" uses `[ToonSerializable]` models (generated
code), "reflection" the same models without it; System.Text.Json writes and reads the same data as JSON, for
reference. Setup checks that both TOON libraries produce identical text first.

**Serialize**

| Data | Toon.Format | ToonNet | ToonNet (reflection) | System.Text.Json |
|------|------------:|--------:|---------------------:|-----------------:|
| Employees (tabular) | 45.58 ms · 51.8 MB | **2.01 ms · 1.7 MB** (22.7x faster) | 2.64 ms · 1.7 MB | 2.22 ms · 3.3 MB |
| Orders (nested objects, arrays) | 73.86 ms · 85.0 MB | **2.75 ms · 4.6 MB** (26.9x faster) | 3.93 ms · 4.6 MB | 3.10 ms · 4.1 MB |
| Products (nested field groups) | 49.09 ms · 53.1 MB | **2.74 ms · 1.0 MB** (17.9x faster) | 3.19 ms · 1.0 MB | 2.98 ms · 2.7 MB |
| Servers (keyed table) | 21.47 ms · 27.6 MB | **0.94 ms · 1.0 MB** (22.8x faster) | 0.99 ms · 1.0 MB | 0.93 ms · 1.5 MB |

**Deserialize**

| Data | Toon.Format | ToonNet | ToonNet (reflection) | System.Text.Json |
|------|------------:|--------:|---------------------:|-----------------:|
| Employees (tabular) | 31.61 ms · 34.3 MB | **5.06 ms · 2.3 MB** (6.2x faster) | 5.42 ms · 2.3 MB | 4.17 ms · 2.5 MB |
| Orders (nested objects, arrays) | 64.70 ms · 78.3 MB | **9.29 ms · 5.4 MB** (7.0x faster) | 10.75 ms · 5.4 MB | 8.44 ms · 5.7 MB |
| Products (nested field groups) | 30.68 ms · 28.8 MB | **4.80 ms · 3.4 MB** (6.4x faster) | 5.58 ms · 3.4 MB | 6.31 ms · 3.5 MB |
| Servers (keyed table) | 15.30 ms · 21.9 MB | **3.49 ms · 3.1 MB** (4.4x faster) | 3.72 ms · 3.1 MB | 2.75 ms · 2.5 MB |

With 100 items ToonNet serializes 10–12.7x and deserializes 2.9–3.5x faster than Toon.Format. The overloads that take
a `ToonTypeInfo` (no reflection, for trimming and NativeAOT) are as fast as "ToonNet": 18.6–27x and 4.7–7.1x faster
than Toon.Format with 10,000 items. ToonNet writes as fast as System.Text.Json or faster (0.89–1.01x its time) and
takes 0.76–1.27x its time when reading (less for nested field groups), while its TOON output is much smaller than the JSON. Full results, including 100 items: [benchmark/ToonNet.Comparison/RESULTS.md](https://github.com/selcukgural/ToonNet/blob/master/benchmark/ToonNet.Comparison/RESULTS.md).
Run them on your hardware with `dotnet run -c Release --project benchmark/ToonNet.Comparison -- --filter "*"`.

**Why:** Toon.Format serializes a value with System.Text.Json into a `JsonNode` tree and encodes the tree, and decodes
text into a `JsonNode` tree that System.Text.Json then binds. ToonNet writes objects straight to TOON text and reads
them straight from it, with code generated for each type.

### Features

| | ToonNet | Toon.Format 1.0.0 |
|---|---|---|
| TOON spec | v4.4, 564/564 fixtures | v4.4, 564/564 fixtures |
| Targets | .NET 8, .NET 10 | .NET 10, .NET Standard 2.0 |
| Typed API | `ToonSerializer.Serialize`/`Deserialize` (text, streams, files) | `ToonEncoder.Encode<T>`/`ToonDecoder.Decode<T>` via System.Text.Json |
| Source generator | ✅ `[ToonSerializable]` | – |
| NativeAOT / trimming | ✅ `ToonSerializer` with generated type infos, tested in CI | – |
| Document model | `ToonDocument` / `ToonValue` | `JsonNode` |
| Options | indent, delimiter, strict, naming policies, null handling, read-only properties, converters, max depth | indent, delimiter, strict |
| Exact `decimal` / `ulong` | ✅ | rounded through `double` |
| Multi-document streaming | ✅ `SerializeDocumentsAsync` / `DeserializeDocumentsAsync` | single document per stream |
| Queries | ✅ JSONPath (RFC 9535, 715/715 compliance cases) over trees and text, string/type/aggregate and your own functions; `DeserializeAt`, `DeserializeAll` | – |
| Business rules | ✅ `ToonRuleSet`: assertions, `Required`, `Forbidden`, `Unique`, with the path of each violation | – |
| Editing | ✅ indexers, `GetValue<T>`, `DeepClone`, `DeepEquals`, `SetValue`/`RemoveAll` by path, JSON Merge Patch | `JsonNode` |
| LLM helpers | ✅ `ToonPrompt`: shortened arrays, answer templates, lenient reading of answers | – |
| ASP.NET Core | input/output formatters, `ToonResult`, configuration provider | – |
| JSON / YAML conversion | ✅ `ToonConvert`, `ToonYamlConvert` | via `JsonNode` |

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

// UTF-8 in and out, without intermediate strings
byte[] utf8 = ToonSerializer.SerializeToUtf8Bytes(myObject);
ToonSerializer.Serialize(bufferWriter, myObject);              // any IBufferWriter<byte>, e.g. a PipeWriter
var fromBytes = ToonSerializer.Deserialize<MyClass>(utf8);     // ReadOnlySpan<byte>, ReadOnlySpan<char> or Stream
```

### Reader and Writer

`ToonDocumentWriter` and `ToonDocumentReader` write and read documents value by value, without a `ToonDocument` tree:

```csharp
using var writer = new ToonDocumentWriter(stream);
writer.WriteStartObject();
writer.WriteStartTable("users", 2, ["id", "name"]);
writer.WriteNumberValue(1); writer.WriteStringValue("Ada");
writer.WriteNumberValue(2); writer.WriteStringValue("Bob");
writer.WriteEndArray();
writer.WriteEndObject();

using var reader = new ToonDocumentReader(utf8Bytes);
while (reader.Read())
{
    if (reader.TokenType == ToonTokenType.PropertyName) Console.WriteLine(reader.GetPropertyName());
}
```

### Querying and Editing

Paths are JSONPath (RFC 9535); they select values in a tree, straight from text, or straight into objects:

```csharp
// Only the value at a path: the rest is skipped, not built (about 20x faster for a small part of a large document)
var orders = ToonSerializer.DeserializeAt<List<Order>>(toon, "$.data.orders");

// Every value a filter selects, read straight into objects
var paid = ToonSerializer.DeserializeAll<Order>(toon, "$.data.orders[?@.total > 100 && @.paid == true]");

// The items of an array inside a document, streamed
await foreach (var order in ToonSerializer.DeserializeAsyncEnumerableAt<Order>(stream, "$.data.orders")) { }

// ToonNet functions in filters, and your own
var admins = ToonSerializer.DeserializeAll<User>(toon, "$.users[?ends_with(@.email, '@firma.com') && contains(@.roles, 'admin')]");
var big = ToonSerializer.DeserializeAll<Order>(toon, "$.orders[?sum(@.lines[*].price) > 1000]");
var options = new ToonPathOptions { Functions = new ToonPathFunctions().AddPredicate("is_vip", c => IsVip(c)) };

// Paths from clients: limits on length, nesting, work and time
var safe = new ToonSerializerOptions { PathOptions = new ToonPathOptions { MaxLength = 256, Timeout = TimeSpan.FromMilliseconds(200) } };

// Trees: navigate, query, change, patch
var root = ToonDocument.Parse(toon).Root;
string name = root["users"]![0]["name"]!.GetValue<string>();
var ids = root.Select("$.orders[?@.total > 100].id");
root.SetValue("$.meta.updatedBy", "selcuk");
root.RemoveAll("$.users[?@.active == false]");
root = ToonMergePatch.Apply(root, patch);     // JSON Merge Patch (RFC 7386)
```

### Rules and Validation

Business rules as paths, in the manner of Schematron, with the location of every value that breaks one:

```csharp
var rules = new ToonRuleSet()
    .Rule("$.orders[*].total", "@ >= 0", "An order total cannot be negative ({value})")
    .Rule("$.orders[*]", "sum(@.lines[*].price) == @.total", "The lines do not add up to the total")
    .Required("$.customer.email", "The customer needs an email")
    .Forbidden("$..password", "Passwords must not be sent")
    .Unique("$.orders[*].id", "Order id {value} repeats");

var result = rules.Validate(toon);    // result.Violations: $['orders'][1]['total']: An order total cannot be negative (-5)
result.ThrowIfInvalid();              // or ToonPrompt.DeserializeValidated<T>(answer, rules) for model answers
```

### Language Models

```csharp
using ToonNet.Core.Llm;

var context = ToonPrompt.Serialize(orders, new ToonPromptOptions { MaxArrayItems = 50 }); // "# 380 more items"
var shape   = ToonPrompt.DescribeShape<Summary>();      // Lines[N]{Sku,Quantity}: <string>,<integer> ...
var summary = ToonPrompt.Deserialize<Summary>(answer);  // the ```toon block, read leniently
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
| `SerializeToUtf8Bytes<T>(obj)` | Core | C# Object | UTF-8 bytes | Network, caches |
| `Serialize<T>(IBufferWriter<byte> \| Stream, obj)` | Core | C# Object | UTF-8 | Pipelines, synchronous I/O |
| `Deserialize<T>(utf8)` | Core | UTF-8 bytes, chars or stream | C# Object | Read bytes without a string |
| `DeserializeAt<T>(toon, path)` | Core | TOON text | C# Object | One part of a large document |
| `DeserializeAll<T>(toon, path)` | Core | TOON text | List of objects | Rows that pass a filter |
| `DeserializeAsyncEnumerableAt<T>(stream, path)` | Core | UTF-8 stream | Items as they arrive | A large array inside a document |
| `ToonPath.Parse(path).Select(root)` / `.SelectFrom(toon)` | Core | Tree or TOON text | Values | Queries |
| `ToonMergePatch.Apply(target, patch)` | Core | Trees | Tree | Partial updates |
| `ToonRuleSet.Validate(toon)` | Core | TOON text or tree | Violations with paths | Business rules |
| `ToonPrompt.Serialize` / `DescribeShape` / `Deserialize` | Core | Objects, model answers | TOON, templates, objects | LLM prompts and answers |
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
await ToonSerializer.SerializeDocumentsAsync(
    items: dbContext.Users.AsAsyncEnumerable(),
    filePath: "users_export.toon",
    cancellationToken: cts.Token
);

// Read back with incremental deserialization
await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<User>("users_export.toon"))
{
    ProcessUser(user);  // Memory-efficient: only one user in memory at a time
}

// Advanced: Custom separator mode and batch size
await ToonSerializer.SerializeDocumentsAsync(
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

One large root array (an export written as a table) can be read item by item, without loading the document:

```csharp
await foreach (var user in ToonSerializer.DeserializeAsyncEnumerable<User>(File.OpenRead("users.toon")))
{
    ProcessUser(user);
}
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

📖 **Full API documentation: [API-GUIDE.md](https://github.com/selcukgural/ToonNet/blob/master/docs/API-GUIDE.md)**

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

**Output** – the metadata dictionaries have the same keys, so they become a nested field group of the table:
```toon
[1]{Id,Content,Metadata{source,page}}:
  doc_123_chunk_1,ToonNet provides efficient serialization...,docs,"1"
```

(`"1"` is quoted because it is a string that would otherwise read as a number.)

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

