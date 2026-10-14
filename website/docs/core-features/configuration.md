# Configuration

Complete guide to customizing ToonNet serialization and deserialization behavior using `ToonSerializerOptions` and `ToonOptions`.

## Overview

ToonNet provides two configuration classes:
- **`ToonSerializerOptions`**: Configure serialization/deserialization behavior
- **`ToonOptions`**: Configure TOON parsing and encoding

## ToonSerializerOptions

Main configuration class for controlling serialization and deserialization.

### Creating Options

```csharp
using ToonNet.Core;
using ToonNet.Core.Serialization;

// Default options
var options = new ToonSerializerOptions();

// Custom options
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
    IgnoreNullValues = true
};
```

### Properties Overview

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `PropertyNamingPolicy` | `PropertyNamingPolicy` | `Default` | Transform property names |
| `PropertyNameCaseInsensitive` | `bool` | `false` | Match keys to property names ignoring case when reading |
| `UnmappedMemberHandling` | `ToonUnmappedMemberHandling` | `Skip` | `Disallow` makes a key that matches no property or constructor parameter an error when reading |
| `RespectRequiredMembers` | `bool` | `true` | Make a missing key for a C# `required` property an error when reading (`false` before 4.0) |
| `IgnoreNullValues` | `bool` | `false` | Skip null properties during serialization |
| `IncludeReadOnlyProperties` | `bool` | `true` | Serialize get-only properties (and properties whose setter is not public, unless `[ToonInclude]`) |
| `SerializeRuntimeTypes` | `bool` | `false` | Write objects with the properties of their runtime type instead of their declared type (`true` before 4.0) |
| `MaxDepth` | `int` | `100` | Maximum object nesting while serializing/deserializing (1–200, or up to 1000 with `AllowExtendedLimits`) |
| `AllowExtendedLimits` | `bool` | `false` | Raise the `MaxDepth` ceiling from 200 to 1000 |
| `Converters` | `List<IToonConverter>` | empty | Custom type converters |
| `ToonOptions` | `ToonOptions` | `new ToonOptions()` | Lower-level parsing and encoding options; cannot be null |

`IncludeTypeInformation` and `PublicOnly` were removed in 3.0 (they had no effect since 2.0).

There is no option to switch indentation off: TOON expresses nesting by indentation, so output is always indented
(see `ToonOptions.IndentSize` below).

## PropertyNamingPolicy

Transform property names during serialization/deserialization.

### Available Policies

```csharp
public enum PropertyNamingPolicy
{
    Default,        // Keep original C# names (FirstName)
    CamelCase,      // firstName, lastName
    SnakeCase,      // first_name, last_name
    LowerCase       // firstname, lastname
}
```

`CamelCase` and `SnakeCase` follow System.Text.Json's `JsonNamingPolicy.CamelCase` and `JsonNamingPolicy.SnakeCaseLower`,
so acronyms stay together: `URLValue` becomes `urlValue` and `url_value`, `HTTPServer` becomes `httpServer` and
`http_server`.

### Examples

```csharp
public class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
}

var person = new Person
{
    FirstName = "Alice",
    LastName = "Smith",
    Age = 30
};
```

#### Default (No transformation)

```csharp
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.Default
};
string toon = ToonSerializer.Serialize(person, options);
```

**Output:**
```toon
FirstName: Alice
LastName: Smith
Age: 30
```

#### CamelCase

```csharp
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase
};
string toon = ToonSerializer.Serialize(person, options);
```

**Output:**
```toon
firstName: Alice
lastName: Smith
age: 30
```

#### SnakeCase

```csharp
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase
};
string toon = ToonSerializer.Serialize(person, options);
```

**Output:**
```toon
first_name: Alice
last_name: Smith
age: 30
```

`SnakeCase` keeps runs of upper-case letters together: `HTTPCode` becomes `http_code`, `UserID` becomes `user_id` and
`MyURLValue` becomes `my_url_value`. Use `[ToonProperty]` for any other spelling.

#### LowerCase

```csharp
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.LowerCase
};
string toon = ToonSerializer.Serialize(person, options);
```

**Output:**
```toon
firstname: Alice
lastname: Smith
age: 30
```

### Deserialization with Naming Policy

Match serialized names during deserialization:

```csharp
string toonInput = """
firstName: Alice
lastName: Smith
age: 30
""";

var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase
};

Person person = ToonSerializer.Deserialize<Person>(toonInput, options);
// person.FirstName = "Alice"
// person.LastName = "Smith"
```

A type marked `[ToonSerializable(NamingPolicy = ...)]` uses that policy instead of the one in the options, whether it is
converted by generated code or by reflection.

Naming policies apply to property names only. Dictionary keys are written as they are, and a `[ToonProperty("name")]`
attribute overrides the policy for that property.

