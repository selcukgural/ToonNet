# Deserialization

Complete guide to deserializing TOON format back to .NET objects using `ToonSerializer`.

## Overview

Deserialization converts TOON format strings, streams, or files back to strongly-typed .NET objects. ToonNet provides type-safe deserialization with full generic support.

## Basic Deserialization

### Deserialize from String

```csharp
using ToonNet.Core.Serialization;

string toonInput = """
Name: Alice
Age: 30
Email: alice@example.com
""";

Person person = ToonSerializer.Deserialize<Person>(toonInput);

Console.WriteLine($"{person.Name} is {person.Age} years old");
// Output: Alice is 30 years old
```

### Deserialize from Stream

```csharp
await using var stream = File.OpenRead("data.toon");
var data = await ToonSerializer.DeserializeFromStreamAsync<MyData>(stream);  // read as UTF-8
```

### Deserialize from File

```csharp
var config = await ToonSerializer.DeserializeFromFileAsync<AppConfig>("appsettings.toon");  // read as UTF-8
```

There are no synchronous stream or file overloads; read the text yourself and call `Deserialize<T>(string)` if you
need one.

## Deserializing Different Types

### Primitive Types

```csharp
int number = ToonSerializer.Deserialize<int>("42");
double price = ToonSerializer.Deserialize<double>("19.99");
bool flag = ToonSerializer.Deserialize<bool>("true");
string text = ToonSerializer.Deserialize<string>("Hello World");
```

A root primitive that contains `: ` or looks like `key: value` is read as an object, so quote such strings
(`"\"10:30\""`).

### Collections

#### Arrays

```csharp
string toonInput = "[5]: 10,20,30,40,50";

int[] numbers = ToonSerializer.Deserialize<int[]>(toonInput);
// Result: [10, 20, 30, 40, 50]
```

#### Lists

```csharp
string toonInput = """
[3]:
  - Apple
  - Banana
  - Cherry
""";

List<string> fruits = ToonSerializer.Deserialize<List<string>>(toonInput);
```

#### Dictionaries

```csharp
string toonInput = """
FirstName: Alice
LastName: Smith
Age: 30
""";

Dictionary<string, object> data = ToonSerializer.Deserialize<Dictionary<string, object>>(toonInput);
// Values: "Alice", "Smith", 30L (integers are read as long into object)

// Typed dictionary
Dictionary<string, int> scores = ToonSerializer.Deserialize<Dictionary<string, int>>("""
Math: 95
Physics: 87
Chemistry: 92
""");
```

### Complex Objects

```csharp
string toonInput = """
Name: John Doe
Address:
  Street: 123 Main St
  City: New York
  ZipCode: 10001
Age: 35
""";

public class Person
{
    public string Name { get; set; }
    public Address Address { get; set; }
    public int Age { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

Person person = ToonSerializer.Deserialize<Person>(toonInput);
```

### Collections of Objects

```csharp
// Tabular form (what ToonSerializer writes for uniform objects)
string toonInput = """
[3]{Name,Department,Salary}:
  Alice,Engineering,85000
  Bob,Marketing,65000
  Charlie,Sales,70000
""";

// The list form is read as well:
// [3]:
//   - Name: Alice
//     Department: Engineering
//     Salary: 85000
//   ...

public class Employee
{
    public string Name { get; set; }
    public string Department { get; set; }
    public int Salary { get; set; }
}

List<Employee> employees = ToonSerializer.Deserialize<List<Employee>>(toonInput);
```

## Nullable Types

### Nullable Value Types

```csharp
string toonInput = """
Age: 25
BirthDate: null
""";

public class Person
{
    public int? Age { get; set; }
    public DateTime? BirthDate { get; set; }
}

Person person = ToonSerializer.Deserialize<Person>(toonInput);
// person.Age = 25
// person.BirthDate = null
```

### Nullable Reference Types

```csharp
string toonInput = """
Name: John
MiddleName: null
Email: john@example.com
""";

public class User
{
    public string Name { get; set; }
    public string? MiddleName { get; set; }
    public string? Email { get; set; }
}

User user = ToonSerializer.Deserialize<User>(toonInput);
```

