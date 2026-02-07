# Serialization

Complete guide to serializing .NET objects to TOON format using `ToonSerializer`.

## Overview

`ToonSerializer` is a static class that provides methods for converting .NET objects to TOON format. It uses expression trees (not reflection) for high performance.

## Basic Serialization

### Serialize to String

```csharp
using ToonNet.Core;

var person = new Person { Name = "Alice", Age = 30 };
string toon = ToonSerializer.Serialize(person);
```

### Serialize to Stream

```csharp
using var stream = new MemoryStream();
ToonSerializer.Serialize(stream, person);

// Or with explicit encoding
ToonSerializer.Serialize(stream, person, Encoding.UTF8);
```

### Serialize to File

```csharp
// Synchronous
ToonSerializer.SerializeToFile("person.toon", person);

// Asynchronous
await ToonSerializer.SerializeToFileAsync("person.toon", person);
```

## Serialization Options

### Using ToonSerializerOptions

Configure serialization behavior with `ToonSerializerOptions`:

```csharp
var options = new ToonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
    IgnoreNullValues = false
};

string toon = ToonSerializer.Serialize(person, options);
```

### Write Indented (Formatting)

Control output formatting:

```csharp
// Compact (default)
var options = new ToonSerializerOptions { WriteIndented = false };
string compact = ToonSerializer.Serialize(person, options);
// Output: Name: Alice\nAge: 30

// Indented (readable)
var options = new ToonSerializerOptions { WriteIndented = true };
string indented = ToonSerializer.Serialize(person, options);
// Output (with proper indentation for nested objects)
```

### Property Naming Policy

Transform property names during serialization:

```csharp
public enum PropertyNamingPolicy
{
    Default,        // Keep original names (default)
    CamelCase,      // firstName, lastName
    SnakeCase,      // first_name, last_name
    KebabCase,      // first-name, last-name
    PascalCase      // FirstName, LastName
}
```

**Example:**

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

// Default naming
var defaultOptions = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.Default
};
string toon1 = ToonSerializer.Serialize(person, defaultOptions);
// Output:
// FirstName: Alice
// LastName: Smith
// Age: 30

// CamelCase naming
var camelOptions = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase
};
string toon2 = ToonSerializer.Serialize(person, camelOptions);
// Output:
// firstName: Alice
// lastName: Smith
// age: 30