Keep the names distinct after the policy: ToonNet does not check it. Two properties that become the same key
(`UserId` and `userId` under `CamelCase`) are written as one key, with the value of the last, and reading sets both
from it.

## IgnoreNullValues

Control null value serialization.

### Include Nulls (Default)

```csharp
var options = new ToonSerializerOptions { IgnoreNullValues = false };
```

```csharp
public class User
{
    public string Username { get; set; } = "alice";
    public string? Bio { get; set; } = "Engineer";
    public string? Website { get; set; } = null;
}

string toon = ToonSerializer.Serialize(user, options);
```

**Output:**
```toon
Username: alice
Bio: Engineer
Website: null
```

### Ignore Nulls

```csharp
var options = new ToonSerializerOptions { IgnoreNullValues = true };
string toon = ToonSerializer.Serialize(user, options);
```

**Output:**
```toon
Username: alice
Bio: Engineer
```

## Property Name Matching

By default, deserialization matches keys to property names **case-sensitively**, after applying the naming policy (or
the `[ToonProperty]` name).

```csharp
string toonInput = """
Name: Alice
age: 30
""";  // lowercase 'age' won't match 'Age' property

Person person = ToonSerializer.Deserialize<Person>(toonInput);
// person.Age will be 0 (default value) because 'age' doesn't match 'Age'
```

Keys without a matching property are ignored.

