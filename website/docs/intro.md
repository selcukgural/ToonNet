---
sidebar_position: 1
---

# Welcome to ToonNet

ToonNet is a .NET library for serializing and deserializing data in **TOON** (Token-Oriented Object Notation) format. It implements [TOON spec v3.3.2](toon-spec).

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
string toon = ToonSerializer.Serialize(person);

// Deserialize
var restored = ToonSerializer.Deserialize<Person>(toon);
```

## Key Features

- 🚀 **Performance** - Compiled expression-tree property accessors and cached type metadata
- 💰 **Token Efficient** - Tabular arrays declare field names once (lower AI API costs)
- 💻 **Developer Friendly** - System.Text.Json-style API
- 🔄 **Streaming Support** - Multi-document streaming from `IAsyncEnumerable<T>` without materialising the whole dataset
- 🔧 **ASP.NET Core** - Input/output formatters, configuration provider
- 📦 **Format Extensions** - JSON/YAML bidirectional conversion
- 🎯 **Source Generators** - Optional generated `Serialize`/`Deserialize` methods with the same output as `ToonSerializer`

## Documentation Guide

### 🚀 Getting Started
Start here if you're new to ToonNet:
- **[Installation](getting-started/installation)** - NuGet packages and requirements
- **[Quick Start](getting-started/quick-start)** - 5-minute tutorial
- **[Basic Serialization](getting-started/basic-serialization)** - Fundamental examples

### 🎯 Core Features
Deep dive into ToonNet's core functionality:
- **[Serialization](core-features/serialization)** - Convert objects to TOON
- **[Deserialization](core-features/deserialization)** - Convert TOON to objects
- **[Type System](core-features/type-system)** - ToonValue and subclasses
- **[Configuration](core-features/configuration)** - ToonSerializerOptions guide

### 🔌 Format Extensions
Convert between different data formats:
- **[JSON Integration](format-extensions/json-integration)** - JSON ↔ TOON conversion
- **[YAML Integration](format-extensions/yaml-integration)** - YAML ↔ TOON conversion
- **[Custom Formats](format-extensions/custom-formats)** - Create custom converters

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

### 📚 Reference
Additional resources:
- **[API Guide](api-guide)** - Complete API reference
- **[TOON Spec](toon-spec)** - TOON spec v3.3.2 conformance and implementation notes

## Quick Links

| Topic | Link |
|-------|------|
| Installation | [Getting Started](getting-started/installation) |
| First Example | [Quick Start](getting-started/quick-start) |
| API Reference | [API Guide](api-guide) |
| GitHub Repository | [ToonNet on GitHub](https://github.com/selcukgural/ToonNet) |

## Community & Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/selcukgural/ToonNet/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/selcukgural/ToonNet/discussions)

## License

ToonNet is open-source software licensed under the [MIT License](https://github.com/selcukgural/ToonNet/blob/master/LICENSE).

## Thread-Safety

- `ToonSerializer` methods are safe to call concurrently across threads.
- Shared metadata/name caches use `ConcurrentDictionary` for concurrent access.
- Cache entries are created on demand and retained for the process lifetime (no eviction).
- Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.
