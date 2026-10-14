# Basic Serialization

Learn the fundamentals of TOON serialization with practical examples. The samples need `using ToonNet.Core.Serialization;`
(and `using ToonNet.Core;` for the exception types); put the types they declare after the statements, or in their own
files.

## Primitive Types

### Strings

```csharp
string text = "Hello, ToonNet!";
string toon = ToonSerializer.Serialize(text);
// Output: "Hello, ToonNet!"
// (quoted because it contains the delimiter ','; plain strings like Hello World are written unquoted)

string? restored = ToonSerializer.Deserialize<string>(toon);
```

### Numbers

```csharp
int number = 42;
string toon = ToonSerializer.Serialize(number);
// Output: 42

double price = 19.99;
string toonPrice = ToonSerializer.Serialize(price);
// Output: 19.99
```

### Booleans

```csharp
bool isActive = true;
string toon = ToonSerializer.Serialize(isActive);
// Output: true

bool restored = ToonSerializer.Deserialize<bool>("false");
```

### Null Values

```csharp
string? nullValue = null;
string toon = ToonSerializer.Serialize(nullValue);
// Output: null

string? restored = ToonSerializer.Deserialize<string?>("null");
```

## Collections

### Arrays

```csharp
int[] numbers = { 1, 2, 3, 4, 5 };
string toon = ToonSerializer.Serialize(numbers);
```

**Output:**
```toon
[5]: 1,2,3,4,5
```

### Lists

```csharp
List<string> fruits = new() { "Apple", "Banana", "Cherry" };
string toon = ToonSerializer.Serialize(fruits);
```

**Output:**
```toon
[3]: Apple,Banana,Cherry
```

### Dictionaries

```csharp
Dictionary<string, int> scores = new()
{
    ["Alice"] = 95,
    ["Bob"] = 87,
    ["Charlie"] = 92
};

string toon = ToonSerializer.Serialize(scores);
```

**Output:**
```toon
Alice: 95
Bob: 87
Charlie: 92
```

## Complex Objects

### Simple Object

```csharp
var product = new Product
{
    Name = "Laptop",
    Price = 999.99m,
    InStock = true
};

string toon = ToonSerializer.Serialize(product);

public class Product
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}
```

**Output:**
```toon
Name: Laptop
Price: 999.99
InStock: true
```

### Nested Objects

```csharp
var customer = new Customer
{
    Name = "John Doe",
    Address = new Address
    {
        Street = "123 Main St",
        City = "New York",
        ZipCode = "10001"
    }
};

string toon = ToonSerializer.Serialize(customer);

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class Customer
{
    public string Name { get; set; } = "";
    public Address Address { get; set; }
}
```

**Output:**
```toon
Name: John Doe
Address:
  Street: 123 Main St
  City: New York
  ZipCode: "10001"
```

Strings that would otherwise read as a number, boolean or `null` (such as `"10001"`) are quoted so they stay strings.

### Collections of Objects

```csharp
var employees = new List<Employee>
{
    new() { Name = "Alice", Department = "Engineering", Salary = 85000 },
    new() { Name = "Bob", Department = "Marketing", Salary = 65000 },
    new() { Name = "Charlie", Department = "Sales", Salary = 70000 }
};

string toon = ToonSerializer.Serialize(employees);

public class Employee
{
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public int Salary { get; set; }
}
```

**Output:**
```toon
[3]{Name,Department,Salary}:
  Alice,Engineering,85000
  Bob,Marketing,65000
  Charlie,Sales,70000
```

## Nullable Types

### Nullable Value Types

```csharp
var example1 = new NullableExample { Age = 25, BirthDate = null };
string toon1 = ToonSerializer.Serialize(example1);
// Output:
// Age: 25
// BirthDate: null

var example2 = new NullableExample { Age = null, BirthDate = new DateTime(2026, 1, 24, 17, 0, 0, DateTimeKind.Utc) };
string toon2 = ToonSerializer.Serialize(example2);
// Output:
// Age: null
// BirthDate: "2026-01-24T17:00:00Z"

public class NullableExample
{
    public int? Age { get; set; }
    public DateTime? BirthDate { get; set; }
}
```

### Nullable Reference Types

```csharp
var person = new Person
{
    Name = "John",
    MiddleName = null,
    Email = "john@example.com"
};

string toon = ToonSerializer.Serialize(person);

public class Person
{
    public string Name { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Email { get; set; }
}
```