Set `PropertyNameCaseInsensitive = true` to match keys ignoring case (`age` then fills `Age`). An exact match is
preferred, properties whose names differ only in case still need an exact match, and the last matching key wins.
Generated code and the `ToonTypeInfo` overloads apply it too. See
[Deserialization](deserialization#key-matching-is-case-sensitive-by-default).

## Strict Reading

Two options turn silent defaults into errors, as `JsonSerializerOptions` does. `RespectRequiredMembers` is on by
default since ToonNet 4.0; `UnmappedMemberHandling` is off by default:

```csharp
var options = new ToonSerializerOptions
{
    UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow, // a key that matches nothing is an error
    RespectRequiredMembers = true                                 // the default: a missing `required` property is an error
};
```

Both throw a `ToonSerializationException` with the path, for example
`The key matches no property or constructor parameter (path: $.Nmae, target type: Account)` or
`The required property 'Name' is missing (path: $, target type: Account)`. The unknown key appears only in the path,
escaped and cut to 64 characters; a key that is not a simple identifier is written as `$['first name']`. Keys of
read-only properties count as matched, so what `ToonSerializer` writes reads back; a key with the value `null` counts
as present; `required` properties are not checked when the constructor used for reading has `[SetsRequiredMembers]`.
Set `RespectRequiredMembers = false` to leave a missing `required` property at its default value, as before 4.0.
Generated code and the `ToonTypeInfo` overloads apply these options too (code generated before ToonNet 3.3 leaves them
to reflection).

## Non-Public Setters and [ToonInclude]

Reading sets only public setters (`set` and `init`), as System.Text.Json does. A property whose setter is `private`,
`protected` or `internal` is not set from a document, so a client cannot set what the code keeps for itself. Mark the
property `[ToonInclude]` (namespace `ToonNet.Core.Serialization.Attributes`) to let reading use its setter:

```csharp
public class Account
{
    public string Name { get; set; } = "";
    public bool IsAdmin { get; private set; }               // not set from documents
    [ToonInclude] public int Logins { get; private set; }   // set from documents
}

var account = ToonSerializer.Deserialize<Account>("Name: Ada\nIsAdmin: true\nLogins: 3")!;
// account.IsAdmin = false, account.Logins = 3
```

Both properties are still written. A property with a non-public setter and no `[ToonInclude]` counts as read-only: it
is left out when `IncludeReadOnlyProperties` is `false`, and its key counts as matched for
`UnmappedMemberHandling.Disallow`. Generated code follows the same rules.

## IncludeReadOnlyProperties

Get-only properties are serialized by default. Set `IncludeReadOnlyProperties = false` to write only properties that
reading can set (a public setter, or a non-public one with `[ToonInclude]`):

```csharp
public class Rectangle
{
    public int Width { get; set; } = 3;
    public int Height { get; set; } = 4;
    public int Area => Width * Height;
}

ToonSerializer.Serialize(new Rectangle());
// Width: 3
// Height: 4
// Area: 12

ToonSerializer.Serialize(new Rectangle(), new ToonSerializerOptions { IncludeReadOnlyProperties = false });
// Width: 3
// Height: 4
```

## SerializeRuntimeTypes

Writing uses the type a value is declared as: a property's type, a collection's item type, a dictionary's value type,
or the `T` of `Serialize<T>`. An object in a property of a base or summary type writes only the properties of that
type, as with System.Text.Json:

```csharp
public class UserSummary { public string Name { get; set; } = ""; }
public class User : UserSummary { public string PasswordHash { get; set; } = ""; }
public class Reply { public UserSummary Author { get; set; } = new(); }

var reply = new Reply { Author = new User { Name = "Ada", PasswordHash = "x9f..." } };

ToonSerializer.Serialize(reply);
// Author:
//   Name: Ada
```

A value declared as an interface writes the interface's properties; a value declared as `object` is written with its
runtime type. Collections, dictionaries and primitives are written as what they are. Converters registered for an item's or a dictionary
value's declared type apply to it, as they do for properties.

`SerializeRuntimeTypes = true` writes every object with the properties of its runtime type, as ToonNet did before 4.0
(the sample above would also write `PasswordHash`). Members of derived types then reach the output, which can expose
data a narrower type was meant to hide. Prefer declaring the type you want to write, or `object` where any type is
meant.

## MaxDepth

`MaxDepth` limits how deeply objects, collections and dictionaries may nest while serializing and deserializing
(default 100). The root value is at depth 0 and each nested object or collection is one level deeper; primitive values
do not count. `ToonOptions.MaxDepth` uses the same rule for parsing, encoding and validation. Values above 200
also need `AllowExtendedLimits` (up to 1000). The two properties can be set in any order; a value above 200 without
extended limits throws `InvalidOperationException` when the options are used, and values outside 1–1000 throw
`ArgumentOutOfRangeException` from the setter:

```csharp
var options = new ToonSerializerOptions
{
    AllowExtendedLimits = true,
    MaxDepth = 500
};
```

Keep `AllowExtendedLimits` off for input you do not control: deep documents cost more stack and time to read. See
[Security](../advanced/security).

Exceeding the limit throws `ToonEncodingException` when serializing and `ToonSerializationException` when
deserializing (parsing the text throws `ToonParseException` for `ToonOptions.MaxDepth`).
Circular references are detected separately and throw `ToonEncodingException` right away.

## Custom Converters

Register custom type converters for specific types.

```csharp
var options = new ToonSerializerOptions();
options.AddConverter(new CustomDateTimeConverter());  // throws on null
options.Converters.Add(new CustomGuidConverter());

string toon = ToonSerializer.Serialize(obj, options);
```

The first converter whose `CanConvert` returns `true` is used. A converter can also be attached to a property or type
with `[ToonConverter(typeof(MyConverter))]`.

See [Custom Converters](../advanced/custom-converters) for detailed guide.

## ToonOptions

Lower-level configuration for TOON parsing and encoding.

```csharp
var toonOptions = new ToonOptions
{
    IndentSize = 2,
    Delimiter = ',',
    StrictMode = true,
    MaxDepth = 100
};

var options = new ToonSerializerOptions
{
    ToonOptions = toonOptions
};
```

### ToonOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IndentSize` | `int` | `2` | Spaces per indentation level, from 1 to 100, written and expected when reading (strict mode rejects other indentation) |
| `Delimiter` | `char` | `','` | Delimiter for inline arrays and tabular rows: `','`, `'\t'` or `'\|'` |
| `StrictMode` | `bool` | `true` | Enforce the spec §14 errors while parsing (see [Deserialization](deserialization#parsing-rules-and-strict-mode)) |
| `MaxDepth` | `int` | `100` | Maximum nesting depth while parsing and encoding (1–200, or up to 1000 with `AllowExtendedLimits`) |
| `AllowExtendedLimits` | `bool` | `false` | Raise the `MaxDepth` ceiling from 200 to 1000 |

Invalid values throw from the setter: `ArgumentOutOfRangeException` for `IndentSize` outside 1–100 and `MaxDepth`
outside 1–1000, `ArgumentException` for `Delimiter`. A `MaxDepth` above 200 without `AllowExtendedLimits` throws
`InvalidOperationException` when parsing or encoding.

### Indent Configuration

```csharp
// 4-space indentation
var toonOptions = new ToonOptions
{
    IndentSize = 4
};
```

Indentation always uses spaces; TOON does not allow tabs in indentation. In strict mode the parser also expects every
indentation to be a multiple of `IndentSize`, so read documents with the same `IndentSize` they were written with.

### Delimiter Configuration

```csharp
var toonOptions = new ToonOptions { Delimiter = '|' };
```

```toon
Tags[2|]: new|priority
Items[2|]{Sku|Qty}:
  A-1|2
  B-7|1
```

Tab and pipe are declared in the array header (`[2|]`, `[2\t]`). The parser always uses the delimiter declared by each
header, so this option only affects encoding.

### Line Endings and Encoding

Output always uses `\n` line endings and has no trailing newline. The file and stream methods of `ToonSerializer`
read and write UTF-8; there is no encoding option. To use another encoding, serialize to a string and write it
yourself.

## Reusing Options

**Best Practice**: Create options once and reuse for better performance.

```csharp
public class ToonService
{
    // Static readonly options - created once
    private static readonly ToonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
        IgnoreNullValues = true
    };
    
    public string Serialize<T>(T obj)
    {
        return ToonSerializer.Serialize(obj, _options);
    }
    
    public T? Deserialize<T>(string toon)
    {
        return ToonSerializer.Deserialize<T>(toon, _options);
    }
}
```

## Configuration Presets

Create common configurations as presets:

```csharp
public static class ToonPresets
{
    // Smaller output: skip nulls
    public static readonly ToonSerializerOptions Compact = new()
    {
        IgnoreNullValues = true
    };
    
    // Debugging: keep every value
    public static readonly ToonSerializerOptions Verbose = new()
    {
        IgnoreNullValues = false
    };
    
    // API-friendly (camelCase)
    public static readonly ToonSerializerOptions Api = new()
    {
        PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
        IgnoreNullValues = true
    };
    
    // Hand-edited configuration files you trust (snake_case, lenient parsing)
    public static readonly ToonSerializerOptions Config = new()
    {
        PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase,
        IgnoreNullValues = true,
        ToonOptions = new ToonOptions { StrictMode = false }
    };
}

// Usage
string toon = ToonSerializer.Serialize(data, ToonPresets.Api);
```

Use `StrictMode = false` only for documents you trust. Non-strict reading does not check array lengths and lets the
last of duplicate keys win, so a document can read differently here than in a strict reader or a check that looked at
the first key. Keep the default for request bodies and other untrusted input. See [Security](../advanced/security).

## Environment-Specific Configuration

```csharp
public static class ToonConfig
{
    public static ToonSerializerOptions GetOptions(string environment)
    {
        return environment switch
        {
            "Development" => new ToonSerializerOptions
            {
                IgnoreNullValues = false  // Include nulls for debugging
            },
            "Production" => new ToonSerializerOptions
            {
                IgnoreNullValues = true  // Optimize size
            },
            _ => new ToonSerializerOptions()
        };
    }
}

// Usage
var options = ToonConfig.GetOptions(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
string toon = ToonSerializer.Serialize(data, options);
```

## Complete Configuration Example

```csharp
// Full-featured configuration
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
    IgnoreNullValues = true,
    IncludeReadOnlyProperties = false,
    MaxDepth = 50,
    ToonOptions = new ToonOptions
    {
        IndentSize = 2,
        Delimiter = '|',
        StrictMode = true
    }
};

// Add custom converters
options.AddConverter(new CustomDateTimeConverter());
options.AddConverter(new CustomGuidConverter());

// Use for serialization
string toon = ToonSerializer.Serialize(data, options);

// Use for deserialization
var result = ToonSerializer.Deserialize<MyData>(toon, options);
```

## Configuration for Different Scenarios

### Scenario 1: Web API Response

```csharp
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
    IgnoreNullValues = true  // Minimize response size
};
```

### Scenario 2: Configuration Files

```csharp
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase,
    IgnoreNullValues = true,
    ToonOptions = new ToonOptions { StrictMode = false }  // tolerate hand-edited files
};
```

Only for files you trust; keep strict mode for untrusted input.

### Scenario 3: Logging/Debugging

```csharp
var options = new ToonSerializerOptions
{
    IgnoreNullValues = false,  // Include all data
    PropertyNamingPolicy = PropertyNamingPolicy.Default
};
```

### Scenario 4: Data Migration

```csharp
var options = new ToonSerializerOptions
{
    IgnoreNullValues = false,  // Preserve all data
    PropertyNamingPolicy = PropertyNamingPolicy.Default,
    ToonOptions = new ToonOptions { StrictMode = false }  // accept documents from older writers
};
```

Only for documents you trust, such as your own exports; keep strict mode for untrusted input
(see [Security](../advanced/security)).

## Thread-Safety

- `ToonSerializer` methods are safe to call concurrently across threads.
- A `ToonEncoder` instance keeps no per-call state and can be shared across threads (it is registered as a singleton by `AddToonNet`).
- Shared metadata/name caches use `ConcurrentDictionary` for concurrent access.
- Cache entries are created on demand and retained for the process lifetime (no eviction).
- Reuse `ToonSerializerOptions` instances, but do not mutate a single instance concurrently across threads.

## See Also

- **[Serialization](serialization)**: Using options during serialization
- **[Deserialization](deserialization)**: Using options during deserialization
- **[Custom Converters](../advanced/custom-converters)**: Creating custom type converters
