---
sidebar_position: 1
---

# Getting Started with ToonNet

**ToonNet** is a high-performance TOON format serialization library for .NET 8+. It provides zero-reflection, expression tree-based serialization with excellent performance characteristics.

## What is TOON?

TOON (Token Optimized Object Notation) is a modern data format designed for:
- 🚀 **AI/LLM Token Optimization** - Minimal token usage for language models
- ⚡ **High Performance** - Faster parsing and smaller payloads than JSON
- 📦 **Human Readable** - Clean, intuitive syntax
- 🔧 **Type Safe** - Strong typing support

## Quick Installation

Install via NuGet Package Manager:

```bash
dotnet add package ToonNet.Core
```

Or via Package Manager Console:

```powershell
Install-Package ToonNet.Core
```

## Your First Serialization

```csharp
using ToonNet;

// Define your model
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Hobbies { get; set; }
}

// Serialize to TOON
var person = new Person 
{ 
    Name = "Alice", 
    Age = 30, 
    Hobbies = new List<string> { "Reading", "Gaming" } 
};

string toonString = ToonConvert.Serialize(person);
// Output: @Person{Name:Alice Age:30 Hobbies:[@Reading @Gaming]}

// Deserialize from TOON
var deserialized = ToonConvert.Deserialize<Person>(toonString);
```

## Available Packages

ToonNet consists of several packages:

- **ToonNet.Core** - Core serialization engine (required)
- **ToonNet.Extensions.Json** - JSON interoperability
- **ToonNet.Extensions.Yaml** - YAML interoperability
- **ToonNet.AspNetCore** - ASP.NET Core integration
- **ToonNet.SourceGenerators** - Compile-time code generation

## Features

✅ **435+ Tests** - Comprehensive test coverage  
✅ **100% TOON v3.0 Spec Compliance** - Full specification support  
✅ **Zero Reflection** - Expression tree-based for maximum performance  
✅ **Object Pooling** - Reduced GC pressure  
✅ **Streaming Support** - Memory-efficient for large data  
✅ **Source Generators** - AOT-friendly code generation

## Next Steps

- 📖 [API Guide](./api-guide.md) - Detailed API documentation
- 🔧 [TOON Spec Compliance](./toon-spec.md) - Format specification
- 📚 [API Reference](./api/intro.md) - Complete API reference

## Performance

ToonNet is designed for high performance:
- Expression tree compilation (no reflection overhead)
- Object pooling for reduced allocations
- Optimized parsing and serialization paths
- Efficient memory usage

## License

ToonNet is licensed under the [MIT License](https://github.com/selcukgural/ToonNet/blob/main/LICENSE).
