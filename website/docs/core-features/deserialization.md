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

### Deserialize from UTF-8

Bytes from the network, a file or a cache can be read without turning them into a string first. The text is decoded
into a pooled buffer that the parser reads in place, and the buffer is cleared before it goes back to the pool. A
byte order mark at the start is ignored; ill-formed UTF-8 is a `ToonParseException`.

```csharp
byte[] utf8 = await httpClient.GetByteArrayAsync(url);
var person = ToonSerializer.Deserialize<Person>(utf8);          // ReadOnlySpan<byte>

var fromChars = ToonSerializer.Deserialize<Person>(text.AsSpan(10, 200));  // ReadOnlySpan<char>
var fromStream = ToonSerializer.Deserialize<Person>(stream);    // read to the end, synchronously
```

Each also has an overload with a `Type`. On 10,000 records, reading bytes this way takes about the same time as decoding
them to a string and calling `Deserialize<T>(string)` (−6% to +1%), with 18–46% less allocation; a large document no longer needs a
string twice its size on the large object heap.

### Deserialize from Stream

```csharp
await using var stream = File.OpenRead("data.toon");
var data = await ToonSerializer.DeserializeFromStreamAsync<MyData>(stream);  // read as UTF-8
```

### Deserialize from File

```csharp
var config = await ToonSerializer.DeserializeFromFileAsync<AppConfig>("appsettings.toon");  // read as UTF-8
```

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
EventDate: 2026-06-15T09:00:00
RegisteredAt: 2026-01-24T17:00:00+00:00
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
gives `Unspecified`, and an offset gives `Local` (converted to the local time zone; use `DateTimeOffset` to keep the
offset). `ToonSerializer` writes dates in the round-trip `O` form without trailing zeros in the fraction
(`2026-06-15T09:00:00`, `2026-06-15T09:00:00Z`, `2026-01-24T17:00:00+00:00`), quoted because they contain `:`.

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

### Key Matching Is Case-Sensitive by Default

Keys must match the property name exactly after the naming policy (or the `[ToonProperty]` name) is applied: with the
default options, `name: Alice` does not set `Name`, and the property keeps its default value.

Documents from other producers and LLMs often differ in case. Set `PropertyNameCaseInsensitive` to match keys to
property and constructor parameter names ignoring case:

```csharp
var options = new ToonSerializerOptions { PropertyNameCaseInsensitive = true };

Person person = ToonSerializer.Deserialize<Person>("name: Alice\nAGE: 30", options);
// person.Name = "Alice", person.Age = 30
```

A key that matches exactly is preferred; properties whose names differ only in case (`Id` and `ID`) still need an
exact match, and when several keys fill the same property the last one wins. Generated code applies the option too, so
the overloads that take a `ToonTypeInfo` support it (code generated before ToonNet 3.3 leaves it to reflection; rebuild
with the current generator). Writing is not affected.

### Unknown Keys and Required Properties

