# Type System

Understanding the TOON value type system and how to work with `ToonValue` and its subclasses.

## Overview

ToonNet provides a strongly-typed object model for representing TOON values. The `ToonValue` class hierarchy mirrors the TOON specification types.

## ToonValue Hierarchy

```
ToonValue (abstract base class)
├── ToonNull
├── ToonBoolean
├── ToonNumber
├── ToonString
├── ToonObject
└── ToonArray
```

## ToonValue Base Class

`ToonValue` is the abstract base class for all TOON types. It provides:

- **Type property**: Gets the `ToonValueType` enum value
- **Implicit operators**: Convert from/to C# primitive types
- **Indexers**: Access nested values

### ToonValueType Enum

```csharp
public enum ToonValueType
{
    Null,
    Boolean,
    Number,
    String,
    Object,
    Array
}
```

### Check Value Type

```csharp
ToonValue value = ToonSerializer.Deserialize<ToonValue>(toonString);

if (value.Type == ToonValueType.Object)
{
    ToonObject obj = (ToonObject)value;
    // Work with object
}
```

## ToonNull

Represents a null value.

```csharp
ToonNull nullValue = ToonNull.Instance;

// Check if value is null
ToonValue value = ...;
bool isNull = value is ToonNull;
```

## ToonBoolean

Represents a boolean value (true/false).

```csharp
// Create
ToonBoolean trueValue = new ToonBoolean(true);
ToonBoolean falseValue = new ToonBoolean(false);

// Implicit conversion
ToonValue value = true;  // Creates ToonBoolean

// Get value
bool boolValue = ((ToonBoolean)value).Value;
```

## ToonNumber

Represents numeric values (integers and floating-point).

```csharp
// Create from different numeric types
ToonNumber intNum = new ToonNumber(42);
ToonNumber longNum = new ToonNumber(9999999999L);
ToonNumber doubleNum = new ToonNumber(3.14159);
ToonNumber decimalNum = new ToonNumber(19.99m);

// Implicit conversions
ToonValue value = 42;        // int → ToonNumber
ToonValue value = 3.14;      // double → ToonNumber
ToonValue value = 19.99m;    // decimal → ToonNumber

// Get value as different types
ToonNumber num = (ToonNumber)value;
int intValue = num.AsInt32();
long longValue = num.AsInt64();
double doubleValue = num.AsDouble();
decimal decimalValue = num.AsDecimal();

// Check if integer or floating-point
bool isInteger = num.IsInteger;
bool isFloatingPoint = num.IsFloatingPoint;
```

## ToonString

Represents text values.

```csharp
// Create
ToonString str = new ToonString("Hello, ToonNet!");

// Implicit conversion
ToonValue value = "Hello";  // Creates ToonString

// Get value
string textValue = ((ToonString)value).Value;

// Direct implicit operator
string text = (string)value;
```

## ToonObject

