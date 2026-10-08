# Type System

Understanding the TOON value type system and how to work with `ToonValue` and its subclasses.

## Overview

ToonNet provides a strongly-typed object model for representing TOON values. The `ToonValue` class hierarchy mirrors the TOON specification types.

## .NET Type Mapping

`ToonSerializer` maps .NET values to the TOON (JSON) data model as follows. This is the host-type normalization
that TOON spec §3 requires implementations to document.

| .NET type | TOON value | Notes |
|-----------|------------|-------|
| `string`, `char` | string | |
| `bool` | `true` / `false` | |
| `byte` … `ulong`, `decimal` | number | Exact value kept |
| `float`, `double`, `Half` | number | Shortest round-trip form; NaN and ±Infinity become `null` |
| `BigInteger`, `Int128`, `UInt128` | number | Written as a quoted string when outside the `decimal` range (lossless) |
| `DateTime`, `DateTimeOffset` | string | ISO 8601 round-trip format (`O`); `DateTimeKind` is preserved |
| `DateOnly` / `TimeOnly` | string | `yyyy-MM-dd` / `HH:mm:ss[.fffffff]` |
| `TimeSpan` | string | Constant format `[-][d.]hh:mm:ss[.fffffff]` |
| `Guid`, `Uri` | string | |
| enum | string | Member name (flags: `A, B`); names are matched case-insensitively and numbers are accepted when reading |
| `null` | `null` | Omitted from objects when `IgnoreNullValues` is set; always kept in arrays |
| arrays, `IEnumerable<T>` | array | Readable into arrays, `List<T>`, `IReadOnlyList<T>`, `HashSet<T>`, `ISet<T>`, immutable collections and other collections with a parameterless constructor |
| dictionaries | object | Keys formatted with the invariant culture; readable into string, numeric, enum, `Guid` (and other parsable) keys |
| other classes, structs, records | object | Public properties in declaration order (base class first, `[ToonPropertyOrder]` first); the runtime type is used |
| `ToonValue` | as is | Useful for dynamic content |

Reading into `object` produces `Dictionary<string, object?>`, `List<object?>`, `string`, `bool`, `long` (integers),
`decimal` (other exact numbers) or `double`.

When reading:

- Integer targets reject fractions (`3.9`) and out-of-range values with a `ToonSerializationException` instead of truncating.
- Quoted numbers are accepted for numeric targets, and unquoted numbers or booleans for `string` targets.
- The constructor is chosen in this order: the one marked `[ToonConstructor]`, then a public parameterless constructor,
  then the public constructor with the most parameters (for example for positional records). Constructor parameters
  are matched to properties by name (case-insensitively), then the remaining settable properties are assigned.
- Errors include the path of the failing value, for example `$.Items[2].Price`.
- Serializing an object graph with a cycle throws a `ToonEncodingException` instead of recursing until `MaxDepth`.

## ToonValue Hierarchy

All model types live in the `ToonNet.Core.Models` namespace.

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

- **`ValueType` property**: Gets the `ToonValueType` enum value
- **Implicit operators**: Convert from `bool`, `int`, `long`, `float`, `double`, `decimal` and `string`

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
ToonValue value = ToonSerializer.Deserialize<ToonValue>(toonString)!;

if (value.ValueType == ToonValueType.Object)
{
    ToonObject obj = (ToonObject)value;
    // Work with object
}
```

## ToonNull

Represents a null value. There is a single shared instance.

```csharp
ToonNull nullValue = ToonNull.Instance;

// Check if value is null
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
bool flag = trueValue;   // implicit ToonBoolean → bool
```

## ToonNumber

Represents a number. Every number has a `double` `Value`; numbers created from integers or `decimal`, and numbers
parsed from TOON that fit in a `decimal`, also keep their exact value in `DecimalValue`.

```csharp
ToonNumber intNum = new ToonNumber(42L);                 // exact: DecimalValue = 42
ToonNumber big = new ToonNumber(9007199254740993L);      // exact, beyond double precision
ToonNumber price = new ToonNumber(19.99m);               // exact: DecimalValue = 19.99
ToonNumber ratio = new ToonNumber(3.14159);              // double only: DecimalValue = null

// Implicit conversions
ToonValue a = 42;        // int → ToonNumber (exact)
ToonValue b = 3.14;      // double → ToonNumber
ToonValue c = 19.99m;    // decimal → ToonNumber (exact)

double d = price.Value;              // 19.99
decimal? exact = price.DecimalValue; // 19.99m
string text = big.ToString();        // "9007199254740993" (canonical TOON form)
```

`ToString()` returns the canonical TOON form (spec §2): no exponent between 1e-6 and 1e21, no trailing zeros,
`-0` as `0`, and `null` for NaN and infinities.

## ToonString

Represents text values.

```csharp
// Create
ToonString str = new ToonString("Hello, ToonNet!");