## Enum Deserialization

### Simple Enum

```csharp
public enum Status
{
    Pending,
    Active,
    Completed,
    Cancelled
}

public class Task
{
    public string Title { get; set; }
    public Status Status { get; set; }
}

string toonInput = """
Title: Review PR
Status: Active
""";

Task task = ToonSerializer.Deserialize<Task>(toonInput);
// task.Status = Status.Active
```

### Enum Flags

```csharp
[Flags]
public enum Permissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    Delete = 8
}

string toonInput = "Read, Write";
Permissions permissions = ToonSerializer.Deserialize<Permissions>(toonInput);
// permissions = Permissions.Read | Permissions.Write
```

Enum names are matched case-insensitively (`active` works), and numeric values (`Status: 1`) are accepted.
Enums are written as their member names (flags as `"Read, Write"`).

## DateTime Deserialization

```csharp
string toonInput = """
Name: Conference 2026
EventDate: 2026-06-15T09:00:00.0000000
RegisteredAt: 2026-01-24T17:00:00.0000000+00:00
""";

public class Event
{
    public string Name { get; set; }
    public DateTime EventDate { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
}

Event evt = ToonSerializer.Deserialize<Event>(toonInput);
// evt.EventDate.Kind == DateTimeKind.Unspecified (no offset in the text)
```

Dates are parsed with the invariant culture, and `DateTimeKind` is preserved: a `Z` suffix gives `Utc`, no suffix
gives `Unspecified`. `ToonSerializer` writes dates in the round-trip `O` format.

## Deserialization Options

### Using ToonSerializerOptions

```csharp
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
    ToonOptions = new ToonOptions { StrictMode = true }
};

Person person = ToonSerializer.Deserialize<Person>(toonInput, options);
```

### Key Matching Is Case-Sensitive

Keys must match the property name exactly after the naming policy (or the `[ToonProperty]` name) is applied. There is
no case-insensitive option: with the default policy, `name: Alice` does not set `Name`, and the property keeps its
default value.

### Property Naming Policy

Match serialized names with different casing:

```csharp
public class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

// Input uses camelCase
string toonInput = """
firstName: Alice
lastName: Smith
""";

var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase
};

Person person = ToonSerializer.Deserialize<Person>(toonInput, options);
```

## Advanced Deserialization

### Generic Collections

```csharp
// List<T>
List<Person> people = ToonSerializer.Deserialize<List<Person>>(toonInput);

// Dictionary<TKey, TValue>
Dictionary<string, Person> personMap = ToonSerializer.Deserialize<Dictionary<string, Person>>(toonInput);

// IEnumerable<T>
IEnumerable<string> items = ToonSerializer.Deserialize<IEnumerable<string>>(toonInput);

// HashSet<T>
HashSet<int> uniqueIds = ToonSerializer.Deserialize<HashSet<int>>(toonInput);
```

### Nested Collections

```csharp
string toonInput = """
Name: Engineering
Teams[3]: Backend,Frontend,DevOps
Projects[2]:
  - Name: Project A
    Members[2]: Alice,Bob
  - Name: Project B
    Members[2]: Charlie,David
""";

public class Department
{
    public string Name { get; set; }
    public List<string> Teams { get; set; }
    public List<Project> Projects { get; set; }
}

public class Project
{
    public string Name { get; set; }
    public List<string> Members { get; set; }
}

Department dept = ToonSerializer.Deserialize<Department>(toonInput);
```

### Complex Nested Structures

```csharp
string toonInput = """
Name: TechCorp
Employees[2]:
  - Name: Alice
    Position: Engineer
    Skills:
      "C#": 9
      Python: 7
    Projects[2]{Name,Status}:
      Project X,Active
      Project Y,Completed
  - Name: Bob
    Position: Designer
    Skills:
      Figma: 8
      Photoshop: 9
    Projects[1]{Name,Status}:
      Project Z,Active
""";

public class Company
{
    public string Name { get; set; }
    public List<Employee> Employees { get; set; }
}

public class Employee
{
    public string Name { get; set; }
    public string Position { get; set; }
    public Dictionary<string, int> Skills { get; set; }
    public List<Project> Projects { get; set; }
}

public class Project
{
    public string Name { get; set; }
    public string Status { get; set; }
}

Company company = ToonSerializer.Deserialize<Company>(toonInput);
```