Keys that match no property are ignored by default. A `required` property without a key is an error
(`RespectRequiredMembers` is `true` by default since ToonNet 4.0; set it to `false` to keep the default value instead).
To report unknown keys too, set `UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow`
(see [Configuration](configuration#strict-reading)):

```csharp
var options = new ToonSerializerOptions { UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow };

ToonSerializer.Deserialize<Person>("Name: Alice\nAeg: 30", options);
// ToonSerializationException: The key matches no property or constructor parameter (path: $.Aeg, target type: Person)
```

The key appears only in the path, escaped and cut to 64 characters (`$['first name']` for a key that is not a simple
identifier).

### Non-Public Setters

Reading sets only public setters (`set` and `init`). A property with a `private`, `protected` or `internal` setter
(`IsAdmin { get; private set; }`) is not set from a document unless it is marked `[ToonInclude]`; see
[Configuration](configuration#non-public-setters-and-tooninclude).

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
| `Deserialize<T>(ReadOnlySpan<byte> utf8, options)` | Deserialize UTF-8 bytes without a string (also with a `Type`) | Network, caches, byte APIs |
| `Deserialize<T>(ReadOnlySpan<char> toon, options)` | Deserialize a span of characters (also with a `Type`) | Slices of larger text |
| `Deserialize<T>(Stream, options)` | Read UTF-8 from a stream synchronously (also with a `Type`) | Synchronous I/O |
| `Deserialize<T>(string \| bytes \| chars \| Stream, ToonTypeInfo<T>, options)` | Without reflection, through generated code (also `DeserializeFromStreamAsync`) | Trimming, NativeAOT |
| `DeserializeFromValue<T>(ToonValue value, options)` | Convert a `ToonValue` tree without text (also with a `ToonTypeInfo`) | Data from `ToonDocument.Parse` or `SerializeToValue` |
| `DeserializeAt<T>(toon, "$.data.orders", options)` | The value at a path only, skipping the rest (string, bytes, `DeserializeAtAsync` for streams) | Response envelopes, one section of a large document; see [Querying](querying) |
| `DeserializeAll<T>(toon, "$.orders[?@.total > 100]", options)` | Every value a path selects, straight from the text | Filtering rows without reading them all |
| `Deserialize<T>(ToonDocumentReader reader, options)` | The value a reader is on | Mixing token reading and objects; see [Reader and Writer](reader-writer) |
| `DeserializeAsync<T>(string toon, options, ct)` | Deserialize from string (`ValueTask<T?>`) | Async call sites |
| `DeserializeFromStreamAsync<T>(Stream, options, ct)` | Read UTF-8 from a stream (also with a `Type`) | Network data |
| `DeserializeFromFileAsync<T>(string filePath, options, ct)` | Read UTF-8 from a file | File input |
| `DeserializeDocumentsAsync<T>(string filePath \| StreamReader, ...)` | Yield one object per document | Multi-document files, see [Streaming](streaming) |
| `DeserializeAsyncEnumerable<T>(Stream, options, ct)` / `DeserializeAsyncEnumerableAt<T>(Stream, path, ...)` | Yield the items of a root array, or of the array at a path | Large arrays, see [Streaming](streaming) |

## Parsing Rules and Strict Mode

ToonNet decodes TOON as defined by spec v4.4 and passes all of its decode conformance fixtures.

- **Type inference:** unquoted `true`, `false` and `null` are literals; tokens matching the JSON number grammar are numbers;
  everything else is a string. Leading zeros make a token a string (`zip: 01234` stays `"01234"`), and so does any other text
  such as `(5)` or `1.2.3`. Quoted values are always strings.
- **Arrays need headers:** `tags[3]: a,b,c`, `users[2]{id,name}:` followed by rows, or `items[2]:` followed by `- ` items.
  Empty arrays are written `key: []` (or `- []` as a list item). A bare `key:` always opens an object.
- **Nested field groups and keyed tables:** `orders[2]{id,customer{name,country}}:` reads each row into nested objects,
  and `servers[2:]{host,port}:` followed by `alpha: a.example.com,8080` rows reads an object keyed by the entry keys
  (bind it to a `Dictionary<string, T>` or a class with those properties).
- **Comments:** lines whose first non-space character is `#` are ignored wherever they appear.
- **Delimiters:** each header declares its own delimiter (`[3]` comma, `[3|]` pipe, `[3<TAB>]` tab).
- **Precision:** numbers that fit in a `decimal` keep their exact value, so `long` and `decimal` properties round-trip exactly.

`ToonOptions.StrictMode` is `true` by default. Strict mode reports every error of spec §14:

Non-strict mode applies exactly the five recoveries of spec §14.4; everything else is an error in both modes
(malformed headers, rows with the wrong number of cells, list items under a bare `key:`, invalid escapes, ...).

| Rule | Strict (default) | Non-strict |
|------|------------------|------------|
| `[N]` must match the number of items, rows and entry rows | error | not checked; every row is read |
| Duplicate keys, or a field name repeated in a header | error | last value wins |
| Indentation must be a multiple of `IndentSize`; no tabs | error | depth = tabs + spaces / `IndentSize` |
| Blank lines inside an array or keyed table | error | ignored |
| The first line of a nested scope indented too deep | error | that depth becomes the scope's depth |

```csharp
var lenient = new ToonSerializerOptions
{
    ToonOptions = new ToonOptions { StrictMode = false }
};

var config = ToonSerializer.Deserialize<AppConfig>(handWrittenToon, lenient);
```

Use non-strict mode only for documents you trust. With duplicate keys the last value wins, so a document can read
differently here than in a strict reader or a check that looked at the first key; keep strict mode for request bodies
and other untrusted input (see [Security](../advanced/security)).

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
    Console.WriteLine($"Path: {ex.Path}");        // e.g. $.Items[2].Price
    Console.WriteLine($"Target type: {ex.TargetType}");
}
```

Both derive from `ToonException`. Keys in paths and messages are escaped and cut to 64 characters, so a key with a line
break or a control character cannot forge a log line. When the target type's constructor throws, the message names only
the exception's type (`The constructor of Money threw ArgumentException`); the exception itself is the
`InnerException`, so its message does not reach clients through model-state errors. Text nested deeper than `ToonOptions.MaxDepth` is a `ToonParseException` (with the
line and column); exceeding `ToonSerializerOptions.MaxDepth` while converting is a `ToonSerializationException` (with the
path).

## Common Issues

### Issue: Type Mismatch

**Problem**: TOON value type doesn't match target C# type.

**Solution**: Ensure types are compatible:

```csharp
// Wrong: trying to deserialize string to int
string toonInput = "Age: NotANumber";
// Throws ToonSerializationException: "The string is not a number (path: $.Age, target type: Int32)"

// Correct:
string toonInput = "Age: 30";
var person = ToonSerializer.Deserialize<Person>(toonInput);
```

### Issue: Missing Properties

**Problem**: TOON input missing properties.

**Solution**: Missing keys leave the property at its initial value; make properties nullable or provide defaults. A
property declared `required` is the exception: its key must be present unless `RespectRequiredMembers` is `false`.

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
3. **Stream multi-document files**: `DeserializeDocumentsAsync` keeps one document in memory at a time
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
- A `ToonEncoder` instance keeps no per-call state and can be shared across threads (it is registered as a singleton by `AddToonNet`).
- Shared metadata/name caches use `ConcurrentDictionary` for concurrent access.
- Cache entries are created on demand and retained for the process lifetime (no eviction).
- Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.
