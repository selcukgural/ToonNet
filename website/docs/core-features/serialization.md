# Serialization

Complete guide to serializing .NET objects to TOON format using `ToonSerializer`.

## Overview

`ToonSerializer` (namespace `ToonNet.Core.Serialization`) is a static class that converts .NET objects to TOON format.
Type metadata is discovered with reflection once per type and cached; property getters and setters are compiled
expression trees, so later calls do not pay the reflection cost again.

## Basic Serialization

### Serialize to String

```csharp
using ToonNet.Core.Serialization;

var person = new Person { Name = "Alice", Age = 30 };
string toon = ToonSerializer.Serialize(person);
// Name: Alice
// Age: 30
```

The output has no trailing newline.

### Serialize to Stream

```csharp
using var stream = new MemoryStream();
await ToonSerializer.SerializeToStreamAsync(person, stream);  // written as UTF-8
```

### Serialize to File

```csharp
await ToonSerializer.SerializeToFileAsync(person, "person.toon");  // written as UTF-8
```

There are no synchronous stream or file overloads; use `Serialize` and write the string yourself if you need one.

## Serialization Options

### Using ToonSerializerOptions

Configure serialization behavior with `ToonSerializerOptions`:

```csharp
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
    IgnoreNullValues = false
};

string toon = ToonSerializer.Serialize(person, options);
```

### Indentation and Delimiter

TOON output is always indented; nesting is expressed by indentation. The indent width and the delimiter used in
inline arrays and tabular rows are set on the nested `ToonOptions`:

```csharp
var options = new ToonSerializerOptions
{
    ToonOptions = new ToonOptions
    {
        IndentSize = 4,     // even number, 2..100 (default 2)
        Delimiter = '|'     // ',' (default), '\t' or '|'
    }
};

string toon = ToonSerializer.Serialize(order, options);
// Tags[2|]: new|priority
// Items[2|]{Sku|Qty}:
//     A-1|2
//     B-7|1
```

### Property Naming Policy

Transform property names during serialization:

```csharp
public enum PropertyNamingPolicy
{
    Default,        // Keep original names (default)
    CamelCase,      // firstName, lastName
    SnakeCase,      // first_name, last_name
    LowerCase       // firstname, lastname
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

`IgnoreNullValues` only skips object properties; `null` items inside arrays are always kept.

## Advanced Scenarios

### Serialize Collections

```csharp
// Array
int[] numbers = { 1, 2, 3, 4, 5 };
string toon1 = ToonSerializer.Serialize(numbers);
// [5]: 1,2,3,4,5

// List
List<string> names = new() { "Alice", "Bob", "Charlie" };
string toon2 = ToonSerializer.Serialize(names);
// [3]: Alice,Bob,Charlie

// Dictionary
Dictionary<string, int> scores = new()
{
    ["Math"] = 95,
    ["Physics"] = 87
};
string toon3 = ToonSerializer.Serialize(scores);
// Math: 95
// Physics: 87

// Uniform objects use the tabular form
var employees = new List<Employee>
{
    new() { Name = "Alice", Department = "Engineering", Salary = 85000 },
    new() { Name = "Bob", Department = "Marketing", Salary = 65000 }
};
string toon4 = ToonSerializer.Serialize(employees);
// [2]{Name,Department,Salary}:
//   Alice,Engineering,85000
//   Bob,Marketing,65000

// Empty collections
string toon5 = ToonSerializer.Serialize(new { Tags = new string[0] });
// Tags: []
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
// Color: Red
// Radius: 5
```

Objects are written with their runtime type, base-class properties first. No type discriminator is written, so
reading the text back into `Shape` needs a [custom converter](../advanced/custom-converters).

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
Departments[1]:
  - Name: Engineering
    Employees[1]:
      - Name: Alice
        Position: Senior Engineer
        Skills:
          "C#": 9
          Python: 7
```

## Async Serialization

### Serialize to String (Async)

```csharp
string toon = await ToonSerializer.SerializeAsync(data);
```

`SerializeAsync` runs synchronously and returns a completed `ValueTask<string>`; it exists for API symmetry.

### Serialize to Stream (Async)