// Implicit conversion
ToonValue value = "Hello";  // Creates ToonString (a null string becomes ToonNull.Instance)

// Get value
string textValue = ((ToonString)value).Value;

// Implicit ToonString → string
string text = str;
```

## ToonObject

Represents key-value pairs (like C# Dictionary or JSON object). The entries are stored in the `Properties`
dictionary (`Dictionary<string, ToonValue>`), which keeps insertion order when encoding.

### Creating ToonObject

```csharp
// Empty object
var obj = new ToonObject();

// Add properties
obj["Name"] = "Alice";
obj["Age"] = 30;
obj["Email"] = "alice@example.com";

// Initialize with values
var obj2 = new ToonObject
{
    ["Name"] = "Alice",
    ["Age"] = 30,
    ["Email"] = "alice@example.com"
};
```

### Accessing Properties

```csharp
// Indexer access: returns null (not ToonNull) when the key is missing
ToonValue? nameValue = obj["Name"];
string name = ((ToonString)nameValue!).Value;

// Check if property exists
bool hasAge = obj.Properties.ContainsKey("Age");

// Get all keys
IEnumerable<string> keys = obj.Properties.Keys;

// Get all values
IEnumerable<ToonValue> values = obj.Properties.Values;

// Iterate
foreach (var kvp in obj.Properties)
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
ToonObject address = (ToonObject)person["Address"]!;
string city = ((ToonString)address["City"]!).Value;
```

### Modifying Properties

```csharp
// Update existing property
obj["Age"] = 31;

// Remove property
obj.Properties.Remove("Email");

// Clear all properties
obj.Properties.Clear();

// Count properties
int count = obj.Properties.Count;
```

Assigning `null` through the indexer is ignored; assign `ToonNull.Instance` to store a TOON `null`.

## ToonArray

Represents ordered sequences of values (like C# List or JSON array). The items are stored in the `Items` list
(`List<ToonValue>`).

### Creating ToonArray

```csharp
// Empty array
var arr = new ToonArray();

// Add items
arr.Add("Apple");
arr.Add("Banana");
arr.Add("Cherry");

// From a list
var numbers = new ToonArray(new List<ToonValue> { 1, 2, 3, 4, 5 });
```

`ToonArray` does not implement `IEnumerable`, so collection initializers (`new ToonArray { ... }`) and LINQ work on
`Items`, not on the array itself.

### Accessing Elements

```csharp
// Indexer access
ToonValue firstItem = arr[0];
string fruit = ((ToonString)firstItem).Value;

// Count elements
int count = arr.Count;

// Iterate
foreach (ToonValue item in arr.Items)
{
    Console.WriteLine(item);
}

// LINQ
var large = numbers.Items
    .OfType<ToonNumber>()
    .Select(n => n.Value)
    .Where(n => n > 2);
```

### Nested Arrays

```csharp
var matrix = new ToonArray(new List<ToonValue>
{
    new ToonArray(new List<ToonValue> { 1, 2, 3 }),
    new ToonArray(new List<ToonValue> { 4, 5, 6 }),
    new ToonArray(new List<ToonValue> { 7, 8, 9 })
});

// Access nested element
ToonArray row = (ToonArray)matrix[0];
double value = ((ToonNumber)row[1]).Value;  // Gets 2
```

### Array of Objects

```csharp
var employees = new ToonArray(new List<ToonValue>
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
});

// Access
ToonObject firstEmployee = (ToonObject)employees[0];
string name = ((ToonString)firstEmployee["Name"]!).Value;
```

The encoder writes arrays of uniform objects like this one in tabular form
(`[2]{Name,Department,Salary}:`). Parsed tabular arrays expose their field names in `FieldNames` (`IsTabular` is
`true`).

### Modifying Arrays

```csharp
// Add item
arr.Add("New Item");

// Replace an item
arr[0] = "Replaced";

// Other list operations go through Items
arr.Items.Insert(1, "Inserted Item");
arr.Items.RemoveAt(0);
arr.Items.Clear();
```

## ToonDocument

Wrapper class for root-level TOON documents.

```csharp
// Parse TOON string to document
string toonString = """
Name: Alice
Age: 30
""";

ToonDocument doc = ToonDocument.Parse(toonString);  // optional second argument: ToonOptions

// Access root value
ToonValue root = doc.Root;

// If root is an object
if (root is ToonObject obj)
{
    string name = ((ToonString)obj["Name"]!).Value;
    double age = ((ToonNumber)obj["Age"]!).Value;
}

// Or, throwing InvalidOperationException when the root has another type
ToonObject rootObject = doc.AsObject();

