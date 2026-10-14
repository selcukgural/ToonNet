# Source Generators

`ToonNet.SourceGenerators` is a Roslyn source generator. For every `partial` class, struct or record marked with
`[ToonSerializable]`, it adds static `Serialize` and `Deserialize` methods to the type.

## Installation

The generated code calls into `ToonNet.Core`, so install both packages:

```bash
dotnet add package ToonNet.Core
dotnet add package ToonNet.SourceGenerators
```

## Usage

```csharp
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization.Attributes;

[ToonSerializable]
public partial class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public List<string> Hobbies { get; set; } = [];
}

var person = new Person { Name = "Alice", Age = 28, Hobbies = ["chess", "running"] };

ToonDocument doc = Person.Serialize(person);
string toon = new ToonEncoder().Encode(doc);
// Name: Alice
// Age: 28
// Hobbies[2]: chess,running

Person copy = Person.Deserialize(ToonDocument.Parse(toon));
```

The generated signatures are:

```csharp
public static ToonDocument Serialize(Person value, ToonSerializerOptions? options = null);
public static Person Deserialize(ToonDocument doc, ToonSerializerOptions? options = null);
```

## Same output as ToonSerializer

The generated code follows the rules of the reflection-based [`ToonSerializer`](../core-features/serialization), so both
produce the same TOON for the same object:

- the same properties (public getter, not an indexer, not `[ToonIgnore]`) in the same order (`[ToonPropertyOrder]`, then
  base-class properties first, then declaration order);
- the same constructor (a `[ToonConstructor]` one, otherwise the public parameterless one, otherwise the public one with
  the most parameters), with parameters bound to keys by property name;
- the same setters for reading (public ones, and non-public ones only with `[ToonInclude]`);
- writing with the declared type of each property, item and dictionary value;
- `[ToonProperty]`, `[ToonConverter]`, `[ToonInclude]`, and the `IgnoreNullValues`, `IncludeReadOnlyProperties`,
  `PropertyNamingPolicy`, `PropertyNameCaseInsensitive`, `UnmappedMemberHandling`, `RespectRequiredMembers`,
  `SerializeRuntimeTypes` and `Converters` options.

### What is generated

Generated code converts these property types itself, without reflection:

- `string`, `bool`, all built-in numeric types, `char`, enums, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`,
  `TimeSpan`, `Guid`, `Uri`, `Half`, `Int128`, `UInt128`, `BigInteger` (and their nullable forms);
- other `[ToonSerializable]` types, whose generated methods are called directly;
- arrays, `List<T>` and the interfaces it implements, `HashSet<T>`/`ISet<T>`/`IReadOnlySet<T>`, and
  `Dictionary<K,V>`/`IDictionary<K,V>`/`IReadOnlyDictionary<K,V>` with primitive keys, when their elements are
  supported (nesting is fine);
- `ToonValue` properties.

Any other property type (`object`, interfaces, abstract types, type parameters, classes without `[ToonSerializable]`,
other collection types) is handed to the reflection-based serializer and reported as warning `TOON006`, with the
reason. Use `[ToonSerializable(AllowReflectionFallback = false)]` to turn these into errors (`TOON007`).

At runtime the generated methods apply the converters of the options to the values they convert. An instance of a
class derived from a property's declared `[ToonSerializable]` type is written as the declared type, as `ToonSerializer`
writes it; with `SerializeRuntimeTypes = true` the generated methods defer to `ToonSerializer`, which writes the runtime
type. The output is the same either way. Code generated before 4.0 does not report its properties' types; regenerate
it with the 4.0 generator.

`ToonSerializer` uses the generated methods as well: each generated type registers them from a module initializer, so
`ToonSerializer.Serialize`/`Deserialize<T>` and everything built on them (the ASP.NET Core formatters, `ToonResult`,
the JSON/YAML helpers) convert the type without reflection. This applies to non-generic types that are reachable from
their assembly; generic types and `private` or `protected` nested types are still converted with reflection. It needs
`ToonNet.Core` and `ToonNet.SourceGenerators` of the same release.

Each type also gets a field writer and a text reader that `ToonSerializer` uses to write and read its objects straight
from TOON text, without building `ToonValue` objects: keys are encoded and matched at compile time, primitives and
enum names are written without boxing, and values are read into typed locals. The static `Serialize` and
`Deserialize` methods still work with a `ToonDocument`.

:::note
The generator does not make serialization allocation-free. For trimmed and NativeAOT apps, see
[NativeAOT and trimming](#nativeaot-and-trimming).
:::

## NativeAOT and trimming

`ToonNet.Core` is annotated for trimming and NativeAOT. The `ToonSerializer` overloads that take a type convert it with
reflection, so they carry `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` and a trimmed or AOT app gets a
warning where it calls them. Every `[ToonSerializable]` type has a static `ToonTypeInfo` property instead; pass it to the
`ToonSerializer` overloads that take a type info, which need no reflection:

```csharp
[ToonSerializable(AllowReflectionFallback = false)] // every property must be converted by generated code
public partial class Catalog
{
    public string Name { get; set; } = "";
    public List<Product> Products { get; set; } = [];
    public Dictionary<string, Warehouse> Warehouses { get; set; } = [];
}

