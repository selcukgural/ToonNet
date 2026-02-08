# Basic Serialization

Learn the fundamentals of TOON serialization with practical examples.

## Primitive Types

### Strings

```csharp
string text = "Hello, ToonNet!";
string toon = ToonSerializer.Serialize(text);
// Output: Hello, ToonNet!

string restored = ToonSerializer.Deserialize<string>(toon);
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
- 1
- 2
- 3
- 4
- 5
```

### Lists

```csharp
List<string> fruits = new() { "Apple", "Banana", "Cherry" };
string toon = ToonSerializer.Serialize(fruits);
```

**Output:**
```toon
- Apple
- Banana
- Cherry
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
public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

var product = new Product
{
    Name = "Laptop",
    Price = 999.99m,
    InStock = true
};

string toon = ToonSerializer.Serialize(product);
```

**Output:**
```toon
Name: Laptop
Price: 999.99
InStock: true
```

### Nested Objects

```csharp
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

public class Customer
{
    public string Name { get; set; }
    public Address Address { get; set; }
}

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
```

**Output:**
```toon
Name: John Doe
Address:
  Street: 123 Main St
  City: New York
  ZipCode: 10001
```

### Collections of Objects

```csharp
public class Employee
{
    public string Name { get; set; }
    public string Department { get; set; }
    public int Salary { get; set; }
}

var employees = new List<Employee>
{
    new() { Name = "Alice", Department = "Engineering", Salary = 85000 },
    new() { Name = "Bob", Department = "Marketing", Salary = 65000 },
    new() { Name = "Charlie", Department = "Sales", Salary = 70000 }
};

string toon = ToonSerializer.Serialize(employees);
```

**Output:**
```toon
- Name: Alice
  Department: Engineering
  Salary: 85000
- Name: Bob
  Department: Marketing
  Salary: 65000
- Name: Charlie
  Department: Sales
  Salary: 70000
```

## Nullable Types

### Nullable Value Types

```csharp
public class NullableExample
{
    public int? Age { get; set; }
    public DateTime? BirthDate { get; set; }
}

var example1 = new NullableExample { Age = 25, BirthDate = null };
string toon1 = ToonSerializer.Serialize(example1);
// Output:
// Age: 25
// BirthDate: null

var example2 = new NullableExample { Age = null, BirthDate = DateTime.Now };
string toon2 = ToonSerializer.Serialize(example2);
// Output:
// Age: null
// BirthDate: 2026-01-24T17:00:00.0000000Z
```

### Nullable Reference Types

```csharp
public class Person
{
    public string Name { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Email { get; set; }
}

var person = new Person
{
    Name = "John",
    MiddleName = null,
    Email = "john@example.com"
};

string toon = ToonSerializer.Serialize(person);
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

var task = new Task { Title = "Review PR", Status = Status.Active };
string toon = ToonSerializer.Serialize(task);
```

**Output:**
```toon
Title: Review PR
Status: Active
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

var permissions = Permissions.Read | Permissions.Write;
string toon = ToonSerializer.Serialize(permissions);
// Output: Read, Write
```

## DateTime and DateTimeOffset

```csharp
public class Event
{
    public string Name { get; set; }
    public DateTime EventDate { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
}

var evt = new Event
{
    Name = "Conference 2026",
    EventDate = new DateTime(2026, 06, 15, 9, 0, 0),
    RegisteredAt = DateTimeOffset.Now
};

string toon = ToonSerializer.Serialize(evt);
```

**Output:**
```toon
Name: Conference 2026
EventDate: 2026-06-15T09:00:00.0000000
RegisteredAt: 2026-01-24T17:00:00.0000000+00:00
```

## Common Patterns

### Optional Properties

```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string? Bio { get; set; }  // Optional
    public string? Website { get; set; }  // Optional
}

var user = new User
{
    Id = 123,
    Username = "alice",
    Bio = "Software Engineer"
    // Website is null
};

string toon = ToonSerializer.Serialize(user);
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
public class Department
{
    public string Name { get; set; }
    public List<string> Teams { get; set; }
}

var dept = new Department
{
    Name = "Engineering",
    Teams = new List<string> { "Backend", "Frontend", "DevOps" }
};

string toon = ToonSerializer.Serialize(dept);
```

**Output:**
```toon
Name: Engineering
Teams:
  - Backend
  - Frontend
  - DevOps
```

## Deserialization Examples

### From String

```csharp
string toonInput = """
Name: Alice
Age: 30
Email: alice@example.com
""";

var person = ToonSerializer.Deserialize<Person>(toonInput);
```

### From File

```csharp
// Async file deserialization
var config = await ToonSerializer.DeserializeAsync<AppConfig>(
    File.OpenRead("appsettings.toon")
);
```

### From Stream

```csharp
using var stream = new MemoryStream(Encoding.UTF8.GetBytes(toonString));
var data = ToonSerializer.Deserialize<MyData>(stream);
```

## Error Handling

```csharp
try
{
    var person = ToonSerializer.Deserialize<Person>(invalidToonString);
}
catch (ToonParseException ex)
{
    Console.WriteLine($"Parse error at line {ex.Line}, column {ex.Column}");
    Console.WriteLine($"Expected: {ex.ExpectedToken}, Got: {ex.ActualToken}");
}
catch (ToonSerializationException ex)
{
    Console.WriteLine($"Serialization error: {ex.Message}");
    Console.WriteLine($"Property: {ex.PropertyName}, Type: {ex.TargetType}");
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
