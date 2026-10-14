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
the same properties in the same order, the same constructor selection, the same setters for reading (public ones, and
non-public ones only with `[ToonInclude]`), writing with declared types, `[ToonProperty]`, `[ToonIgnore]`,
`[ToonInclude]`, `[ToonPropertyOrder]`, `[ToonConverter]`, and the `IgnoreNullValues`, `IncludeReadOnlyProperties`,
`PropertyNamingPolicy`, `PropertyNameCaseInsensitive`, `UnmappedMemberHandling`, `RespectRequiredMembers`,
`SerializeRuntimeTypes` and `Converters` options.

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

At runtime the generated methods apply the converters of the options to the values they convert. An instance of a
type derived from a property's declared `[ToonSerializable]` class is written as the declared class, as
`ToonSerializer` writes it; with `SerializeRuntimeTypes = true` the generated methods defer to `ToonSerializer`, which
writes the runtime type. The output is the same either way. Regenerate code with the 4.0 generator: older generated
writers do not report their properties' types.

`ToonSerializer` uses the generated methods as well: each generated type registers them from a module initializer, so
`ToonSerializer.Serialize`/`Deserialize<T>` and everything built on them (the ASP.NET Core formatters, `ToonResult`,
the JSON/YAML helpers) convert the type without reflection. This applies to non-generic types that are reachable from
their assembly; generic types and `private` or `protected` nested types are still converted with reflection. It needs
`ToonNet.Core` and `ToonNet.SourceGenerators` of the same release.

Each type also gets a field writer and a text reader that `ToonSerializer` uses to write and read its objects straight
from TOON text, without building `ToonValue` objects: keys are encoded and matched at compile time, primitives and
enum names are written without boxing, and values are read into typed locals. The static `Serialize` and
`Deserialize` methods still work with a `ToonDocument`.

Each type also gets a static `ToonTypeInfo` property for the `ToonSerializer` overloads that need no reflection
(trimming and NativeAOT); see below.

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

[ToonSerializable]
public partial record Line(string Sku, int Qty);

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
| Properties with non-public setters marked `[ToonInclude]` (also in base classes) | ✅ (without it, only public setters are used) |
| Inherited properties | ✅ base-class properties first, like `ToonSerializer` |
| Nested types, generic types, the global namespace | ✅ containing types must be `partial` too |
| Abstract types | `Serialize` only |

**Constructor selection** (same as `ToonSerializer`): a constructor marked `[ToonConstructor]`, otherwise the public
parameterless constructor, otherwise the public constructor with the most parameters.

**Known differences from `ToonSerializer`:**

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
| `TOON008` | Error | A `[ToonProperty]` name contains an unpaired surrogate, which TOON cannot represent |
| `TOON009` | Warning | A member of the type is named `Serialize` or `Deserialize` (a property, field or nested type, or a method with the generated method's parameters), so that generated static method is left out; use `ToonSerializer` with `ToonTypeInfo` instead |
| `TOON010` | Error | The type is a static class, a ref struct or a file-local type, which generated code cannot serialize |
| `TOON011` | Error | The project references a `ToonNet.Core` of an older or another major version than the generator; no code is generated until both packages have the same version |

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

## 🪶 NativeAOT and trimming

Pass a type's `ToonTypeInfo` to `ToonSerializer`: those overloads write and read straight from text through generated
code, with the same output and speed as the others, but without reflection, so the app can be trimmed and published
with NativeAOT (use `AllowReflectionFallback = false` so every property is converted by generated code). The overloads
that take a `Type` use reflection and are annotated with `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`.

```csharp
string toon = ToonSerializer.Serialize(catalog, Catalog.ToonTypeInfo);
Catalog? copy = ToonSerializer.Deserialize(toon, Catalog.ToonTypeInfo);

var catalogs = ToonTypeInfo.CreateList(Catalog.ToonTypeInfo);  // collection roots
```

The generated `Serialize`/`Deserialize` methods with `ToonEncoder` and `ToonDocument.Parse` need no reflection either.
Converters of the options are applied without reflection too.

---

## 📋 Requirements

- .NET 8.0 or later (the generated code uses `[UnsafeAccessor]` for `init`-only setters and `[ToonInclude]` non-public setters)
- C# 10 or later (the default of .NET 6 and later; generated files use file-scoped namespaces)
- ToonNet.Core of the same release (generated code uses its hidden writer and reader APIs)

---

## 📚 Links

- [Documentation](https://selcukgural.github.io/ToonNet/docs/advanced/source-generators)
- [ToonNet on GitHub](https://github.com/selcukgural/ToonNet)
- [ToonNet.Core on NuGet](https://www.nuget.org/packages/ToonNet.Core/)

MIT License.
