# Deserialization

Complete guide to deserializing TOON format back to .NET objects using `ToonSerializer`.

## Overview

Deserialization converts TOON format strings, streams, or files back to strongly-typed .NET objects. ToonNet provides type-safe deserialization with full generic support.

## Basic Deserialization

### Deserialize from String

```csharp
using ToonNet.Core;

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
using var stream = File.OpenRead("data.toon");
var data = ToonSerializer.Deserialize<MyData>(stream);

// Or with explicit encoding
using var stream = File.OpenRead("data.toon");
var data = ToonSerializer.Deserialize<MyData>(stream, Encoding.UTF8);
```

### Deserialize from File

```csharp
// Synchronous
var config = ToonSerializer.DeserializeFromFile<AppConfig>("appsettings.toon");

// Asynchronous
var config = await ToonSerializer.DeserializeFromFileAsync<AppConfig>("appsettings.toon");
```

## Deserializing Different Types

### Primitive Types

```csharp
int number = ToonSerializer.Deserialize<int>("42");
double price = ToonSerializer.Deserialize<double>("19.99");
bool flag = ToonSerializer.Deserialize<bool>("true");
string text = ToonSerializer.Deserialize<string>("Hello World");
```

### Collections

#### Arrays

```csharp
string toonInput = """
- 10
- 20
- 30
- 40
- 50
""";

int[] numbers = ToonSerializer.Deserialize<int[]>(toonInput);
// Result: [10, 20, 30, 40, 50]
```

#### Lists

```csharp
string toonInput = """
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
string toonInput = """
- Name: Alice
  Department: Engineering
  Salary: 85000
- Name: Bob
  Department: Marketing
  Salary: 65000
- Name: Charlie
  Department: Sales
  Salary: 70000
""";

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
```

## Deserialization Options

### Using ToonSerializerOptions

```csharp
var options = new ToonSerializerOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
    AllowTrailingCommas = true,
    CaseSensitive = false
};

Person person = ToonSerializer.Deserialize<Person>(toonInput, options);
```

### Case-Insensitive Deserialization

```csharp
string toonInput = """
name: Alice
AGE: 30
eMaIl: alice@example.com
""";

var options = new ToonSerializerOptions { CaseSensitive = false };
Person person = ToonSerializer.Deserialize<Person>(toonInput, options);
// Works! Property names matched case-insensitively
```

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
Teams:
  - Backend
  - Frontend
  - DevOps
Projects:
  - Name: Project A
    Members:
      - Alice
      - Bob
  - Name: Project B
    Members:
      - Charlie
      - David
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
Employees:
  - Name: Alice
    Position: Engineer
    Skills:
      C#: 9
      Python: 7
    Projects:
      - Name: Project X
        Status: Active
      - Name: Project Y
        Status: Completed
  - Name: Bob
    Position: Designer
    Skills:
      Figma: 8
      Photoshop: 9
    Projects:
      - Name: Project Z
        Status: Active
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

// Or deserialize to base type (requires type information in data)
Shape shape = ToonSerializer.Deserialize<Shape>(toonInput);
```

## Async Deserialization

### Deserialize from Stream (Async)

```csharp
using var fileStream = File.OpenRead("data.toon");
var data = await ToonSerializer.DeserializeAsync<MyData>(fileStream);
```

### Deserialize from File (Async)

```csharp
var config = await ToonSerializer.DeserializeFromFileAsync<AppConfig>("appsettings.toon");

// With options
var options = new ToonSerializerOptions { CaseSensitive = false };
var config = await ToonSerializer.DeserializeFromFileAsync<AppConfig>("appsettings.toon", options);
```

## Type Inference

ToonNet automatically infers types during deserialization:

```csharp
// Number types
int intValue = ToonSerializer.Deserialize<int>("42");
long longValue = ToonSerializer.Deserialize<long>("9999999999");
double doubleValue = ToonSerializer.Deserialize<double>("3.14159");
decimal decimalValue = ToonSerializer.Deserialize<decimal>("19.99");

// Boolean
bool flag = ToonSerializer.Deserialize<bool>("true");

// DateTime
DateTime date = ToonSerializer.Deserialize<DateTime>("2026-01-24T17:00:00");

// Guid
Guid id = ToonSerializer.Deserialize<Guid>("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
```

## All Deserialize Methods

| Method | Description | Use Case |
|--------|-------------|----------|
| `Deserialize<T>(string)` | Deserialize from string | Simple, in-memory data |
| `Deserialize<T>(string, ToonSerializerOptions)` | Deserialize with options | Custom parsing |
| `Deserialize<T>(Stream)` | Deserialize from stream | Large files, network data |
| `Deserialize<T>(Stream, Encoding)` | Deserialize with encoding | Non-UTF8 encoding |
| `DeserializeAsync<T>(Stream)` | Async stream deserialization | Async I/O |
| `DeserializeAsync<T>(Stream, ToonSerializerOptions)` | Async with options | Async + custom options |
| `DeserializeFromFile<T>(string)` | Deserialize from file | File input |
| `DeserializeFromFileAsync<T>(string)` | Async file deserialization | Async file I/O |

## Error Handling

```csharp
try
{
    var person = ToonSerializer.Deserialize<Person>(toonInput);
}
catch (ToonParseException ex)
{
    Console.WriteLine($"Parse error at line {ex.Line}, column {ex.Column}");
    Console.WriteLine($"Expected: {ex.ExpectedToken}");
    Console.WriteLine($"Got: {ex.ActualToken}");
}
catch (ToonSerializationException ex)
{
    Console.WriteLine($"Deserialization failed: {ex.Message}");
    Console.WriteLine($"Property: {ex.PropertyName}");
    Console.WriteLine($"Target type: {ex.TargetType}");
}
```

## Common Issues

### Issue: Type Mismatch

**Problem**: TOON value type doesn't match target C# type.

**Solution**: Ensure types are compatible:

```csharp
// Wrong: trying to deserialize string to int
string toonInput = "Age: NotANumber";
// Throws ToonSerializationException

// Correct:
string toonInput = "Age: 30";
var person = ToonSerializer.Deserialize<Person>(toonInput);
```

### Issue: Missing Properties

**Problem**: TOON input missing required properties.

**Solution**: Make properties nullable or provide defaults:

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
3. **Stream large files**: Don't load entire file into memory
4. **Use specific types**: Avoid `object` or `dynamic`
5. **Profile deserialization**: Use BenchmarkDotNet

```csharp
// Good: Reuse options
private static readonly ToonSerializerOptions _options = new()
{
    CaseSensitive = false,
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
