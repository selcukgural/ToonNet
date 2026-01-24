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
// Default options
var options = new ToonSerializerOptions();

// Custom options
var options = new ToonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
    IgnoreNullValues = true
};
```

### Properties Overview

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `WriteIndented` | `bool` | `false` | Format output with indentation |
| `PropertyNamingPolicy` | `PropertyNamingPolicy` | `Default` | Transform property names |
| `IgnoreNullValues` | `bool` | `false` | Skip null properties during serialization |
| `CaseSensitive` | `bool` | `true` | Case-sensitive property matching |
| `AllowTrailingCommas` | `bool` | `false` | Allow trailing commas in arrays/objects |
| `Converters` | `IList<IToonConverter>` | `[]` | Custom type converters |
| `ToonOptions` | `ToonOptions` | `Default` | Lower-level parsing options |

## WriteIndented

Controls output formatting (indentation).

### Compact Format (Default)

```csharp
var options = new ToonSerializerOptions { WriteIndented = false };
string toon = ToonSerializer.Serialize(person, options);
```

**Output:**
```toon
Name: Alice
Age: 30
Email: alice@example.com
```

### Indented Format

```csharp
var options = new ToonSerializerOptions { WriteIndented = true };
string toon = ToonSerializer.Serialize(person, options);
```

**Output (with proper indentation for nested structures):**
```toon
Name: Alice
Age: 30
Address:
  Street: 123 Main St
  City: New York
  ZipCode: 10001