### Polymorphic Deserialization

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

string toonInput = """
Color: Red
Radius: 5.0
""";

// Deserialize to specific type
Circle circle = ToonSerializer.Deserialize<Circle>(toonInput);

// Deserializing to the abstract base type throws ToonSerializationException
// ("Cannot create an instance of the abstract type or interface Shape; register a converter for it").
// TOON carries no type information; use a custom converter to pick the concrete type.
```

## Async Deserialization

### Deserialize from Stream (Async)

```csharp
await using var fileStream = File.OpenRead("data.toon");
var data = await ToonSerializer.DeserializeFromStreamAsync<MyData>(fileStream);
```

### Deserialize from File (Async)

```csharp
var config = await ToonSerializer.DeserializeFromFileAsync<AppConfig>("appsettings.toon");

// With options
var options = new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase };
var config2 = await ToonSerializer.DeserializeFromFileAsync<AppConfig>("appsettings.toon", options);
```

`DeserializeAsync<T>(string)` also exists; it parses synchronously and returns a completed `ValueTask<T?>`.

## Type Inference

The TOON value is converted to the requested target type:

```csharp
// Number types
int intValue = ToonSerializer.Deserialize<int>("42");
long longValue = ToonSerializer.Deserialize<long>("9999999999");
double doubleValue = ToonSerializer.Deserialize<double>("3.14159");
decimal decimalValue = ToonSerializer.Deserialize<decimal>("19.99");

// Boolean
bool flag = ToonSerializer.Deserialize<bool>("true");

// DateTime
DateTime date = ToonSerializer.Deserialize<DateTime>("\"2026-01-24T17:00:00\"");  // quoted: contains ':'