Represents key-value pairs (like C# Dictionary or JSON object).

### Creating ToonObject

```csharp
// Empty object
var obj = new ToonObject();

// Add properties
obj["Name"] = "Alice";
obj["Age"] = 30;
obj["Email"] = "alice@example.com";

// Initialize with values
var obj = new ToonObject
{
    ["Name"] = "Alice",
    ["Age"] = 30,
    ["Email"] = "alice@example.com"
};
```

### Accessing Properties

```csharp
// Indexer access
ToonValue nameValue = obj["Name"];
string name = (string)nameValue;

// Check if property exists
bool hasAge = obj.ContainsKey("Age");

// Get all keys
IEnumerable<string> keys = obj.Keys;

// Get all values
IEnumerable<ToonValue> values = obj.Values;

// Iterate
foreach (var kvp in obj)
{
    string key = kvp.Key;
    ToonValue value = kvp.Value;
    Console.WriteLine($"{key}: {value}");
}
```

### Nested Objects

```csharp
var person = new ToonObject
{
    ["Name"] = "John Doe",
    ["Address"] = new ToonObject
    {
        ["Street"] = "123 Main St",
        ["City"] = "New York",
        ["ZipCode"] = "10001"
    }
};

// Access nested value
ToonObject address = (ToonObject)person["Address"];
string city = (string)address["City"];

// Or chain indexers
string city = (string)person["Address"]["City"];
```

### Modifying Properties

```csharp
// Update existing property
obj["Age"] = 31;

// Remove property
obj.Remove("Email");

// Clear all properties
obj.Clear();

// Count properties
int count = obj.Count;
```

## ToonArray

Represents ordered sequences of values (like C# List or JSON array).

### Creating ToonArray

```csharp
// Empty array
var arr = new ToonArray();

// Add items
arr.Add("Apple");
arr.Add("Banana");
arr.Add("Cherry");

// Initialize with values
var arr = new ToonArray { "Apple", "Banana", "Cherry" };

// From collection
var arr = new ToonArray(new[] { 1, 2, 3, 4, 5 });
```

### Accessing Elements

```csharp
// Indexer access
ToonValue firstItem = arr[0];
string fruit = (string)firstItem;

// Count elements
int count = arr.Count;

// Iterate
foreach (ToonValue item in arr)
{
    Console.WriteLine(item);
}

// LINQ
var numbers = arr.Select(v => (int)v).Where(n => n > 10);
```

### Nested Arrays

```csharp
var matrix = new ToonArray
{
    new ToonArray { 1, 2, 3 },
    new ToonArray { 4, 5, 6 },
    new ToonArray { 7, 8, 9 }
};

// Access nested element
ToonArray row = (ToonArray)matrix[0];
int value = (int)row[1];  // Gets 2

// Or chain indexers
int value = (int)matrix[0][1];  // Gets 2
```

### Array of Objects

```csharp
var employees = new ToonArray
{
    new ToonObject
    {
        ["Name"] = "Alice",
        ["Department"] = "Engineering",
        ["Salary"] = 85000
    },
    new ToonObject
    {
        ["Name"] = "Bob",
        ["Department"] = "Marketing",
        ["Salary"] = 65000
    }
};

// Access
ToonObject firstEmployee = (ToonObject)employees[0];
string name = (string)firstEmployee["Name"];
```

### Modifying Arrays

```csharp
// Add item
arr.Add("New Item");

// Insert at position
arr.Insert(1, "Inserted Item");

// Remove by value
arr.Remove("Apple");

// Remove at index
arr.RemoveAt(0);

// Clear all items
arr.Clear();
```

## ToonDocument

Wrapper class for root-level TOON documents.

```csharp
// Parse TOON string to document
string toonString = """
Name: Alice
Age: 30
""";

ToonDocument doc = ToonDocument.Parse(toonString);

// Access root value
ToonValue root = doc.Root;

// If root is an object
if (root is ToonObject obj)
{
    string name = (string)obj["Name"];
    int age = (int)obj["Age"];
}

// Convert to string
string toonOutput = doc.ToString();
```

## Implicit Operators

`ToonValue` provides implicit conversion operators for common C# types:

```csharp
// From C# to ToonValue
ToonValue intValue = 42;
ToonValue stringValue = "Hello";
ToonValue boolValue = true;
ToonValue doubleValue = 3.14;
ToonValue decimalValue = 19.99m;

// From ToonValue to C#
int num = (int)intValue;
string text = (string)stringValue;
bool flag = (bool)boolValue;
double dbl = (double)doubleValue;
decimal dec = (decimal)decimalValue;
```

## Working with Dynamic TOON Data

### Parse Unknown Structure

```csharp
string toonInput = GetToonFromApi();

ToonDocument doc = ToonDocument.Parse(toonInput);
ToonValue root = doc.Root;

// Inspect type
switch (root.Type)
{
    case ToonValueType.Object:
        var obj = (ToonObject)root;
        ProcessObject(obj);
        break;
    
    case ToonValueType.Array:
        var arr = (ToonArray)root;
        ProcessArray(arr);
        break;
    
    case ToonValueType.String:
        var str = (ToonString)root;
        ProcessString(str.Value);
        break;
    
    // ... handle other types
}
```

### Build TOON Dynamically

```csharp
// Build a complex structure dynamically
var config = new ToonObject
{
    ["AppName"] = "MyApp",
    ["Version"] = "1.0.0",
    ["Database"] = new ToonObject
    {
        ["Host"] = "localhost",
        ["Port"] = 5432,
        ["Name"] = "mydb"
    },
    ["Features"] = new ToonArray
    {
        new ToonObject
        {
            ["Name"] = "Authentication",
            ["Enabled"] = true
        },
        new ToonObject
        {
            ["Name"] = "Caching",
            ["Enabled"] = false
        }
    }
};

// Serialize to TOON
string toon = ToonSerializer.Serialize(config);
```

### Query TOON Data

```csharp
ToonDocument doc = ToonDocument.Parse(toonInput);
ToonObject root = (ToonObject)doc.Root;

// Safe property access with null checks
if (root.TryGetValue("User", out ToonValue userValue) && 
    userValue is ToonObject user &&
    user.TryGetValue("Name", out ToonValue nameValue))
{
    string userName = (string)nameValue;
    Console.WriteLine($"User: {userName}");
}

// LINQ queries on arrays
if (root["Employees"] is ToonArray employees)
{
    var engineeringEmployees = employees
        .Cast<ToonObject>()
        .Where(emp => (string)emp["Department"] == "Engineering")
        .Select(emp => (string)emp["Name"])
        .ToList();
}
```

## Type Conversion Helpers

```csharp
// Safe conversions
public static class ToonValueExtensions
{
    public static string AsString(this ToonValue value, string defaultValue = "")
    {
        return value is ToonString str ? str.Value : defaultValue;
    }
    
    public static int AsInt(this ToonValue value, int defaultValue = 0)
    {
        return value is ToonNumber num ? num.AsInt32() : defaultValue;
    }
    
    public static bool AsBool(this ToonValue value, bool defaultValue = false)
    {
        return value is ToonBoolean b ? b.Value : defaultValue;
    }
}

// Usage
ToonObject obj = ...;
string name = obj["Name"].AsString("Unknown");
int age = obj["Age"].AsInt(0);
bool isActive = obj["IsActive"].AsBool(false);
```

## Pattern Matching

Use C# pattern matching with TOON types:

```csharp
ToonValue value = obj["Data"];

string result = value switch
{
    ToonNull => "No data",
    ToonString str => $"Text: {str.Value}",
    ToonNumber num => $"Number: {num.AsDouble()}",
    ToonBoolean b => $"Boolean: {b.Value}",
    ToonObject o => $"Object with {o.Count} properties",
    ToonArray arr => $"Array with {arr.Count} items",
    _ => "Unknown type"
};
```

## Best Practices

1. **Use strong types**: Prefer `ToonSerializer.Deserialize<T>()` over manual `ToonValue` manipulation
2. **Check types before casting**: Use `is` or `Type` property
3. **Handle nulls**: Check for `ToonNull` before accessing properties
4. **Use TryGetValue**: For safe property access on `ToonObject`
5. **Prefer implicit operators**: Cleaner syntax for conversions

```csharp
// Good: Type-safe deserialization
Person person = ToonSerializer.Deserialize<Person>(toonInput);

// OK: Manual manipulation when needed
ToonObject obj = (ToonObject)ToonDocument.Parse(toonInput).Root;
if (obj.TryGetValue("Name", out ToonValue nameValue))
{
    string name = (string)nameValue;
}
```

## See Also

- **[Serialization](serialization)**: Convert objects to TOON
- **[Deserialization](deserialization)**: Convert TOON to objects
- **[Basic Serialization](../getting-started/basic-serialization)**: Examples of different types
