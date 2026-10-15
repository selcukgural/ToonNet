---
sidebar_position: 1
---

# Welcome to ToonNet

ToonNet is a .NET library for serializing and deserializing data in **TOON** (Token-Oriented Object Notation) format. It implements [TOON spec v4.4](toon-spec), passes all 564 official conformance fixtures and writes the same text as the official .NET library.

## What is TOON?

TOON is a human-readable data format designed for:
- **AI/LLM prompts** - Fewer tokens than JSON, especially for arrays of uniform objects
- **Configuration files** - Clean, readable syntax
- **Data exchange** - Human and machine friendly

## Quick Example

```csharp
using ToonNet.Core.Serialization;

// Serialize
var person = new Person { Name = "Alice", Age = 30 };
string toon = ToonSerializer.Serialize(person);   // Name: Alice\nAge: 30

// Deserialize
var restored = ToonSerializer.Deserialize<Person>(toon);

public class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}
```

## Key Features

- 🚀 **Performance** - Compiled accessors, cached type metadata, span-based parsing and pooled buffers
- 💰 **Token Efficient** - Tabular arrays and keyed tables declare field names once, nested field groups flatten
  uniform nested objects into the same rows (lower AI API costs)
- 📐 **Spec v4.4** - All 564 official conformance fixtures pass; output is identical to the official .NET library
- 🎯 **.NET 8 and .NET 10** - Builds for both
- 💻 **Developer Friendly** - System.Text.Json-style API
- 🔄 **Streaming Support** - Multi-document streaming from `IAsyncEnumerable<T>` without materializing the whole dataset
- 🔧 **ASP.NET Core** - Input/output formatters, configuration provider
- 📦 **Format Extensions** - JSON/YAML bidirectional conversion
- 🎯 **Source Generators** - Optional generated `Serialize`/`Deserialize` methods with the same output as `ToonSerializer`

## Beyond Serialization

Other TOON libraries convert values to and from text. ToonNet also lets you work with TOON data where it is, without
converting it first:

| You want to | With ToonNet | Page |
|-------------|--------------|------|
| Find values: the rows over a limit, the users of a domain | JSONPath (RFC 9535, all 715 compliance cases) plus string, type and aggregate functions: `$.orders[?sum(@.lines[*].price) > 1000]` | [Querying](core-features/querying) |
| Read one part of a large document | `DeserializeAt<T>(toon, "$.data.orders")` skips the rest without building it (about 20x faster for a small part) | [Querying](core-features/querying#into-objects) |
| Read only the rows that match | `DeserializeAll<T>(toon, "$.orders[?@.total > 100]")`, filtered on the text (2.3x faster, 13x less memory than LINQ after deserializing) | [Querying](core-features/querying#into-objects) |
| Stream a large array inside a response | `DeserializeAsyncEnumerableAt<T>(stream, "$.data.orders")` with flat memory | [Streaming](core-features/streaming#an-array-inside-a-document) |
| Check business rules | `ToonRuleSet`: Schematron-style rules (`"@.total >= 0"`, `Unique`, `Forbidden("$..password")`) with the path of every violation | [Rules and Validation](core-features/validation) |
| Change a document | Indexers, `SetValue`/`RemoveAll` by path, `DeepClone`, `DeepEquals`, JSON Merge Patch | [Editing Trees](core-features/editing) |
| Talk to a language model | Shortened data for prompts, answer templates, lenient and validated reading of answers | [Language Models](core-features/language-models) |
| Accept queries from clients | Limits on length, nesting, work and time (`ToonPathOptions`), linear-time regular expressions | [Querying](core-features/querying#paths-from-untrusted-input) |

## Documentation Guide

### 🚀 Getting Started
Start here if you're new to ToonNet:
- **[Installation](getting-started/installation)** - NuGet packages and requirements
- **[Migrating to 4.0](getting-started/migration-4)** - Changed defaults and how to keep the old behavior
- **[Quick Start](getting-started/quick-start)** - 5-minute tutorial
- **[Basic Serialization](getting-started/basic-serialization)** - Fundamental examples

### 🎯 Core Features
Deep dive into ToonNet's core functionality:
- **[Serialization](core-features/serialization)** - Convert objects to TOON
- **[Deserialization](core-features/deserialization)** - Convert TOON to objects
- **[Streaming](core-features/streaming)** - Multi-document files and the items of large arrays, one at a time
- **[Reader and Writer](core-features/reader-writer)** - Token-level reading and writing without a tree
- **[Querying](core-features/querying)** - JSONPath with ToonNet functions, over trees, text and into objects
- **[Rules and Validation](core-features/validation)** - Business rules written as paths
- **[Editing Trees](core-features/editing)** - Navigate, change and patch documents
- **[Language Models](core-features/language-models)** - Prompts and answers
- **[Type System](core-features/type-system)** - ToonValue and subclasses
- **[Configuration](core-features/configuration)** - ToonSerializerOptions guide

### 🔌 Format Extensions
Convert between different data formats:
- **[JSON Integration](format-extensions/json-integration)** - JSON ↔ TOON conversion
- **[YAML Integration](format-extensions/yaml-integration)** - YAML ↔ TOON conversion
- **[Converter Examples](format-extensions/custom-formats)** - Converters for dates, identifiers and value objects

### 🌐 ASP.NET Core
Integrate ToonNet with ASP.NET Core:
- **[Dependency Injection](aspnet-core/dependency-injection)** - Service configuration
- **[Input Formatters](aspnet-core/input-formatters)** - Handle TOON requests
- **[Output Formatters](aspnet-core/output-formatters)** - Return TOON responses
- **[Configuration Provider](aspnet-core/configuration-provider)** - TOON config files

### ⚡ Advanced Topics
Optimization and customization:
- **[Performance Tuning](advanced/performance-tuning)** - Optimization strategies
- **[Custom Converters](advanced/custom-converters)** - Type-specific converters
- **[Source Generators](advanced/source-generators)** - Compile-time code generation
- **[Security](advanced/security)** - Defaults and limits for untrusted input

### 📚 Reference
Additional resources:
- **[API Guide](api-guide)** - The API at a glance, in the style of System.Text.Json
- **[TOON Spec](toon-spec)** - TOON spec v4.4 conformance and implementation notes
- **[ToonNet vs Toon.Format](comparison)** - Features and measurements next to the official library

## Quick Links

| Topic | Link |
|-------|------|
| Installation | [Getting Started](getting-started/installation) |
| First Example | [Quick Start](getting-started/quick-start) |
| API Overview | [API Guide](api-guide) |
| Large Files | [Streaming](core-features/streaming) |
| Compared With Toon.Format | [ToonNet vs Toon.Format](comparison) |
| GitHub Repository | [ToonNet on GitHub](https://github.com/selcukgural/ToonNet) |

## Community & Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/selcukgural/ToonNet/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/selcukgural/ToonNet/discussions)

## License

ToonNet is open-source software licensed under the [MIT License](https://github.com/selcukgural/ToonNet/blob/master/LICENSE).

## Thread-Safety

`ToonSerializer` and a shared `ToonEncoder` are safe to use from several threads; see [Configuration](core-features/configuration#thread-safety) for the caches and options.

