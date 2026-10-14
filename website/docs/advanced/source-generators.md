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
- `[ToonProperty]`, `[ToonConverter]`, and the `IgnoreNullValues`, `IncludeReadOnlyProperties`, `PropertyNamingPolicy`
  and `Converters` options.

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

At runtime the generated methods defer to `ToonSerializer` when the options contain converters, or when a property
holds an instance of a class derived from its declared `[ToonSerializable]` type. The output is the same either way.

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

`ToonNet.Core` is annotated for trimming and NativeAOT. `ToonSerializer` converts types with reflection, so its methods
carry `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` and a trimmed or AOT app gets a warning where it calls
them. The generated methods need no reflection: write and read `[ToonSerializable]` types with them, `ToonEncoder` and
`ToonDocument.Parse`:

```csharp
[ToonSerializable(AllowReflectionFallback = false)] // every property must be converted by generated code
public partial class Catalog
{
    public string Name { get; set; } = "";
    public List<Product> Products { get; set; } = [];
    public Dictionary<string, Warehouse> Warehouses { get; set; } = [];
}

string toon = new ToonEncoder().Encode(Catalog.Serialize(catalog));
Catalog copy = Catalog.Deserialize(ToonDocument.Parse(toon));

// The same with UTF-8 bytes, without intermediate strings
byte[] utf8 = new ToonEncoder().EncodeToUtf8Bytes(Catalog.Serialize(catalog));
Catalog fromBytes = Catalog.Deserialize(ToonDocument.Parse(utf8));
```

`ToonEncoder.Encode(document, IBufferWriter<byte>)` writes the UTF-8 to a buffer writer such as a `PipeWriter`.

This holds as long as the options contain no converters and objects are of their declared types (a derived instance
is handed to the reflection-based serializer). The repository's CI publishes such an app with NativeAOT and fails on
any trim or AOT warning.

## Supported shapes

| Shape | Support |
|-------|---------|
| Classes, structs, records, record structs | ✅ |
| Positional records and constructor parameters | ✅ parameter defaults are used for missing keys |
| `init`-only and `required` properties | ✅ |
| Private setters (also in base classes) | ✅ |
| Nested types, generic types, the global namespace | ✅ containing types must be `partial` too |
| Abstract types | `Serialize` only |

Known differences from `ToonSerializer`:

- A non-`required` `init` property of a **generic** type is set through the object initializer, so a missing key resets
  it to `default` instead of keeping its initializer value.
- A `required` property whose key is missing from the document is set to `default`.
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
- ToonNet.Core of the same release

## See Also

- **[Serialization](../core-features/serialization)**: the reflection-based serializer
- **[Custom Converters](custom-converters)**: converters work with generated code too
