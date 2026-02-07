---
sidebar_position: 1
---

# Welcome to ToonNet

ToonNet is a high-performance .NET library for serializing and deserializing data in **TOON** (Token-Optimized Object Notation) format.

## What is TOON?

TOON is a human-readable data format designed for:
- **AI/LLM prompts** - Up to 40% fewer tokens than JSON
- **Configuration files** - Clean, readable syntax
- **Data exchange** - Human and machine friendly

## Quick Example

```csharp
using ToonNet.Core;

// Serialize
var person = new Person { Name = "Alice", Age = 30 };
string toon = ToonSerializer.Serialize(person);

// Deserialize
var restored = ToonSerializer.Deserialize<Person>(toon);
```

## Key Features

- 🚀 **High Performance** - Expression trees, not reflection (10-100x faster)
- 💰 **Token Efficient** - 40% fewer tokens than JSON (lower AI API costs)
- 💻 **Developer Friendly** - System.Text.Json-compatible API
- 🔧 **ASP.NET Core** - Input/output formatters, configuration provider
- 📦 **Format Extensions** - JSON/YAML bidirectional conversion
- 🎯 **Source Generators** - Compile-time code generation

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
- **[TOON Spec](toon-spec)** - TOON v3.0 specification

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

ToonNet is open-source software licensed under the [MIT License](https://github.com/selcukgural/ToonNet/blob/main/LICENSE).

## Thread-Safety

- `ToonSerializer` methods are safe to call concurrently across threads.
- Shared metadata/name caches use `ConcurrentDictionary` for concurrent access.
- Cache entries are created on demand and retained for the process lifetime (no eviction).
- Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.