**Output:**
```toon
Name: John
MiddleName: null
Email: john@example.com
```

## Enums

### Simple Enum

```csharp
var task = new TodoItem { Title = "Review PR", Status = Status.Active };
string toon = ToonSerializer.Serialize(task);

public enum Status
{
    Pending,
    Active,
    Completed,
    Cancelled
}

public class TodoItem
{
    public string Title { get; set; } = "";
    public Status Status { get; set; }
}
```

**Output:**
```toon
Title: Review PR
Status: Active
```

### Enum Flags

```csharp
var permissions = Permissions.Read | Permissions.Write;
string toon = ToonSerializer.Serialize(permissions);
// Output: "Read, Write"

[Flags]
public enum Permissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    Delete = 8
}
```

## DateTime and DateTimeOffset

```csharp
var evt = new Event
{
    Name = "Conference 2026",
    EventDate = new DateTime(2026, 06, 15, 9, 0, 0),
    RegisteredAt = new DateTimeOffset(2026, 1, 24, 17, 0, 0, TimeSpan.Zero)
};

string toon = ToonSerializer.Serialize(evt);

public class Event
{
    public string Name { get; set; } = "";
    public DateTime EventDate { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
}
```

**Output:**
```toon
Name: Conference 2026
EventDate: "2026-06-15T09:00:00"
RegisteredAt: "2026-01-24T17:00:00+00:00"
```

## Common Patterns

### Optional Properties

```csharp
var user = new User
{
    Id = 123,
    Username = "alice",
    Bio = "Software Engineer"
    // Website is null
};

string toon = ToonSerializer.Serialize(user);

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string? Bio { get; set; }  // Optional
    public string? Website { get; set; }  // Optional
}
```

**Output:**
```toon
Id: 123
Username: alice
Bio: Software Engineer
Website: null
```

### Nested Collections

```csharp
var dept = new Department
{
    Name = "Engineering",
    Teams = new List<string> { "Backend", "Frontend", "DevOps" }
};

string toon = ToonSerializer.Serialize(dept);

public class Department
{
    public string Name { get; set; } = "";
    public List<string> Teams { get; set; }
}
```

**Output:**
```toon
Name: Engineering
Teams[3]: Backend,Frontend,DevOps
```

## Deserialization Examples

### From String

```csharp
string toonInput = """
Name: Alice
Email: alice@example.com
""";

var person = ToonSerializer.Deserialize<Person>(toonInput);   // the Person of "Nullable Reference Types"
```

### From File

```csharp
// Async file deserialization; AppConfig is your settings class
var config = await ToonSerializer.DeserializeFromFileAsync<AppConfig>("appsettings.toon");
```

### From Stream

```csharp
using System.Text;

using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Alice\nEmail: alice@example.com"));
var person = await ToonSerializer.DeserializeFromStreamAsync<Person>(stream);   // also an HTTP response or file stream
```

## Error Handling

```csharp
using ToonNet.Core;
using ToonNet.Core.Serialization;

try
{
    var person = ToonSerializer.Deserialize<Person>("Name: Alice\nAge: abc");
}
catch (ToonParseException ex)
{
    // Invalid TOON text (strict mode is on by default)
    Console.WriteLine($"Parse error at line {ex.Line}, column {ex.Column}");
}
catch (ToonSerializationException ex)
{
    // Valid TOON that does not fit the target type, e.g. "Age: abc"
    Console.WriteLine($"Serialization error: {ex.Message}");
    Console.WriteLine($"Path: {ex.Path}, Type: {ex.TargetType}");  // Path: $.Age, Type: System.Int32
}

public class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}
```

## Best Practices

1. **Use nullable types** for optional properties
2. **Initialize collections** to avoid null checks
3. **Use record types** for immutable data
4. **Handle exceptions** appropriately in production
5. **Use async methods** for I/O operations
6. **Use streaming API** for large datasets (millions of records)

## Next Steps

- **[Serialization](../core-features/serialization)**: Deep dive into serialization options and streaming API
- **[Deserialization](../core-features/deserialization)**: Advanced deserialization techniques
- **[Configuration](../core-features/configuration)**: Customize serialization behavior
- **[Performance Tuning](../advanced/performance-tuning)**: Optimize for large datasets with streaming
