# Migrating to 4.0

ToonNet 4.0 changes three defaults that let documents and responses do more than the code meant, so they now behave
as System.Text.Json does, and fixes behavior that callers may rely on. This page lists what to check; the
[changelog](https://github.com/selcukgural/ToonNet/blob/master/CHANGELOG.md) has every change.

## Non-Public Setters Are Not Set From Documents

Before 4.0, reading set any setter, `private set` included. Now only `set` and `init` setters are used:

```csharp
public class Order
{
    public decimal Total { get; private set; }   // 3.x: set from the document; 4.0: not set
}
```

If a property with a non-public setter must be read, mark it `[ToonInclude]`:

```csharp
[ToonInclude]
public decimal Total { get; private set; }
```

Such a property is now read-only for `IncludeReadOnlyProperties = false` (it is left out when writing then), unless it
has `[ToonInclude]`.

## Objects Are Written as Their Declared Type

Before 4.0, an object was written with the members of its runtime type. Now the type it is declared as decides: a
property's type, a collection's item type, a dictionary's value type, or the `T` of `Serialize<T>`.

```csharp
string toon = ToonSerializer.Serialize(new Zoo { Star = new Dog { Name = "Rex", GoodBoy = true } });

public class Zoo { public Animal Star { get; set; } = new(); }

public class Animal { public string Name { get; set; } = ""; }

public class Dog : Animal { public bool GoodBoy { get; set; } }
```

4.0 writes the members of `Animal`:

```toon
Star:
  Name: Rex
```

3.x, and 4.0 with `SerializeRuntimeTypes = true`, also write `GoodBoy: true` under `Star`.

To write derived members:

- declare the property as the derived type, or as `object` (always written as its runtime type), or
- set `ToonSerializerOptions.SerializeRuntimeTypes = true` to get the 3.x behavior everywhere (check that no derived
  type holds data a reader must not see).

Converters registered in the options for an item's or a dictionary value's declared type now apply to it, as they did
for properties.

## Required Members Are Required

`RespectRequiredMembers` is true by default: a document without a key for a `required` property throws
`ToonSerializationException` (`The required property 'Email' is missing`). Set it to false to keep the 3.x behavior:

```csharp
var options = new ToonSerializerOptions { RespectRequiredMembers = false };
```

## ASP.NET Core

`ReadFromToonAsync` (minimal APIs) now rejects bodies over 4 MB with `413 Payload Too Large`, as the MVC formatter did.
For an endpoint that takes larger bodies, give it its own limit:

```csharp
app.MapPost("/import", async (HttpRequest request) => await request.ReadFromToonAsync<Batch>())
   .WithMetadata(new RequestSizeLimitAttribute(64 * 1024 * 1024)); // Microsoft.AspNetCore.Mvc
```

## Source Generators

Use the 4.0 generator with ToonNet.Core 4.0, and rebuild; with a `ToonNet.Core` of another version the generator
reports `TOON011` and generates nothing. Generated files have new names (with a hash of the type's
name), and code from older generators:

- is read with reflection when the options check required members (the default now), because it does not check them;
- does not tell the serializer its property types, so tables of its objects are planned through the tree.

## Other Changes That Can Affect You

- A value tuple (`(int, string)`) throws `NotSupportedException` when written; it used to become an empty object.
  `Tuple<T1, T2>` is still written as `Item1`/`Item2`.
- A multidimensional array (`int[,]`) throws `ToonEncodingException` when written.
- A number out of the range of an enum's underlying type is a `ToonSerializationException` (`300` into a `byte` enum
  used to wrap around to 44).
- Writing documents with an explicit separator throws `ToonSerializationException` when a document has a line equal to
  the separator.
- In YAML, keys that differ only in their tag (`a` and `!!str a`) are duplicate keys; explicit core-schema tags decide
  the type in any style (`!!int "42"` is the number 42) and accept only their values (`!!int 1.5` is an error);
  `ToYaml` quotes the strings YAML 1.1 readers would type (`2001-12-14`, `12:30:00`, `0b101`, `1_000`), `<<`, `=`,
  and strings with carriage returns or YAML 1.1 line breaks.
- `ToonConvert.ToJson` with `Indented = true` indents arrays of numbers like other values (it wrote them on one line
  with a misplaced bracket).
- The MVC output formatter no longer copies the charset of the `Accept` header into the response (UTF-8 and UTF-16
  only); request bodies of actions with their own `[RequestSizeLimit]` are limited by that limit, not by 4 MB.
- `ToonNet.AspNetCore` no longer depends on `Microsoft.Extensions.Configuration.Binder` and
  `Microsoft.Extensions.Options.DataAnnotations`; reference them yourself if your code uses them through ToonNet.
- `ToonYamlConverter.MaxAliasExpansionNodes`, `MaxAliasExpansionChars` and `ToonFormatterDefaults.MaxRequestBodySize`
  are `static readonly` fields instead of constants (they can no longer be used in attributes or `switch` cases).
- Members that use reflection in the JSON, YAML and ASP.NET Core packages are annotated for trimming and NativeAOT,
  so trimmed and AOT apps get a warning where they call them (see
  [Source Generators – NativeAOT and trimming](../advanced/source-generators#nativeaot-and-trimming)).

## Error Messages

Messages that tests or log queries match may have changed:

| 3.x | 4.0 |
|-----|-----|
| `The constructor of X threw: <its message>` | `The constructor of X threw ArgumentException` (the exception is the inner exception) |
| `The key 'Nmae' matches no property ...` | `The key matches no property ...` (the key is in the path) |
| `Duplicate key 'a'` | `Duplicate key "a"` |
| `$.user name.Id` | `$['user name'].Id` (keys that are not identifiers; long keys cut to 64 characters) |
| Rule message `{value}` of a string: `x` | `"x"` (quoted and escaped) |

## Changes in 4.0.1

4.0.1 is a security release. Two of its fixes change what code reads:

- **Constructor parameters are read as System.Text.Json reads them.** A parameter whose member the type keeps out of
  documents (`[ToonIgnore]`, a property without a public getter, a field) is no longer read; it gets its default value.
  A parameter that matches no public property makes reading the type throw `InvalidOperationException` (the generator
  reports warning `TOON012`), unless the constructor is marked `[ToonConstructor]`:

  ```csharp
  public class Login
  {
      [ToonConstructor] // keeps reading `pass` from a key of its name, as before 4.0.1
      public Login(string user, string pass) { User = user; Secret = pass; }

      public string User { get; }
      public string Secret { get; }
  }
  ```

- **Reflection, delegate, stream, file system, process and handle types** (`Type`, `Action`, `Stream`, `FileInfo`,
  `ProcessStartInfo`, `SafeHandle`, ...) are not serialized or deserialized: `NotSupportedException`, as in
  System.Text.Json. Register a converter to write such a value (a file's name, a type's name).

Messages and output that changed: a setter's exception is reported as
`The setter of Email threw ArgumentException` (the exception is the inner exception); strings of three or more
backticks or tildes alone are written quoted; `ReadFromToonAsync` rejects charsets other than UTF-8 and UTF-16; the
MVC output formatter writes a response as the type the action declares.

## See Also

- [Security](../advanced/security): safe defaults and limits
- [CHANGELOG](https://github.com/selcukgural/ToonNet/blob/master/CHANGELOG.md): all changes in 4.0