```csharp
await using var fileStream = File.Create("data.toon");
await ToonSerializer.SerializeToStreamAsync(data, fileStream);
```

### Serialize to File (Async)

```csharp
await ToonSerializer.SerializeToFileAsync(data, "data.toon");

// With options
var options = new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase };
await ToonSerializer.SerializeToFileAsync(data, "data.toon", options);
```

## Streaming Serialization (Large Datasets)

For large datasets (millions of records, database exports, ETL pipelines), use streaming serialization to avoid loading all data into memory.

### Basic Streaming

Stream data incrementally without memory pressure:

```csharp
// Example: Export millions of users from database
await ToonSerializer.SerializeStreamAsync(
    items: dbContext.Users.AsAsyncEnumerable(),
    filePath: "users_export.toon",
    cancellationToken: cancellationToken
);

// Deserialize incrementally
await foreach (var user in ToonSerializer.DeserializeStreamAsync<User>("users_export.toon"))
{
    await ProcessUserAsync(user);  // Only one user in memory at a time
}
```

### Advanced Streaming Options

Configure separator mode and batch size:

```csharp
// Custom write options for large datasets
var writeOptions = new ToonMultiDocumentWriteOptions
{
    Mode = ToonMultiDocumentSeparatorMode.ExplicitSeparator,  // Use "---" separator
    DocumentSeparator = "---",
    BatchSize = 100  // Buffer 100 items before writing (improves I/O throughput)
};

await ToonSerializer.SerializeStreamAsync(
    items: GenerateLargeDatasetAsync(),
    filePath: "export.toon",
    options: serializerOptions,
    writeOptions: writeOptions,
    cancellationToken: cts.Token
);

// Read with matching separator mode
await foreach (var item in ToonSerializer.DeserializeStreamAsync<Item>(
    "export.toon",
    options: serializerOptions,
    multiDocumentOptions: ToonMultiDocumentReadOptions.ExplicitSeparator,
    cancellationToken: cts.Token))
{
    ProcessItem(item);
}
```

### Separator Modes

Choose the separator mode that fits your use case:

| Mode | Format | Use Case |
|------|--------|----------|
| **BlankLine** (default) | Documents separated by one blank line | Human-readable; the encoder never writes blank lines inside a document |
| **ExplicitSeparator** | Documents separated by a `---` line (`DocumentSeparator`) | Explicit boundaries, YAML-like |

The separator is always written with `\n`, independent of the platform line ending. Read the file back with the
same mode.

**Example output (BlankLine):**
```toon
Name: Alice
Age: 25

Name: Bob
Age: 30

Name: Charlie
Age: 35
```

**Example output (ExplicitSeparator):**
```toon
Name: Alice
Age: 25
---
Name: Bob
Age: 30
---
Name: Charlie
Age: 35
```

### Memory Characteristics

- Items are pulled from the `IAsyncEnumerable<T>` one at a time, so the source never has to be materialized.
- Up to `BatchSize` serialized documents (default 50) are buffered before each write, so memory use grows with batch
  size × item size, not with the number of items.
- `DeserializeStreamAsync` reads line by line and keeps only the current document in memory.
- The `CancellationToken` is checked for every item.

The repository contains a BenchmarkDotNet suite for streaming (`benchmark/ToonNet.Benchmarks/StreamingSerializationBenchmarks.cs`);
run it on your own hardware and data shape for numbers.

### Use Cases

**Database Exports:**
```csharp
// Export entire table without loading into memory
await ToonSerializer.SerializeStreamAsync(
    dbContext.Orders
        .AsNoTracking()
        .AsAsyncEnumerable(),
    "orders_backup.toon"
);
```

**ETL Pipelines:**
```csharp
// Transform and export data incrementally
await ToonSerializer.SerializeStreamAsync(
    ReadAndTransformDataAsync(),
    "transformed_data.toon"
);

async IAsyncEnumerable<TransformedData> ReadAndTransformDataAsync()
{
    await foreach (var raw in ReadRawDataAsync())
    {
        yield return Transform(raw);
    }
}
```

**Log Processing:**
```csharp
// Process multi-GB log files without memory issues
await foreach (var logEntry in ToonSerializer.DeserializeStreamAsync<LogEntry>("app.log.toon"))
{
    if (logEntry.Level == "ERROR")
    {
        await AlertAsync(logEntry);
    }
}
```

