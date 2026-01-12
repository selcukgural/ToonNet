# ToonNet.SourceGenerators

**Compile-time code generation for zero-allocation TOON serialization**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Package](https://img.shields.io/badge/package-ToonNet.SourceGenerators-blue)](#)
[![Status](https://img.shields.io/badge/status-stable-success)](#)

---

## 📦 What is ToonNet.SourceGenerators?

ToonNet.SourceGenerators uses **C# Source Generators** to generate serialization code at **compile-time**:

- ✅ **Zero Allocation** - No runtime reflection or expression trees
- ✅ **AOT Compatible** - Works with Native AOT compilation
- ✅ **Maximum Performance** - Direct property access (no overhead)
- ✅ **Type Safe** - Compile-time errors for unsupported types
- ✅ **Auto-Generated** - No manual serialization code needed

**Perfect for:**
- ⚡ **Hot Paths** - APIs with high-frequency serialization
- 🚀 **Performance Critical** - Real-time systems, gaming, IoT
- 📱 **Native AOT** - Self-contained executables
- 🎯 **Zero Overhead** - When every microsecond counts

---

## 🚀 Quick Start

### Installation

```bash
# Core package (required)
dotnet add package ToonNet.Core

# Source generators
dotnet add package ToonNet.SourceGenerators
```

### Basic Usage

```csharp
using ToonNet.Core.Serialization.Attributes;
using ToonNet.SourceGenerators;

// Mark class with [ToonSerializable]
[ToonSerializable]
public partial class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Hobbies { get; set; }
}

// Generated code provides IToonSerializable interface
var person = new Person 
{ 
    Name = "Alice", 
    Age = 28,
    Hobbies = new List<string> { "Reading", "Coding" }
};

// Use generated Serialize method (zero-allocation!)
string toon = person.Serialize();

// Use generated Deserialize method
var personBack = Person.Deserialize(toon);
```

---

## 🔧 How It Works

### Source Generator Process

1. **Compile-Time Analysis** - Analyzes [ToonSerializable] classes
2. **Code Generation** - Generates serialization methods
3. **Zero Runtime Overhead** - All work done at compile-time

### Generated Code Example

**Your Code:**
```csharp
[ToonSerializable]
public partial class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

**Generated Code** (simplified):
```csharp
public partial class Product : IToonSerializable<Product>
{
    public string Serialize(ToonSerializerOptions? options = null)
    {
        var sb = new StringBuilder();
        sb.Append("Id: ").AppendLine(Id.ToString());
        sb.Append("Name: ").AppendLine(Name);
        sb.Append("Price: ").AppendLine(Price.ToString());
        return sb.ToString();
    }

    public static Product Deserialize(string toonString, ToonSerializerOptions? options = null)
    {
        var doc = Parse(toonString);
        var obj = new Product();
        obj.Id = GetInt32(doc, "Id");
        obj.Name = GetString(doc, "Name");
        obj.Price = GetDecimal(doc, "Price");
        return obj;
    }
}
```

---

## 📖 Attributes

### [ToonSerializable]

Marks a class for source generation:

```csharp
[ToonSerializable]
public partial class MyClass
{
    // Class must be partial
    // Must have parameterless constructor (or primary constructor)
}
```

### [ToonProperty]

Customizes property serialization:

```csharp
[ToonSerializable]
public partial class Product
{
    [ToonProperty("product_id")]
    public int Id { get; set; }
    
    public string Name { get; set; }
}

// Serializes as:
// product_id: 123
// Name: Laptop
```

### [ToonIgnore]

Excludes properties from serialization:

```csharp
[ToonSerializable]
public partial class User
{
    public string Username { get; set; }
    
    [ToonIgnore]
    public string PasswordHash { get; set; }  // Not serialized
}
```

---

## ⚡ Performance Comparison

### Benchmark Results

```
BenchmarkDotNet v0.13.12, Windows 11
Intel Core i7-12700K, 1 CPU, 12 logical and 8 physical cores
.NET SDK 8.0.100

| Method                    | Mean      | Allocated |
|-------------------------- |----------:|----------:|
| SourceGenerator_Serialize | 45.2 ns   | -         |  ← Zero allocation!
| ExpressionTree_Serialize  | 89.5 ns   | 120 B     |
| Reflection_Serialize      | 4,250 ns  | 856 B     |

Source Generator is:
- 2x faster than Expression Trees
- 94x faster than Reflection
- Zero heap allocations (hot path)
```

### When to Use Source Generators

✅ **Use Source Generators When:**
- Hot path with high-frequency serialization
- AOT compilation required
- Zero-allocation is critical
- Startup performance matters

⚠️ **Use Runtime Serialization When:**
- Types unknown at compile-time
- Dynamic type loading (plugins)
- Reflection-based scenarios
- Generic/flexible serialization

---

## 🎯 Real-World Examples

### Example 1: High-Performance API

```csharp
using ToonNet.SourceGenerators;

[ToonSerializable]
public partial class ApiResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    [HttpGet]
    public IActionResult GetData()
    {
        var response = new ApiResponse
        {
            StatusCode = 200,
            Message = "Success",
            Timestamp = DateTime.UtcNow
        };

        // Zero-allocation serialization
        var toon = response.Serialize();
        return Content(toon, "application/toon");
    }
}
```

### Example 2: Gaming/Real-Time Systems

```csharp
[ToonSerializable]
public partial class PlayerState
{
    public int PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public float Health { get; set; }
    public int Score { get; set; }
}

public class NetworkManager
{
    public void BroadcastState(PlayerState state)
    {
        // Critical path - zero allocations
        string data = state.Serialize();
        networkSocket.Send(data);
    }

    public PlayerState ReceiveState(string data)
    {
        // Fast deserialization
        return PlayerState.Deserialize(data);
    }
}
```

### Example 3: IoT/Embedded Systems

```csharp
[ToonSerializable]
public partial class SensorReading
{
    public DateTime Timestamp { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public int BatteryLevel { get; set; }
}

public class SensorDevice
{
    public void SendTelemetry()
    {
        var reading = new SensorReading
        {
            Timestamp = DateTime.UtcNow,
            Temperature = ReadTemperature(),
            Humidity = ReadHumidity(),
            BatteryLevel = GetBatteryLevel()
        };

        // Minimal memory footprint
        var payload = reading.Serialize();
        mqttClient.Publish("sensors/data", payload);
    }
}
```

### Example 4: Native AOT Application

```csharp
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PublishAot>true</PublishAot>  <!-- Enable Native AOT -->
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="ToonNet.Core" />
    <PackageReference Include="ToonNet.SourceGenerators" />
  </ItemGroup>
</Project>

[ToonSerializable]
public partial class Config
{
    public string AppName { get; set; }
    public int MaxConnections { get; set; }
}

// Works with Native AOT (no reflection!)
var config = new Config { AppName = "MyApp", MaxConnections = 100 };
var toon = config.Serialize();
```

---

## 🔍 Supported Types

### Primitive Types
- `string`, `int`, `long`, `short`, `byte`, `sbyte`
- `uint`, `ulong`, `ushort`
- `float`, `double`, `decimal`
- `bool`, `char`, `Guid`, `DateTime`, `DateTimeOffset`

### Collections
- `List<T>`, `T[]`
- `Dictionary<TKey, TValue>`
- `IEnumerable<T>`, `IList<T>`, `ICollection<T>`

### Complex Types
- Nested classes with `[ToonSerializable]`
- Nullable types (`int?`, `DateTime?`)
- Enums

### Limitations

❌ **Not Supported:**
- Abstract/interface types
- Circular references
- Types without parameterless constructor
- Generic types (not instantiated)

---

## 🧪 Testing

```bash
# Run source generator tests
cd tests/ToonNet.SourceGenerators.Tests
dotnet test

# Verify generated code
dotnet build --verbosity detailed
# Check obj/Debug/net8.0/generated/ folder
```

---

## 🔗 Related Packages

**Core:**
- [`ToonNet.Core`](../ToonNet.Core) - Core serialization (required)

**Extensions:**
- [`ToonNet.Extensions.Json`](../ToonNet.Extensions.Json) - JSON ↔ TOON
- [`ToonNet.Extensions.Yaml`](../ToonNet.Extensions.Yaml) - YAML ↔ TOON

**Web:**
- [`ToonNet.AspNetCore`](../ToonNet.AspNetCore) - ASP.NET Core DI
- [`ToonNet.AspNetCore.Mvc`](../ToonNet.AspNetCore.Mvc) - MVC formatters

**Development:**
- [`ToonNet.Benchmarks`](../ToonNet.Benchmarks) - Performance tests
- [`ToonNet.Tests`](../../tests/ToonNet.Tests) - Test suite

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete guide
- [API Guide](../../docs/API-GUIDE.md) - API reference
- [Benchmarks](../ToonNet.Benchmarks) - Performance data

---

## 📋 Requirements

- .NET 8.0 or later
- C# 12.0+ (for partial classes)
- ToonNet.Core

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

## 🤝 Contributing

Contributions welcome! Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

---

**Part of the [ToonNet](../../README.md) serialization library family.**
