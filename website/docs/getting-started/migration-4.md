# Migrating to 4.0

ToonNet 4.0 changes three defaults that let documents and responses do more than the code meant, so they now behave
as System.Text.Json does. Everything else from 3.x keeps working; this page lists what to check.

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
public class Animal { public string Name { get; set; } = ""; }
public class Dog : Animal { public bool GoodBoy { get; set; } }

ToonSerializer.Serialize(new Zoo { Star = new Dog { Name = "Rex", GoodBoy = true } });
// 3.x: Star: { Name: Rex, GoodBoy: true }    4.0: Star: { Name: Rex }
```

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

Use the 4.0 generator with ToonNet.Core 4.0, and rebuild. Generated files have new names (with a hash of the type's
name), and code from older generators:

- is read with reflection when the options check required members (the default now), because it does not check them;
- does not tell the serializer its property types, so tables of its objects are planned through the tree.

## Error Messages

Messages that tests or log queries match may have changed:

| 3.x | 4.0 |
|-----|-----|
| `The constructor of X threw: <its message>` | `The constructor of X threw ArgumentException` (the exception is the inner exception) |
| `The key 'Nmae' matches no property ...` | `The key matches no property ...` (the key is in the path) |
| `Duplicate key 'a'` | `Duplicate key "a"` |
| `$.user name.Id` | `$['user name'].Id` (keys that are not identifiers; long keys cut to 64 characters) |
| Rule message `{value}` of a string: `x` | `"x"` (quoted and escaped) |

## See Also

- [Security](../advanced/security): safe defaults and limits
- [CHANGELOG](https://github.com/selcukgural/ToonNet/blob/master/CHANGELOG.md): all changes in 4.0