// Convert back to TOON text
string toonOutput = new ToonEncoder().Encode(doc);  // ToonNet.Core.Encoding
```

`ToonDocument.Parse` throws `ToonParseException` for invalid input. `ToonDocument.ToString()` is not overridden; use
`ToonEncoder` to get the text.

## Implicit Operators

`ToonValue` provides implicit conversion operators from common C# types. Getting a C# value back requires a cast to
the concrete subclass first:

```csharp
// From C# to ToonValue
ToonValue intValue = 42;
ToonValue stringValue = "Hello";
ToonValue boolValue = true;
ToonValue doubleValue = 3.14;
ToonValue decimalValue = 19.99m;

// From ToonValue to C#
long num = (long)((ToonNumber)intValue).DecimalValue!.Value;
string text = (ToonString)stringValue;     // implicit ToonString → string
bool flag = (ToonBoolean)boolValue;        // implicit ToonBoolean → bool
double dbl = (ToonNumber)doubleValue;      // implicit ToonNumber → double
decimal dec = ((ToonNumber)decimalValue).DecimalValue!.Value;
```

To convert whole trees to and from .NET objects, use `ToonSerializer.SerializeToValue` and
`ToonSerializer.DeserializeFromValue<T>`.

## Working with Dynamic TOON Data

### Parse Unknown Structure

```csharp
string toonInput = GetToonFromApi();

ToonDocument doc = ToonDocument.Parse(toonInput);
ToonValue root = doc.Root;

// Inspect type
switch (root.ValueType)
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
    ["Features"] = new ToonArray(new List<ToonValue>
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
    })
};

// Serialize to TOON
string toon = ToonSerializer.Serialize(config);
// AppName: MyApp
// Version: 1.0.0
// Database:
//   Host: localhost
//   Port: 5432
//   Name: mydb
// Features[2]{Name,Enabled}:
//   Authentication,true
//   Caching,false
```

### Query TOON Data

```csharp
ToonDocument doc = ToonDocument.Parse(toonInput);
ToonObject root = doc.AsObject();

// Safe property access with null checks
if (root.Properties.TryGetValue("User", out ToonValue? userValue) &&
    userValue is ToonObject user &&
    user.Properties.TryGetValue("Name", out ToonValue? nameValue) &&
    nameValue is ToonString userName)
{
    Console.WriteLine($"User: {userName.Value}");
}

// LINQ queries on arrays
if (root["Employees"] is ToonArray employees)
{
    var engineeringEmployees = employees.Items
        .OfType<ToonObject>()
        .Where(emp => emp["Department"] is ToonString { Value: "Engineering" })
        .Select(emp => ((ToonString)emp["Name"]!).Value)
        .ToList();
}
```

## Type Conversion Helpers

ToonNet does not ship helpers like these; you can add your own extension methods:

```csharp
// Safe conversions
public static class ToonValueExtensions
{
    public static string AsString(this ToonValue? value, string defaultValue = "")
    {
        return value is ToonString str ? str.Value : defaultValue;
    }
    
    public static int AsInt(this ToonValue? value, int defaultValue = 0)
    {
        return value is ToonNumber num ? (int)num.Value : defaultValue;
    }
    
    public static bool AsBool(this ToonValue? value, bool defaultValue = false)
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
ToonValue? value = obj["Data"];

string result = value switch
{
    null => "Missing",
    ToonNull => "No data",
    ToonString str => $"Text: {str.Value}",
    ToonNumber num => $"Number: {num.Value}",
    ToonBoolean b => $"Boolean: {b.Value}",
    ToonObject o => $"Object with {o.Properties.Count} properties",
    ToonArray arr => $"Array with {arr.Count} items",
    _ => "Unknown type"
};
```

## Best Practices

1. **Use strong types**: Prefer `ToonSerializer.Deserialize<T>()` over manual `ToonValue` manipulation
2. **Check types before casting**: Use `is` or the `ValueType` property
3. **Handle nulls**: The `ToonObject` indexer returns `null` for missing keys; `ToonNull` is an explicit TOON `null`
4. **Use TryGetValue**: `obj.Properties.TryGetValue(...)` for safe property access on `ToonObject`
5. **Prefer implicit operators**: Cleaner syntax for building values

```csharp
// Good: Type-safe deserialization
Person person = ToonSerializer.Deserialize<Person>(toonInput)!;

// OK: Manual manipulation when needed
ToonObject obj = ToonDocument.Parse(toonInput).AsObject();
if (obj.Properties.TryGetValue("Name", out ToonValue? nameValue) && nameValue is ToonString name)
{
    Console.WriteLine(name.Value);
}
```

## See Also

- **[Serialization](serialization)**: Convert objects to TOON
- **[Deserialization](deserialization)**: Convert TOON to objects
- **[Basic Serialization](../getting-started/basic-serialization)**: Examples of different types