// Guid
Guid id = ToonSerializer.Deserialize<Guid>("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
```

## All Deserialize Methods

All methods take an optional `ToonSerializerOptions? options = null`; the async ones also take an optional
`CancellationToken`.

| Method | Description | Use Case |
|--------|-------------|----------|
| `Deserialize<T>(string toon, options)` | Deserialize from string | Simple, in-memory data |
| `Deserialize(string toon, Type type, options)` | Same, with a runtime type | Non-generic code |
| `DeserializeFromValue<T>(ToonValue value, options)` | Convert a `ToonValue` tree without text | Data from `ToonDocument.Parse` or `SerializeToValue` |
| `DeserializeAsync<T>(string toon, options, ct)` | Deserialize from string (`ValueTask<T?>`) | Async call sites |
| `DeserializeFromStreamAsync<T>(Stream, options, ct)` | Read UTF-8 from a stream | Network data |
| `DeserializeFromFileAsync<T>(string filePath, options, ct)` | Read UTF-8 from a file | File input |
| `DeserializeStreamAsync<T>(string filePath \| StreamReader, ...)` | Yield one object per document | Multi-document files, see [Streaming](streaming) |

## Parsing Rules and Strict Mode

ToonNet decodes TOON as defined by spec v3.3.2 and passes all of its decode conformance fixtures.

- **Type inference:** unquoted `true`, `false` and `null` are literals; tokens matching the JSON number grammar are numbers;
  everything else is a string. Leading zeros make a token a string (`zip: 01234` stays `"01234"`), and so does any other text
  such as `(5)` or `1.2.3`. Quoted values are always strings.
- **Arrays need headers:** `tags[3]: a,b,c`, `users[2]{id,name}:` followed by rows, or `items[2]:` followed by `- ` items.
  Empty arrays are written `key: []`. A bare `key:` always opens an object.
- **Delimiters:** each header declares its own delimiter (`[3]` comma, `[3|]` pipe, `[3<TAB>]` tab).
- **Precision:** numbers that fit in a `decimal` keep their exact value, so `long` and `decimal` properties round-trip exactly.

`ToonOptions.StrictMode` is `true` by default. Strict mode reports every error of spec §14:

| Rule | Strict (default) | Non-strict |
|------|------------------|------------|
| `[N]` must match the number of items, rows and row values | error | not checked |
| Malformed header such as `items[03]:` or `x[bar]:` | error | read as a literal key |
| Duplicate keys in one object | error | last value wins |
| Indentation must be a multiple of `IndentSize`; no tabs | error | depth = spaces / `IndentSize`, a tab counts as `IndentSize` spaces |
| Blank lines inside an array | error | ignored |
| List items under a bare `key:` (no header) | error with a hint | read as an array |

```csharp
var lenient = new ToonSerializerOptions
{
    ToonOptions = new ToonOptions { StrictMode = false }
};

var config = ToonSerializer.Deserialize<AppConfig>(handWrittenToon, lenient);
```

## Error Handling

```csharp
try
{
    var person = ToonSerializer.Deserialize<Person>(toonInput);
}
catch (ToonParseException ex)
{
    // Invalid TOON text, e.g. "Array length mismatch: expected 3, got 2"
    Console.WriteLine($"Parse error at line {ex.Line}, column {ex.Column}");
}
catch (ToonSerializationException ex)
{
    // Valid TOON that does not fit the target type
    Console.WriteLine($"Deserialization failed: {ex.Message}");
    Console.WriteLine($"Path: {ex.PropertyName}");        // e.g. $.Items[2].Price
    Console.WriteLine($"Target type: {ex.TargetType}");
}
```

Both derive from `ToonException`. Exceeding `ToonSerializerOptions.MaxDepth` while converting also throws
`ToonParseException` (with line and column 0).

## Common Issues

### Issue: Type Mismatch

**Problem**: TOON value type doesn't match target C# type.

**Solution**: Ensure types are compatible:

```csharp
// Wrong: trying to deserialize string to int
string toonInput = "Age: NotANumber";
// Throws ToonSerializationException: 'NotANumber' is not a number (Path: $.Age)

// Correct:
string toonInput = "Age: 30";
var person = ToonSerializer.Deserialize<Person>(toonInput);
```

### Issue: Missing Properties

**Problem**: TOON input missing required properties.

**Solution**: Missing keys leave the property at its initial value; make properties nullable or provide defaults:

```csharp
public class Person
{
    public string Name { get; set; } = "Unknown";  // Default value
    public int? Age { get; set; }  // Nullable
}
```

### Issue: Extra Properties

**Problem**: TOON input has properties not in C# class.

**Solution**: ToonNet ignores extra properties by default.

```csharp
string toonInput = """
Name: Alice
Age: 30
ExtraField: Some Value
""";

// Works! ExtraField is ignored
Person person = ToonSerializer.Deserialize<Person>(toonInput);
```

## Performance Tips

1. **Reuse options**: Create `ToonSerializerOptions` once
2. **Use async methods**: For I/O-bound operations
3. **Stream multi-document files**: `DeserializeStreamAsync` keeps one document in memory at a time
   (`DeserializeFromFileAsync` and `DeserializeFromStreamAsync` read the whole input)
4. **Use specific types**: Avoid `object` or `dynamic`
5. **Profile deserialization**: Use BenchmarkDotNet

```csharp
// Good: Reuse options
private static readonly ToonSerializerOptions _options = new()
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase
};

public Person DeserializePerson(string toonInput)
{
    return ToonSerializer.Deserialize<Person>(toonInput, _options);
}
```

## See Also

- **[Serialization](serialization)**: Convert objects to TOON
- **[Type System](type-system)**: Understanding TOON types
- **[Configuration](configuration)**: Detailed options guide
- **[Custom Converters](../advanced/custom-converters)**: Handle custom types

## Thread-Safety

- `ToonSerializer` methods are safe to call concurrently across threads.
- Shared metadata/name caches use `ConcurrentDictionary` for concurrent access.
- Cache entries are created on demand and retained for the process lifetime (no eviction).
- Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.
