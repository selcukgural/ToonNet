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

:::note
ToonNet is not yet annotated for trimming or Native AOT, so the generator makes no AOT guarantee, and it does not make
serialization allocation-free.
:::

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

- .NET 8.0 or later
- ToonNet.Core of the same major version

## See Also

- **[Serialization](../core-features/serialization)**: the reflection-based serializer
- **[Custom Converters](custom-converters)**: converters work with generated code too