```

## PropertyNamingPolicy

Transform property names during serialization/deserialization.

### Available Policies

```csharp
public enum PropertyNamingPolicy
{
    Default,        // Keep original C# names
    CamelCase,      // firstName, lastName
    SnakeCase,      // first_name, last_name
    KebabCase,      // first-name, last-name
    PascalCase      // FirstName, LastName
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

#### KebabCase

```csharp
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.KebabCase
};
string toon = ToonSerializer.Serialize(person, options);
```

**Output:**
```toon
first-name: Alice
last-name: Smith
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

## CaseSensitive

Control case sensitivity during deserialization.

### Case Sensitive (Default)

```csharp
var options = new ToonSerializerOptions { CaseSensitive = true };
```

```csharp
string toonInput = """
Name: Alice
age: 30
""";  // lowercase 'age' won't match 'Age' property

Person person = ToonSerializer.Deserialize<Person>(toonInput, options);
// person.Age will be 0 (default value) because 'age' doesn't match 'Age'
```

### Case Insensitive

```csharp
var options = new ToonSerializerOptions { CaseSensitive = false };
```

```csharp
string toonInput = """
name: Alice
AGE: 30
eMaIl: alice@example.com
""";

Person person = ToonSerializer.Deserialize<Person>(toonInput, options);
// All properties matched successfully!
// person.Name = "Alice"
// person.Age = 30
// person.Email = "alice@example.com"
```

## AllowTrailingCommas

Allow trailing commas in collections (future feature).

```csharp
var options = new ToonSerializerOptions { AllowTrailingCommas = true };
```

## Custom Converters

Register custom type converters for specific types.

```csharp
var options = new ToonSerializerOptions();
options.Converters.Add(new CustomDateTimeConverter());
options.Converters.Add(new CustomGuidConverter());

string toon = ToonSerializer.Serialize(obj, options);
```

See [Custom Converters](../advanced/custom-converters) for detailed guide.

## ToonOptions

Lower-level configuration for TOON parsing and encoding.

```csharp
var toonOptions = new ToonOptions
{
    IndentSize = 2,
    IndentChar = ' ',
    NewLine = "\n",
    Encoding = Encoding.UTF8
};

var options = new ToonSerializerOptions
{
    ToonOptions = toonOptions
};
```

### ToonOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IndentSize` | `int` | `2` | Number of indent characters per level |
| `IndentChar` | `char` | `' '` (space) | Character used for indentation |
| `NewLine` | `string` | `Environment.NewLine` | Line ending character(s) |
| `Encoding` | `Encoding` | `Encoding.UTF8` | Text encoding |

### Indent Configuration

```csharp
// 4-space indentation
var toonOptions = new ToonOptions
{
    IndentSize = 4,
    IndentChar = ' '
};

// Tab indentation
var toonOptions = new ToonOptions
{
    IndentSize = 1,
    IndentChar = '\t'
};
```

### Line Ending Configuration

```csharp
// Unix-style (LF)
var toonOptions = new ToonOptions { NewLine = "\n" };

// Windows-style (CRLF)
var toonOptions = new ToonOptions { NewLine = "\r\n" };

// Mac-style (CR) - legacy
var toonOptions = new ToonOptions { NewLine = "\r" };
```

### Encoding Configuration

```csharp
// UTF-8 (default)
var toonOptions = new ToonOptions { Encoding = Encoding.UTF8 };

// UTF-16
var toonOptions = new ToonOptions { Encoding = Encoding.Unicode };

// ASCII
var toonOptions = new ToonOptions { Encoding = Encoding.ASCII };
```

## Reusing Options

**Best Practice**: Create options once and reuse for better performance.

```csharp
public class ToonService
{
    // Static readonly options - created once
    private static readonly ToonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
        IgnoreNullValues = true
    };
    
    private static readonly ToonSerializerOptions _deserializerOptions = new()
    {
        PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
        CaseSensitive = false
    };
    
    public string Serialize<T>(T obj)
    {
        return ToonSerializer.Serialize(obj, _serializerOptions);
    }
    
    public T Deserialize<T>(string toon)
    {
        return ToonSerializer.Deserialize<T>(toon, _deserializerOptions);
    }
}
```

## Configuration Presets

Create common configurations as presets:

```csharp
public static class ToonPresets
{
    // Compact, production-ready
    public static readonly ToonSerializerOptions Compact = new()
    {
        WriteIndented = false,
        IgnoreNullValues = true
    };
    
    // Human-readable, development
    public static readonly ToonSerializerOptions Readable = new()
    {
        WriteIndented = true,
        IgnoreNullValues = false
    };
    
    // API-friendly (camelCase)
    public static readonly ToonSerializerOptions Api = new()
    {
        PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
        IgnoreNullValues = true,
        CaseSensitive = false
    };
    
    // Configuration files (snake_case)
    public static readonly ToonSerializerOptions Config = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase,
        IgnoreNullValues = true
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
                WriteIndented = true,
                IgnoreNullValues = false  // Include nulls for debugging
            },
            "Production" => new ToonSerializerOptions
            {
                WriteIndented = false,
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
    WriteIndented = true,
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
    IgnoreNullValues = true,
    CaseSensitive = false,
    AllowTrailingCommas = true,
    ToonOptions = new ToonOptions
    {
        IndentSize = 2,
        IndentChar = ' ',
        NewLine = "\n",
        Encoding = Encoding.UTF8
    }
};

// Add custom converters
options.Converters.Add(new CustomDateTimeConverter());
options.Converters.Add(new CustomGuidConverter());

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
    IgnoreNullValues = true,
    WriteIndented = false  // Minimize response size
};
```

### Scenario 2: Configuration Files

```csharp
var options = new ToonSerializerOptions
{
    WriteIndented = true,  // Human-readable
    PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase,
    IgnoreNullValues = true
};
```

### Scenario 3: Logging/Debugging

```csharp
var options = new ToonSerializerOptions
{
    WriteIndented = true,
    IgnoreNullValues = false,  // Include all data
    PropertyNamingPolicy = PropertyNamingPolicy.Default
};
```

### Scenario 4: Data Migration

```csharp
var options = new ToonSerializerOptions
{
    CaseSensitive = false,  // Flexible property matching
    IgnoreNullValues = false,  // Preserve all data
    PropertyNamingPolicy = PropertyNamingPolicy.Default
};
```

## See Also

- **[Serialization](serialization)**: Using options during serialization
- **[Deserialization](deserialization)**: Using options during deserialization
- **[Custom Converters](../advanced/custom-converters)**: Creating custom type converters