[ToonSerializable] public partial record Product(string Sku, decimal Price);  // property types need generated code too
[ToonSerializable] public partial record Warehouse(string City, int Stock);

var catalog = new Catalog { Name = "Spring", Products = [new("A-1", 9.5m)], Warehouses = { ["ist"] = new("Istanbul", 40) } };

string toon = ToonSerializer.Serialize(catalog, Catalog.ToonTypeInfo);
Catalog? copy = ToonSerializer.Deserialize(toon, Catalog.ToonTypeInfo);

// UTF-8, buffer writers and streams work the same way
byte[] utf8 = ToonSerializer.SerializeToUtf8Bytes(catalog, Catalog.ToonTypeInfo);
Catalog? fromBytes = ToonSerializer.Deserialize(utf8, Catalog.ToonTypeInfo);

// Collection roots: compose the type info of the elements
var catalogs = ToonTypeInfo.CreateList(Catalog.ToonTypeInfo);           // also CreateArray, CreateDictionary
string many = ToonSerializer.Serialize(new List<Catalog> { catalog }, catalogs);
```

These overloads write and read straight from text, like the others and with the same output, at the same speed: on
10,000 records they serialize 2.4–4x and deserialize 1.6–2x faster than going through a `ToonDocument`. A value that
only reflection would handle directly (an object without generated code in a property typed `object`, a derived
instance with `SerializeRuntimeTypes`) is converted alone by the reflection-based serializer, with the same text; the
rest of the document is still written straight. Converters of the options are applied too: values no converter
applies to are still converted straight from text, the others through the generated tree-based methods. `ToonTypeInfo.GetGenerated<T>()` returns the same type info for generic code or a type
whose own member is named `ToonTypeInfo`.

The generated `Serialize`/`Deserialize` methods with `ToonEncoder` and `ToonDocument.Parse` need no reflection either:

```csharp
string toon = new ToonEncoder().Encode(Catalog.Serialize(catalog));
Catalog copy = Catalog.Deserialize(ToonDocument.Parse(toon));
byte[] utf8 = new ToonEncoder().EncodeToUtf8Bytes(Catalog.Serialize(catalog));  // or Encode(document, bufferWriter)
```

This holds as long as objects are of their declared types or `SerializeRuntimeTypes` is off (with it on, a derived
instance is handed to the reflection-based serializer). A document that leaves out a `required` or `init` property with
an initializer (`= true`, `= new()`) is read by the reflection-based reader too, which keeps the initializer. The
repository's CI publishes an app that uses both with NativeAOT and fails on any trim or AOT warning.

The other packages are annotated as well (`IsAotCompatible`). These members use reflection and warn in trimmed and
AOT apps:

| Package | Members with `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` | Without reflection |
|---------|-------------------------------------------------------------------|--------------------|
| ToonNet.Extensions.Json | `ToonConvert.DeserializeFromJson`, `SerializeToJson` (System.Text.Json), `ParseJson` | `FromJson`, `ToJson`, `ToonJsonConverter` |
| ToonNet.Extensions.Yaml | `ToYaml` (`[RequiresDynamicCode]`: YamlDotNet's serializer) | `FromYaml` |
| ToonNet.AspNetCore | – | `AddToonNet`, `AddToonFile` |
| ToonNet.AspNetCore.Mvc | `AddToonFormatters`, the formatters, `ReadFromToonAsync<T>()` and `(Type)`, `ToonResult`, `Results.Extensions.Toon` | `ReadFromToonAsync(typeInfo)`; write responses with `ToonSerializer` and a type info |

## Supported shapes

| Shape | Support |
|-------|---------|
| Classes, structs, records, record structs | ✅ |
| Positional records and constructor parameters | ✅ parameter defaults are used for missing keys |
| `init`-only and `required` properties | ✅ |
| Non-public setters marked `[ToonInclude]` (also in base classes) | ✅ (without it, only public setters are used) |
| Nested types, generic types, the global namespace | ✅ containing types must be `partial` too |
| Abstract types | `Serialize` only |

Known differences from `ToonSerializer`:

- A circular reference is reported as "maximum depth exceeded" instead of "circular reference" (both throw
  `ToonEncodingException`).

## Attribute options

```csharp
[ToonSerializable(
    NamingPolicy = PropertyNamingPolicy.CamelCase, // fixed; omit to follow options.PropertyNamingPolicy at runtime
    GeneratePublicMethods = true,                  // false generates internal methods
    IncludeNullChecks = true,                      // ArgumentNullException for a null argument
    IncludeDocumentation = true,                   // XML docs on the generated methods
    AllowReflectionFallback = true)]               // false turns TOON006 warnings into TOON007 errors
