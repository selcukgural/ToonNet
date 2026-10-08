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

`string`, `bool` and the built-in numeric properties are read and written directly. All other property types
(collections, dictionaries, enums, dates, `Guid`, nested objects) are handed to `ToonSerializer.SerializeToValue` and
`ToonSerializer.DeserializeFromValue`, which use reflection.

:::note
The generator is not a Native AOT or trimming solution, and it does not make serialization allocation-free. Use it when
you want static, discoverable `Serialize`/`Deserialize` methods with the same behavior as `ToonSerializer`.
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

## Attribute options

```csharp
[ToonSerializable(
    NamingPolicy = PropertyNamingPolicy.CamelCase, // fixed; omit to follow options.PropertyNamingPolicy at runtime
    GeneratePublicMethods = true,                  // false generates internal methods
    IncludeNullChecks = true,                      // ArgumentNullException for a null argument
    IncludeDocumentation = true)]                  // XML docs on the generated methods
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