## Performance Tips

1. **Reuse ToonSerializerOptions**: Create once, use multiple times
2. **Use async methods** for I/O-bound operations
3. **Metadata is cached**: the first call for a type builds and caches its metadata; later calls reuse it
4. **Stream large collections**: use `SerializeStreamAsync` instead of building one huge string
5. **Profile your code**: Use BenchmarkDotNet for optimization

```csharp
// Good: Reuse options
private static readonly ToonSerializerOptions _options = new()
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase
};

public string SerializeUser(User user)
{
    return ToonSerializer.Serialize(user, _options);
}
```

## All Serialize Methods

All methods take an optional `ToonSerializerOptions? options = null`; the async ones also take an optional
`CancellationToken`.

| Method | Description | Use Case |
|--------|-------------|----------|
| `Serialize<T>(T value, options)` | Serialize to string | Simple, in-memory data |
| `Serialize(object value, Type type, options)` | Serialize with a runtime type | Non-generic code |
| `SerializeToValue<T>(T value, options)` | Convert to a `ToonValue` tree without text | Inspect or modify before encoding |
| `SerializeAsync<T>(T value, options, ct)` | Serialize to string (`ValueTask<string>`) | Async call sites |
| `SerializeToStreamAsync<T>(T value, Stream, options, ct)` | Write UTF-8 to a stream | Network, files |
| `SerializeToStreamAsync(Type, object value, Stream, options, ct)` | Same, with a runtime type | Non-generic code |
| `SerializeToFileAsync<T>(T value, string filePath, options, ct)` | Write UTF-8 to a file | File persistence |
| `SerializeCollectionToFileAsync<T>(IEnumerable<T>, string filePath, options, ct)` | One document per item, blank-line separated | Multi-document files |
| `SerializeCollectionToStreamAsync<T>(IEnumerable<T>, Stream, options, ct)` | Same, to a stream | Multi-document streams |
| **`SerializeStreamAsync<T>(IAsyncEnumerable<T>, string filePath, ...)`** | **Stream large datasets to a file** | **DB exports, large datasets** |
| **`SerializeStreamAsync<T>(IAsyncEnumerable<T>, Stream, ...)`** | **Stream to a stream** | **ETL pipelines** |

The `SerializeStreamAsync` overloads that take `ToonMultiDocumentWriteOptions` control the separator mode and batch
size. See [Streaming](streaming) for details.

## Error Handling

```csharp
try
{
    string toon = ToonSerializer.Serialize(obj);
}
catch (ToonEncodingException ex)
{
    // Circular reference or MaxDepth exceeded
    Console.WriteLine($"Encoding error: {ex.Message}");
    Console.WriteLine($"Property path: {ex.PropertyPath}");  // e.g. $.Children[0].Parent
}
```

`ToonSerializationException` is thrown by deserialization (type mismatches), not by `Serialize`. A `[ToonConverter]`
type that does not implement `IToonConverter` throws `InvalidOperationException`.

## Common Issues

### Issue: Circular References

**Problem**: Object graph contains circular references.

**Solution**: ToonNet detects the cycle and throws `ToonEncodingException` with the path of the back-reference
(for example `$.Children[0].Parent`). Break the cycle:

```csharp
using ToonNet.Core.Serialization.Attributes;

public class Parent
{
    public string Name { get; set; }
    public List<Child> Children { get; set; }
}

public class Child
{
    public string Name { get; set; }
    // Don't serialize parent reference
    [ToonIgnore]
    public Parent Parent { get; set; }
}
```

### Issue: Large Objects

**Problem**: Serializing very large objects causes memory issues.

**Solution**: Write directly to a file or stream, and for large collections stream the items:

```csharp
await using var fileStream = File.Create("large-data.toon");
await ToonSerializer.SerializeToStreamAsync(largeObject, fileStream);

// Large collections: one document per item, never materialized as a whole
await ToonSerializer.SerializeStreamAsync(GetItemsAsync(), "items.toon");
```

`SerializeToStreamAsync` still builds the whole TOON string in memory before writing it.

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
