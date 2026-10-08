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
| `IgnoreNullValues` | `bool` | `false` | Skip null properties during serialization |
| `IncludeReadOnlyProperties` | `bool` | `true` | Serialize get-only properties |
| `MaxDepth` | `int` | `100` | Maximum object nesting while serializing/deserializing (1–200, or up to 1000 with `AllowExtendedLimits`) |
| `AllowExtendedLimits` | `bool` | `false` | Raise the `MaxDepth` ceiling from 200 to 1000 |
| `Converters` | `List<IToonConverter>` | empty | Custom type converters |
| `ToonOptions` | `ToonOptions` | `new ToonOptions()` | Lower-level parsing and encoding options; cannot be null |

`IncludeTypeInformation` and `PublicOnly` still exist but are `[Obsolete]` and have no effect.

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

`SnakeCase` inserts `_` before every upper-case letter, so acronyms are split letter by letter (`HTTPCode` becomes
`h_t_t_p_code`); use `[ToonProperty("http_code")]` for such names.

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

Naming policies apply to property names only. Dictionary keys are written as they are, and a `[ToonProperty("name")]`
attribute overrides the policy for that property.

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

Deserialization matches keys to property names **case-sensitively**, after applying the naming policy (or the
`[ToonProperty]` name). There is no case-insensitive option.

```csharp
string toonInput = """
Name: Alice
age: 30
""";  // lowercase 'age' won't match 'Age' property

Person person = ToonSerializer.Deserialize<Person>(toonInput);
// person.Age will be 0 (default value) because 'age' doesn't match 'Age'
```

Keys without a matching property are ignored.

## IncludeReadOnlyProperties

Get-only properties are serialized by default. Set `IncludeReadOnlyProperties = false` to write only properties that
have a setter:

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

## MaxDepth

`MaxDepth` limits how deeply objects may nest while serializing and deserializing (default 100). Values above 200
throw `ArgumentOutOfRangeException` unless `AllowExtendedLimits` is set first, which allows up to 1000:

```csharp
var options = new ToonSerializerOptions
{
    AllowExtendedLimits = true,  // set before MaxDepth
    MaxDepth = 500
};
```

Exceeding the limit throws `ToonEncodingException` when serializing and `ToonParseException` when deserializing.
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
| `IndentSize` | `int` | `2` | Spaces per indentation level; an even number from 2 to 100 |
| `Delimiter` | `char` | `','` | Delimiter for inline arrays and tabular rows: `','`, `'\t'` or `'\|'` |
| `StrictMode` | `bool` | `true` | Enforce the spec §14 errors while parsing (see [Deserialization](deserialization#parsing-rules-and-strict-mode)) |
| `MaxDepth` | `int` | `100` | Maximum nesting depth while parsing and encoding (1–200, or up to 1000 with `AllowExtendedLimits`) |
| `AllowExtendedLimits` | `bool` | `false` | Raise the `MaxDepth` ceiling from 200 to 1000 |

Invalid values throw from the setter: `ArgumentOutOfRangeException` for `IndentSize` and `MaxDepth`,
`ArgumentException` for `Delimiter`.

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
    
    // Hand-edited configuration files (snake_case, lenient parsing)
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

## Thread-Safety

- `ToonSerializer` methods are safe to call concurrently across threads.
- Shared metadata/name caches use `ConcurrentDictionary` for concurrent access.
- Cache entries are created on demand and retained for the process lifetime (no eviction).
- Reuse `ToonSerializerOptions` instances, but do not mutate a single instance concurrently across threads.

## See Also

- **[Serialization](serialization)**: Using options during serialization
- **[Deserialization](deserialization)**: Using options during deserialization
- **[Custom Converters](../advanced/custom-converters)**: Creating custom type converters
