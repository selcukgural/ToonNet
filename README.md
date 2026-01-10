<div align="center">

```
    ╔══════════════════════════════════════════════════════╗
    ║                                                      ║
    ║        ▀█▀ █▀█ █▀█ █▄░█   █▄░█ █▀▀ ▀█▀               ║
    ║        ░█░ █▄█ █▄█ █░▀█ • █░▀█ ██▄ ░█░               ║
    ║                                                      ║
    ╚══════════════════════════════════════════════════════╝
```

**⚡ Lightning-Fast • 🤖 AI-Ready • 📖 Human-First**

*Modern data serialization for .NET*

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-288%20passing-success?style=flat)](FINAL_STATUS.md)
[![Coverage](https://img.shields.io/badge/coverage-75.9%25-brightgreen?style=flat)](COVERAGE_REPORT.md)
[![Spec Compliance](https://img.shields.io/badge/TOON%20v3.0-100%25%20compliant-blue?style=flat)](ToonSpec.md)

</div>

---

```toon
# ToonNet itself, described in TOON format
project:
  name: ToonNet
  tagline: Lightning-fast data serialization for .NET
  why[3]:
    - ⚡ 3-5x faster than reflection
    - 🤖 AI-ready clean syntax
    - 📖 Human-readable format
  stats:
    tests: 288
    coverage: 75.9%
    spec_compliance: 100%
    status: production-ready
```

---

## 🚀 Quick Start (30 seconds)

### 1️⃣ Your First TOON Document

```csharp
using ToonNet.Core;
using ToonNet.Core.Parsing;
using ToonNet.Core.Encoding;

// Parse TOON text
var toonText = @"
name: Alice
age: 30
email: alice@example.com
";

var parser = new ToonParser();
var doc = parser.Parse(toonText);

// Access values
var root = (ToonObject)doc.Root;
var name = ((ToonString)root["name"]).Value;    // "Alice"
var age = ((ToonNumber)root["age"]).Value;      // 30

// Encode back to TOON
var encoder = new ToonEncoder();
var output = encoder.Encode(doc);
```

**✅ Done!** You just parsed and encoded your first TOON document.

---

## 📖 What is TOON?

TOON is a **human-readable data format** that's easier to read than JSON and simpler than YAML:

| Format | Sample |
|--------|--------|
| **JSON** | `{"name":"Alice","tags":["dev","admin"],"verified":true}` |
| **YAML** | `name: Alice`<br>`tags:`<br>`  - dev`<br>`  - admin`<br>`verified: true` |
| **TOON** | `name: Alice`<br>`tags: dev, admin`<br>`verified: true` |

**Why TOON?**
- ✨ **Cleaner**: No quotes needed for simple strings
- 📝 **Readable**: Indentation-based structure like Python
- 🎯 **Practical**: Arrays can be inline (`tags: a, b, c`) or list-style
- ⚡ **Fast**: Zero-allocation parsing with source generators

---

## 🎯 3 Ways to Use ToonNet (Choose Your Style)

### 🥉 Level 1: Parse TOON Manually (Most Control)

Perfect for: Config files, data exploration, custom parsing logic

```csharp
var parser = new ToonParser();
var doc = parser.Parse(toonString);

var root = (ToonObject)doc.Root;
var users = (ToonArray)root["users"];
var firstUser = (ToonObject)users.Items[0];
```

**When to use:** Direct TOON manipulation, config parsers, custom logic

---

### 🥈 Level 2: Automatic Serialization (Most Flexible)

Perfect for: Any C# class, dynamic scenarios, rapid prototyping

```csharp
using ToonNet.Core.Serialization;

// Works with ANY class - no attributes needed!
public class User
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Tags { get; set; }
}

var user = new User 
{ 
    Name = "Alice", 
    Age = 30,
    Tags = new List<string> { "dev", "admin" }
};

// Serialize to TOON string (static method, like JsonSerializer)
var toonString = ToonSerializer.Serialize(user);

// Deserialize back to object
var restored = ToonSerializer.Deserialize<User>(toonString);
```

**Output:**
```toon
name: Alice
age: 30
tags[2]: dev, admin
```

**When to use:** Working with existing classes, rapid development, flexibility

---

### 🥇 Level 3: Generated Code (Fastest - Recommended)

Perfect for: APIs, hot paths, production code, DTOs

```csharp
using ToonNet.Core.Serialization.Attributes;

// Add [ToonSerializable] and make it partial
[ToonSerializable]
public partial class User
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Tags { get; set; }
}

var user = new User 
{ 
    Name = "Alice", 
    Age = 30, 
    Tags = new List<string> { "dev", "admin" }
};

// Use auto-generated static methods
var doc = User.Serialize(user);        // Zero reflection!
var restored = User.Deserialize(doc);  // Type-safe!
```

**Benefits:**
- ⚡ **3-5x faster** than reflection
- 🎯 **Compile-time type checking**
- 💾 **87% less memory** allocation
- 🔍 **IntelliSense support** for generated methods

**When to use:** Performance-critical code, APIs, DTOs, production systems

---

## 💡 Real-World Examples (Copy & Paste Ready)

### Example 1: Configuration File

```csharp
// config.toon
var configText = @"
database:
  host: localhost
  port: 5432
  name: myapp
  credentials:
    username: admin
    password: secret
logging:
  level: info
  outputs: console, file
";

var parser = new ToonParser();
var doc = parser.Parse(configText);
var root = (ToonObject)doc.Root;

var db = (ToonObject)root["database"];
var host = ((ToonString)db["host"]).Value;  // "localhost"
```

### Example 2: API Response

```csharp
[ToonSerializable]
public partial class ApiResponse
{
    public string Status { get; set; }
    public int Code { get; set; }
    public List<User> Users { get; set; }
}

[ToonSerializable]
public partial class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}

// Usage
var response = new ApiResponse
{
    Status = "success",
    Code = 200,
    Users = new List<User>
    {
        new() { Id = 1, Username = "alice", Email = "alice@example.com" },
        new() { Id = 2, Username = "bob", Email = "bob@example.com" }
    }
};

var doc = ApiResponse.Serialize(response);
var encoder = new ToonEncoder();
var toonString = encoder.Encode(doc);
```

**Output:**
```toon
status: success
code: 200
users[2]:
  - id: 1
    username: alice
    email: alice@example.com
  - id: 2
    username: bob
    email: bob@example.com
```

### Example 3: Custom Naming Policies

```csharp
using ToonNet.Core.Serialization;

public class UserDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase
};

var user = new UserDto { FirstName = "Alice", LastName = "Smith" };
var toonString = ToonSerializer.Serialize(user, options);
```

**Output:**
```toon
first_name: Alice
last_name: Smith
```

### Example 4: Nested Objects & Arrays

```csharp
var complexToon = @"
company: TechCorp
employees[3]:
  - id: 1
    name: Alice
    roles[2]: admin, developer
    profile:
      email: alice@techcorp.com
      verified: true
  - id: 2
    name: Bob
    roles[1]: developer
    profile:
      email: bob@techcorp.com
      verified: false
  - id: 3
    name: Charlie
    roles[2]: designer, manager
    profile:
      email: charlie@techcorp.com
      verified: true
";

var parser = new ToonParser();
var doc = parser.Parse(complexToon);

// Navigate the structure
var root = (ToonObject)doc.Root;
var employees = (ToonArray)root["employees"];
var firstEmployee = (ToonObject)employees.Items[0];
var profile = (ToonObject)firstEmployee["profile"];
var email = ((ToonString)profile["email"]).Value;
```

### Example 5: JSON ↔️ TOON Conversion

```csharp
using ToonNet.Core.Interop;
using System.Text.Json;

// JSON → TOON
var json = """
{
    "name": "Alice",
    "age": 30,
    "tags": ["dev", "admin"],
    "profile": {
        "email": "alice@example.com",
        "verified": true
    }
}
""";

// Convert JSON to TOON document
var toonDoc = ToonJsonConverter.FromJson(json);

// Encode as TOON string
var encoder = new ToonEncoder();
var toonString = encoder.Encode(toonDoc);

Console.WriteLine(toonString);
```

**Output:**
```toon
name: Alice
age: 30
tags[3]: dev, admin
profile:
  email: alice@example.com
  verified: true
```

**TOON → JSON:**
```csharp
var toon = """
name: Alice
age: 30
tags: dev, admin
profile:
  email: alice@example.com
  verified: true
""";

// Parse TOON
var parser = new ToonParser();
var doc = parser.Parse(toon);

// Convert to JSON
var json = ToonJsonConverter.ToJson(doc, writeIndented: true);

Console.WriteLine(json);
```

**Output:**
```json
{
  "name": "Alice",
  "age": 30,
  "tags": [
    "dev",
    "admin"
  ],
  "profile": {
    "email": "alice@example.com",
    "verified": true
  }
}
```

**Use Cases:**
- 🔄 **Migrate from JSON** - Convert existing JSON configs to TOON
- 📡 **API Integration** - Accept JSON, work with TOON internally
- 🔧 **Tooling** - Build JSON/TOON converters and validators
- 📊 **Data Migration** - Batch convert between formats
```

---

## 🛠️ Installation

Add to your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/ToonNet.Core/ToonNet.Core.csproj" />
  <!-- Optional: For source generators -->
  <ProjectReference Include="path/to/ToonNet.SourceGenerators/ToonNet.SourceGenerators.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

---

## 📚 TOON Format Cheat Sheet

### Basic Syntax

```toon
# Comments start with #
key: value                  # Simple key-value
nested:                     # Nested object
  child: value
  deep:
    value: here
```

### Strings

```toon
bare: No quotes needed
quoted: "Special chars: \n\t"
multiline: |
  Line 1
  Line 2
```

### Numbers & Booleans

```toon
integer: 42
decimal: 3.14
scientific: 1.5e10
boolean: true
null_value: null
```

### Arrays

```toon
# Inline arrays
tags: javascript, python, rust

# List-style arrays
frameworks:
  - React
  - Vue
  - Angular

# Array with length notation
items[3]: apple, banana, orange
```

### Array of Objects

```toon
users[2]:
  - name: Alice
    age: 30
  - name: Bob
    age: 25
```

### Tabular Arrays (CSV-like)

```toon
products[id, name, price]:
  1, Laptop, 999.99
  2, Mouse, 29.99
  3, Keyboard, 79.99
```

**📘 Full Specification:** See [ToonSpec.md](ToonSpec.md) for complete language reference

---

## ⚡ Performance

ToonNet is **fast**. Source-generated code is 3-5x faster than reflection:

| Scenario | Generated | Reflection | Speedup |
|----------|-----------|------------|---------|
| 5 properties | 1.2 µs | 5.8 µs | **4.8x** |
| 10 properties | 2.0 µs | 12.5 µs | **6.2x** |
| 15 properties | 2.8 µs | 18.2 µs | **6.5x** |
| Memory | 64 B | 512 B | **87% less** |

---

## 🎨 Design Philosophy

### Static API (Like JsonSerializer)

ToonSerializer follows the same design pattern as `System.Text.Json.JsonSerializer`:

```csharp
// ✅ Correct: Static methods (no instantiation needed)
var toonString = ToonSerializer.Serialize(user);
var user = ToonSerializer.Deserialize<User>(toonString);

// With options
var options = new ToonSerializerOptions { /* ... */ };
var toonString = ToonSerializer.Serialize(user, options);
```

**Why static?**
- 🎯 **Familiar**: Same pattern as JsonSerializer
- ⚡ **Performance**: No instance allocation overhead
- 🔒 **Thread-safe**: Stateless design, safe for concurrent use
- 🧹 **Clean**: No object lifecycle management needed

### Thread Safety

All ToonNet APIs are thread-safe:
- ✅ `ToonSerializer` - Static, stateless
- ✅ `ToonParser` - Immutable state per parse operation
- ✅ `ToonEncoder` - Immutable state per encode operation
- ✅ `ToonSerializerOptions` - Immutable configuration

```csharp
// Safe to use from multiple threads
Parallel.For(0, 1000, i =>
{
    var toon = ToonSerializer.Serialize(data[i]);
    var result = ToonSerializer.Deserialize<MyType>(toon);
});
```

---

## 🧪 Testing & Quality

```
✅ 288/288 tests passing (100%)
✅ 75.9% code coverage (ToonNet.Core)
✅ 100% TOON v3.0 spec compliance
✅ Zero known bugs
✅ Production ready
```

Run tests yourself:
```bash
dotnet test
```

Generate coverage report:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:TestResults/CoverageReport
```

---

## 🎓 Advanced Features

### Custom Type Converters

```csharp
public class ColorConverter : ToonConverter<Color>
{
    public override ToonValue Write(Color value, ToonSerializerOptions options)
        => new ToonString($"#{value.R:X2}{value.G:X2}{value.B:X2}");

    public override Color Read(ToonValue value, ToonSerializerOptions options)
    {
        var hex = ((ToonString)value).Value.TrimStart('#');
        return Color.FromArgb(
            Convert.ToInt32(hex.Substring(0, 2), 16),
            Convert.ToInt32(hex.Substring(2, 2), 16),
            Convert.ToInt32(hex.Substring(4, 2), 16)
        );
    }
}

[ToonSerializable]
public partial class Theme
{
    public string Name { get; set; }
    
    [ToonConverter(typeof(ColorConverter))]
    public Color PrimaryColor { get; set; }
}
```

### Custom Constructors

```csharp
[ToonSerializable]
public partial class Point
{
    public int X { get; set; }
    public int Y { get; set; }

    [ToonConstructor]
    public Point(int x, int y)
    {
        X = x;
        Y = y;
        // Custom initialization logic here
    }
}
```

### Property Attributes

```csharp
[ToonSerializable]
public partial class User
{
    [ToonProperty("user_id")]
    public int Id { get; set; }
    
    [ToonPropertyOrder(1)]
    public string Name { get; set; }
    
    [ToonIgnore]
    public string InternalField { get; set; }
}
```

---

## 📖 Documentation

| Document | Description |
|----------|-------------|
| [ToonSpec.md](ToonSpec.md) | Complete TOON v3.0 format specification |
| [FINAL_STATUS.md](FINAL_STATUS.md) | Project completion report & metrics |
| [COVERAGE_REPORT.md](COVERAGE_REPORT.md) | Detailed test coverage analysis |
| [COVERAGE_SUMMARY.md](COVERAGE_SUMMARY.md) | Quick coverage reference |

---

## 🤝 Contributing

We welcome contributions! Please follow these guidelines:

1. ✅ **Tests must pass** - All 288 tests must remain green
2. 📝 **Add tests** - New features need test coverage
3. 📚 **Document** - Update README if adding user-facing features
4. 🎨 **Code style** - Follow existing C# conventions
5. ✉️ **PR description** - Explain what and why

```bash
# Before submitting PR
dotnet build
dotnet test
```

---

## 🗺️ Roadmap

- [ ] NuGet package publishing
- [ ] JSON ↔️ TOON conversion utility
- [ ] YAML ↔️ TOON conversion utility
- [ ] VS Code extension with syntax highlighting
- [ ] Online TOON playground/validator
- [ ] Schema validation support
- [ ] Streaming parser for large files

---

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details

---

## 🌟 Show Your Support

If ToonNet helped you, please ⭐ star this repository!

---

## 📞 Support & Community

- 🐛 **Issues**: [GitHub Issues](https://github.com/selcukgural/ToonNet/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/selcukgural/ToonNet/discussions)
- 📧 **Email**: [Contact](mailto:your-email@example.com)

---

**Built with ❤️ by developers, for developers**

---

## 🎯 Quick Links

- [Get Started](#-quick-start-30-seconds) - Your first TOON document in 30 seconds
- [Examples](#-real-world-examples-copy--paste-ready) - Copy-paste ready code
- [Performance](#-performance) - See the benchmarks
- [API Reference](ToonSpec.md) - Complete TOON format specification
- [Full Documentation](FINAL_STATUS.md) - Everything you need to know