// SnakeCase naming
var snakeOptions = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase
};
string toon3 = ToonSerializer.Serialize(person, snakeOptions);
// Output:
// first_name: Alice
// last_name: Smith
// age: 30
```

### Ignore Null Values

Control null value serialization:

```csharp
public class User
{
    public string Username { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
}

var user = new User
{
    Username = "alice",
    Bio = "Engineer",
    Website = null
};

// Include nulls (default)
var includeNulls = new ToonSerializerOptions { IgnoreNullValues = false };
string toon1 = ToonSerializer.Serialize(user, includeNulls);
// Output:
// Username: alice
// Bio: Engineer
// Website: null

// Ignore nulls
var ignoreNulls = new ToonSerializerOptions { IgnoreNullValues = true };
string toon2 = ToonSerializer.Serialize(user, ignoreNulls);
// Output:
// Username: alice
// Bio: Engineer
```

## Advanced Scenarios

### Serialize Collections

```csharp
// Array
int[] numbers = { 1, 2, 3, 4, 5 };
string toon = ToonSerializer.Serialize(numbers);

// List
List<string> names = new() { "Alice", "Bob", "Charlie" };
string toon = ToonSerializer.Serialize(names);

// Dictionary
Dictionary<string, int> scores = new()
{
    ["Math"] = 95,
    ["Physics"] = 87
};
string toon = ToonSerializer.Serialize(scores);
```

### Serialize with Custom Converters

Register custom converters for specific types:

```csharp
var options = new ToonSerializerOptions();
options.Converters.Add(new CustomDateTimeConverter());

string toon = ToonSerializer.Serialize(obj, options);
```

See [Custom Converters](../advanced/custom-converters) for details.

### Serialize Polymorphic Types

```csharp
public abstract class Shape
{
    public string Color { get; set; }
}

public class Circle : Shape
{
    public double Radius { get; set; }
}

public class Rectangle : Shape
{
    public double Width { get; set; }
    public double Height { get; set; }
}

// Serialize as base type
Shape shape = new Circle { Color = "Red", Radius = 5.0 };
string toon = ToonSerializer.Serialize<Shape>(shape);
// Output includes all properties from Circle
```

### Serialize Complex Nested Structures

```csharp
public class Company
{
    public string Name { get; set; }
    public List<Department> Departments { get; set; }
}

public class Department
{
    public string Name { get; set; }
    public List<Employee> Employees { get; set; }
}

public class Employee
{
    public string Name { get; set; }
    public string Position { get; set; }
    public Dictionary<string, int> Skills { get; set; }
}

var company = new Company
{
    Name = "TechCorp",
    Departments = new List<Department>
    {
        new Department
        {
            Name = "Engineering",
            Employees = new List<Employee>
            {
                new Employee
                {
                    Name = "Alice",
                    Position = "Senior Engineer",
                    Skills = new Dictionary<string, int>
                    {
                        ["C#"] = 9,
                        ["Python"] = 7
                    }
                }
            }
        }
    }
};

string toon = ToonSerializer.Serialize(company);
```

**Output:**
```toon
Name: TechCorp
Departments:
  - Name: Engineering
    Employees:
      - Name: Alice
        Position: Senior Engineer
        Skills:
          C#: 9
          Python: 7
```

## Async Serialization

### Serialize to Stream (Async)

```csharp
using var fileStream = File.Create("data.toon");
await ToonSerializer.SerializeAsync(fileStream, data);
```

### Serialize to File (Async)

```csharp
await ToonSerializer.SerializeToFileAsync("data.toon", data);

// With options
var options = new ToonSerializerOptions { WriteIndented = true };
await ToonSerializer.SerializeToFileAsync("data.toon", data, options);
```

## Performance Tips

1. **Reuse ToonSerializerOptions**: Create once, use multiple times
2. **Use async methods** for I/O-bound operations
3. **Avoid reflection**: ToonNet uses expression trees automatically
4. **Stream for large data**: Use stream methods instead of string methods
5. **Profile your code**: Use BenchmarkDotNet for optimization

```csharp
// Good: Reuse options
private static readonly ToonSerializerOptions _options = new()
{
    WriteIndented = true,
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase
};

public string SerializeUser(User user)
{
    return ToonSerializer.Serialize(user, _options);
}
```

## All Serialize Methods

| Method | Description | Use Case |
|--------|-------------|----------|
| `Serialize<T>(T value)` | Serialize to string | Simple, in-memory data |
| `Serialize<T>(T value, ToonSerializerOptions)` | Serialize with options | Custom formatting |
| `Serialize<T>(Stream, T value)` | Serialize to stream | Large data, file output |
| `Serialize<T>(Stream, T, Encoding)` | Serialize with encoding | Non-UTF8 encoding |
| `SerializeAsync<T>(Stream, T)` | Async stream serialization | Async I/O |
| `SerializeAsync<T>(Stream, T, ToonSerializerOptions)` | Async with options | Async + custom options |
| `SerializeToFile<T>(string, T)` | Serialize to file | File persistence |
| `SerializeToFileAsync<T>(string, T)` | Async file serialization | Async file I/O |

## Error Handling

```csharp
try
{
    string toon = ToonSerializer.Serialize(obj);
}
catch (ToonSerializationException ex)
{
    Console.WriteLine($"Serialization failed: {ex.Message}");
    Console.WriteLine($"Property: {ex.PropertyName}");
    Console.WriteLine($"Type: {ex.TargetType}");
}
catch (ToonEncodingException ex)
{
    Console.WriteLine($"Encoding error: {ex.Message}");
    Console.WriteLine($"Property path: {ex.PropertyPath}");
    Console.WriteLine($"Value: {ex.ProblematicValue}");
}
```

## Common Issues

### Issue: Circular References

**Problem**: Object graph contains circular references.

**Solution**: ToonNet detects and throws `ToonSerializationException`. Break the cycle:

```csharp
public class Parent
{
    public string Name { get; set; }
    public List<Child> Children { get; set; }
}

public class Child
{
    public string Name { get; set; }
    // Don't serialize parent reference
    [ToonIgnore]  // Custom attribute (if implemented)
    public Parent Parent { get; set; }
}
```

### Issue: Large Objects

**Problem**: Serializing very large objects causes memory issues.

**Solution**: Use streaming:

```csharp
using var fileStream = File.Create("large-data.toon");
await ToonSerializer.SerializeAsync(fileStream, largeObject);
```

## Thread-Safety

- `ToonSerializer` methods are safe to call concurrently across threads.
- Shared metadata/name caches use `ConcurrentDictionary` for concurrent access.
- Cache entries are created on demand and retained for the process lifetime (no eviction).
- Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.

## See Also

- **[Deserialization](deserialization)**: Convert TOON back to objects
- **[Configuration](configuration)**: Detailed options guide
- **[Performance Tuning](../advanced/performance-tuning)**: Optimization strategies
- **[Custom Converters](../advanced/custom-converters)**: Create custom type converters