public partial class User
{
    [ToonProperty("display_name")]
    public string Name { get; set; } = "";

    [ToonIgnore]
    public string PasswordHash { get; set; } = "";
}
```

## Diagnostics

| Id | Severity | Meaning |
|----|----------|---------|
| `TOON001` | Error | Code generation failed unexpectedly |
| `TOON002` | Error | The type, or a type it is nested in, is not `partial` |
| `TOON003` | Warning | The type has no public properties to serialize |
| `TOON005` | Warning | The type has no public constructor; only `Serialize` is generated |
| `TOON006` | Warning | A property or constructor parameter is converted with reflection; the message says why |
| `TOON007` | Error | Same as `TOON006`, on a type with `AllowReflectionFallback = false` |
| `TOON008` | Error | A `[ToonProperty]` name contains an unpaired surrogate, which TOON cannot represent |
| `TOON009` | Warning | A member of the type is named `Serialize` or `Deserialize` (a property, field or nested type, or a method with the generated method's parameters), so that generated static method is left out; use `ToonSerializer` with `ToonTypeInfo` instead |
| `TOON010` | Error | The type is a static class, a ref struct or a file-local type, which generated code cannot serialize |
| `TOON011` | Error | The project references a `ToonNet.Core` of an older or another major version than the generator; no code is generated until both packages have the same version |

## Viewing generated code

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

The files are written to `obj/<configuration>/<tfm>/generated/ToonNet.SourceGenerators/`. In Visual Studio and Rider
they are also listed under **Dependencies → Analyzers → ToonNet.SourceGenerators**.

## Requirements

- .NET 8.0 or later (`ToonNet.Core` builds for .NET 8 and .NET 10)
- C# 10 or later (the default of .NET 6 and later; generated files use file-scoped namespaces)
- ToonNet.Core of the same release

## See Also

- **[Serialization](../core-features/serialization)**: the reflection-based serializer
- **[Custom Converters](custom-converters)**: converters work with generated code too
