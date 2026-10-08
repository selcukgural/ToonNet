# ToonNet.SourceGenerators

**Roslyn source generator that adds `Serialize` / `Deserialize` methods to your TOON types**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/ToonNet.SourceGenerators.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.SourceGenerators/)
[![Downloads](https://img.shields.io/nuget/dt/ToonNet.SourceGenerators.svg?style=flat)](https://www.nuget.org/packages/ToonNet.SourceGenerators/)

---

## 📦 What does it do?

For every `partial` class, struct or record marked with `[ToonSerializable]`, the generator adds two static methods:

```csharp
public static ToonDocument Serialize(T value, ToonSerializerOptions? options = null);
public static T Deserialize(ToonDocument doc, ToonSerializerOptions? options = null);
```

The generated code follows the same rules as the reflection-based `ToonSerializer` and produces the same TOON:
the same properties in the same order, the same constructor selection, `[ToonProperty]`, `[ToonIgnore]`,
`[ToonPropertyOrder]`, `[ToonConverter]`, and the `IgnoreNullValues`, `IncludeReadOnlyProperties`,
`PropertyNamingPolicy` and `Converters` options.

**What is generated and what is not:**

- Properties of type `string`, `bool` and the built-in numeric types are read and written directly, without reflection
  (unless converters are registered in the options).
- Every other property type (collections, dictionaries, enums, dates, `Guid`, nested objects) is handed to
  `ToonSerializer.SerializeToValue` / `DeserializeFromValue`, which use reflection. The generator is therefore **not** a
  Native AOT or trimming solution, and it makes no zero-allocation promise.

---

## 🚀 Quick Start

### Installation

The generated code calls into `ToonNet.Core`, so install both packages:

```bash
dotnet add package ToonNet.Core
dotnet add package ToonNet.SourceGenerators
```

`ToonNet.SourceGenerators` is a development dependency: it only runs at compile time and adds nothing to your output.

### Usage

```csharp
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization.Attributes;

[ToonSerializable]
public partial record Order(string Id, List<Line> Lines)
{
    public string Note { get; init; } = "";
}

public record Line(string Sku, int Qty);

// Object -> TOON
var order = new Order("A-1", [new("pen", 2), new("ink", 1)]) { Note = "rush" };
string toon = new ToonEncoder().Encode(Order.Serialize(order));
// Id: A-1
// Lines[2]{Sku,Qty}:
//   pen,2
//   ink,1
// Note: rush

// TOON -> object
Order copy = Order.Deserialize(ToonDocument.Parse(toon));
```

---

## 🔧 Supported shapes

| Shape | Support |
|-------|---------|
| Classes, structs, records, record structs | ✅ |
| Positional records and constructor parameters | ✅ bound by name (case-insensitive), parameter defaults used for missing keys |
| `init`-only and `required` properties | ✅ |
| Properties with private setters (also in base classes) | ✅ |
| Inherited properties | ✅ base-class properties first, like `ToonSerializer` |
| Nested types, generic types, the global namespace | ✅ containing types must be `partial` too |
| Abstract types | `Serialize` only |

**Constructor selection** (same as `ToonSerializer`): a constructor marked `[ToonConstructor]`, otherwise the public
parameterless constructor, otherwise the public constructor with the most parameters.

**Known differences from `ToonSerializer`:**

- A non-`required` `init` property of a **generic** type is set through the object initializer, so a key missing from
  the document resets it to `default` instead of keeping its initializer value. Non-generic types keep the initializer
  value (the setter is called through `[UnsafeAccessor]`).
- A `required` property whose key is missing from the document is set to `default`.
- Converters registered in the options for the declaring type itself are not consulted (they are for its properties).

---

## 📖 Attributes

All attributes live in `ToonNet.Core.Serialization.Attributes`.

```csharp
[ToonSerializable(
    NamingPolicy = PropertyNamingPolicy.CamelCase, // fixed naming; omit to use options.PropertyNamingPolicy at runtime
    GeneratePublicMethods = true,                  // false = internal methods
    IncludeNullChecks = true,                      // ArgumentNullException for a null argument
    IncludeDocumentation = true)]                  // XML docs on the generated methods
public partial class User
{
    [ToonPropertyOrder(-1)]
    public int Id { get; set; }

    [ToonProperty("display_name")]
    public string Name { get; set; } = "";

    [ToonIgnore]
    public string PasswordHash { get; set; } = "";

    [ToonConverter(typeof(UnixTimeConverter))]
    public DateTimeOffset LastSeen { get; set; }
}
```

---

## ⚠️ Diagnostics

| Id | Severity | Meaning |
|----|----------|---------|
| `TOON001` | Error | Code generation failed unexpectedly (please report it) |
| `TOON002` | Error | The type, or a type it is nested in, is not `partial` |
| `TOON003` | Warning | The type has no public properties to serialize |
| `TOON005` | Warning | The type has no public constructor; only `Serialize` is generated |

---

## 🔍 Viewing the generated code

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

The files are written to `obj/<configuration>/<tfm>/generated/ToonNet.SourceGenerators/`. In Visual Studio and Rider
they are also listed under **Dependencies → Analyzers → ToonNet.SourceGenerators**.

---

## 📋 Requirements

- .NET 8.0 or later (the generated code uses `[UnsafeAccessor]` for `init`-only and private setters)
- ToonNet.Core of the same major version

---

## 📚 Links

- [Documentation](https://selcukgural.github.io/ToonNet/docs/advanced/source-generators)
- [ToonNet on GitHub](https://github.com/selcukgural/ToonNet)
- [ToonNet.Core on NuGet](https://www.nuget.org/packages/ToonNet.Core/)

MIT License.
