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

Generated code converts these property types itself, without reflection:

- `string`, `bool`, all built-in numeric types, `char`, enums, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`,
  `TimeSpan`, `Guid`, `Uri`, `Half`, `Int128`, `UInt128`, `BigInteger` (and their nullable forms);
- other `[ToonSerializable]` types (their generated methods are called directly);
- `T[]`, `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`,
  `HashSet<T>`, `ISet<T>`, `IReadOnlySet<T>`, and `Dictionary<K,V>`, `IDictionary<K,V>`, `IReadOnlyDictionary<K,V>` with
  primitive keys, when their elements are supported too (nesting is fine, e.g. `Dictionary<Guid, List<int>[]>`);
- `ToonValue` properties.

Everything else is handed to the reflection-based `ToonSerializer`, and the generator reports each such property as
warning **`TOON006`** with the reason: `object`, interfaces, abstract types and type parameters (the runtime type is
unknown), classes without `[ToonSerializable]`, other collection types (`ImmutableArray`, `SortedDictionary`, ...).
Set `[ToonSerializable(AllowReflectionFallback = false)]` to make these errors (`TOON007`).

At runtime the generated methods defer to `ToonSerializer` when the options contain converters, and when a property
holds an instance of a type derived from the declared `[ToonSerializable]` class (`ToonSerializer` writes the runtime
type). The output is the same either way.

The library itself is not yet annotated for trimming or Native AOT, so the generator makes no AOT guarantee, and it
makes no zero-allocation promise.

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
- A circular reference is reported as "maximum depth exceeded" (`ToonEncodingException`), where `ToonSerializer`
  reports "circular reference" (also a `ToonEncodingException`).

---

## 📖 Attributes

All attributes live in `ToonNet.Core.Serialization.Attributes`.

```csharp
[ToonSerializable(
    NamingPolicy = PropertyNamingPolicy.CamelCase, // fixed naming; omit to use options.PropertyNamingPolicy at runtime
    GeneratePublicMethods = true,                  // false = internal methods
    IncludeNullChecks = true,                      // ArgumentNullException for a null argument
    IncludeDocumentation = true,                   // XML docs on the generated methods
    AllowReflectionFallback = true)]               // false turns TOON006 warnings into TOON007 errors
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
| `TOON006` | Warning | A property (or constructor parameter) is converted with reflection; the message says why |
| `TOON007` | Error | Same as `TOON006`, on a type with `AllowReflectionFallback = false` |

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
