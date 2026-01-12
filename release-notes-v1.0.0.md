# ToonNet v1.0.0 - Initial Public Release 🚀

## Overview
First stable release of **ToonNet** - A high-performance .NET serialization library for the **TOON format** (Tree Object Oriented Notation).

## 📦 NuGet Packages
All packages are now available on NuGet.org:

- **ToonNet.Core** - Core serialization engine
- **ToonNet.Extensions.Json** - JSON ↔ TOON conversion
- **ToonNet.Extensions.Yaml** - YAML ↔ TOON conversion  
- **ToonNet.SourceGenerators** - Compile-time code generation
- **ToonNet.AspNetCore** - Dependency injection
- **ToonNet.AspNetCore.Mvc** - MVC formatters

## ✨ Key Features

### Performance
- 🚀 **10-100x faster** than reflection-based serializers
- 🔥 **2x faster** with source generators (zero allocations)
- 💾 **40% token reduction** for AI/LLM applications

### Capabilities
- ✅ Full TOON v3.0 specification compliance
- ✅ Expression tree-based serialization (zero reflection)
- ✅ Bidirectional JSON ↔ TOON ↔ YAML conversion
- ✅ AOT-compatible with source generators
- ✅ ASP.NET Core integration with formatters

### Quality
- 🧪 **435+ passing tests**
- 📚 Comprehensive documentation
- 🎯 Production-ready architecture

## 📖 Documentation
- [Main README](https://github.com/selcukgural/ToonNet/blob/v1.0.0/README.md)
- [CHANGELOG](https://github.com/selcukgural/ToonNet/blob/v1.0.0/CHANGELOG.md)
- Package-specific READMEs in each project

## 🎯 Quick Start

```bash
dotnet add package ToonNet.Core
```

```csharp
var person = new Person { Name = "Alice", Age = 30 };
string toon = ToonSerializer.Serialize(person);
var restored = ToonSerializer.Deserialize<Person>(toon);
```

## 📦 Package Files
The following NuGet packages are attached to this release:
- ToonNet.AspNetCore.1.0.0.nupkg (27 KB)
- ToonNet.AspNetCore.Mvc.1.0.0.nupkg (27 KB)
- ToonNet.Core.1.0.0.nupkg (76 KB)
- ToonNet.Extensions.Json.1.0.0.nupkg (26 KB)
- ToonNet.Extensions.Yaml.1.0.0.nupkg (26 KB)
- ToonNet.SourceGenerators.1.0.0.nupkg (42 KB)

## 🙏 Feedback
Please report issues or suggestions on [GitHub Issues](https://github.com/selcukgural/ToonNet/issues).

---

**Full Changelog**: https://github.com/selcukgural/ToonNet/blob/v1.0.0/CHANGELOG.md
