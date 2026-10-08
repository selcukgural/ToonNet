# ToonNet API Guide: System.Text.Json Style

## 🎯 **Design Philosophy: Familiar by Design**

ToonNet's API follows the patterns of System.Text.Json (`Serialize`/`Deserialize` with optional options) - if you know System.Text.Json, ToonNet will feel familiar.

---

## 📊 **API Comparison**

### System.Text.Json API
```csharp
using System.Text.Json;

// Serialize
string json = JsonSerializer.Serialize(person);
string json = JsonSerializer.Serialize(person, options);

// Deserialize
Person p = JsonSerializer.Deserialize<Person>(json);
Person p = JsonSerializer.Deserialize<Person>(json, options);
```

### ToonNet API (Same Pattern)
```csharp
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;  // For JSON conversion methods

// Serialize to TOON
string toon = ToonSerializer.Serialize(person);
string toon = ToonSerializer.Serialize(person, options);

// Deserialize from TOON
Person p = ToonSerializer.Deserialize<Person>(toon);
Person p = ToonSerializer.Deserialize<Person>(toon, options);

// 🆕 JSON ↔ TOON Conversion (Extension Package!)
string toon = ToonConvert.FromJson(jsonString);  // JSON → TOON
string json = ToonConvert.ToJson(toonString);    // TOON → JSON
```

---

## ✅ **Complete API Reference**

### 1. **C# Object → TOON** (Standard Serialization)
```csharp
var person = new Person { Name = "John", Age = 30 };
string toon = ToonSerializer.Serialize(person);

// Output:
// Name: John
// Age: 30
```

### 2. **TOON → C# Object** (Standard Deserialization)
```csharp
string toon = "Name: John\nAge: 30";
var person = ToonSerializer.Deserialize<Person>(toon);

Console.WriteLine(person.Name); // "John"
Console.WriteLine(person.Age);  // 30
```

### 3. **JSON String → TOON String** (🆕 NEW!)
```csharp
string json = """{"name": "John", "age": 30}""";
string toon = ToonConvert.FromJson(json);

// Output:
// name: John
// age: 30
```

### 4. **TOON String → JSON String** (🆕 NEW!)
```csharp
string toon = "name: John\nage: 30";
string json = ToonConvert.ToJson(toon);

// Output: {"name":"John","age":30}
```

### 5. **JSON String → C# Object**
```csharp
string json = """{"Name": "John", "Age": 30}""";

// Plain System.Text.Json deserialization (optional JsonSerializerOptions)
var person = ToonConvert.DeserializeFromJson<Person>(json);

// Converts the JSON to TOON first, then deserializes with ToonSerializer (optional ToonSerializerOptions)
var person2 = ToonConvert.ParseJson<Person>(json);
```

### 6. **C# Object → JSON String**
```csharp
var person = new Person { Name = "John", Age = 30 };
string json = ToonConvert.SerializeToJson(person);   // System.Text.Json, indented by default

// Output:
// {
//   "Name": "John",
//   "Age": 30
// }
```

---

## 🔥 **Real-World Examples**

### Example 1: API Response (JSON → TOON)
```csharp
// You receive JSON from an API
var response = await httpClient.GetStringAsync("https://api.example.com/users/123");

// Convert to TOON format (more readable for logs/debugging)
string toonLog = ToonConvert.FromJson(response);

// Log it
logger.LogInformation($"User data:\n{toonLog}");

// Output in logs:
// User data:
// id: 123
// name: John Doe
// email: john@example.com
// isActive: true
```

### Example 2: Config Files (TOON ↔ JSON)
```csharp
// Load TOON config (human-readable)
string toonConfig = await File.ReadAllTextAsync("appsettings.toon");

// Convert to JSON for System.Text.Json consumers
string jsonConfig = ToonConvert.ToJson(toonConfig);

// Now you can use it with IConfiguration, etc.
var config = JsonSerializer.Deserialize<AppSettings>(jsonConfig);
```

### Example 3: Data Migration
```csharp
// You have JSON data files
var jsonFiles = Directory.GetFiles("data", "*.json");

foreach (var jsonFile in jsonFiles)
{
    // Read JSON
    string json = await File.ReadAllTextAsync(jsonFile);
    
    // Convert to TOON (smaller, especially for arrays of uniform objects)
    string toon = ToonConvert.FromJson(json);
    
    // Save as TOON
    var toonFile = Path.ChangeExtension(jsonFile, ".toon");
    await File.WriteAllTextAsync(toonFile, toon);
    
    Console.WriteLine($"Converted: {jsonFile} → {toonFile}");
}
```

### Example 4: Webhook Logging
```csharp
app.MapPost("/webhook", async (HttpRequest request) =>
{
    // Read JSON payload
    using var reader = new StreamReader(request.Body);
    string jsonPayload = await reader.ReadToEndAsync();
    
    // Convert to TOON for readable logs
    string toonPayload = ToonConvert.FromJson(jsonPayload);
    
    // Log (TOON is more readable than JSON in logs!)
    logger.LogInformation($"Webhook received:\n{toonPayload}");
    
    return Results.Ok();
});
```

---

## 📦 **Package Installation**

```bash
dotnet add package ToonNet.Core
dotnet add package ToonNet.Extensions.Json   # for ToonConvert (JSON ↔ TOON)
```

`ToonSerializer` is in `ToonNet.Core`; the JSON conversion methods (`ToonConvert`) are in `ToonNet.Extensions.Json`.

---

## 🎯 **API Design Principles**

### ✅ **DO: Like System.Text.Json**
```csharp
// ✅ Familiar, clean, simple
string toon = ToonConvert.FromJson(json);
string json = ToonConvert.ToJson(toon);
```

### ❌ **DON'T: Unfamiliar patterns**
```csharp
// ❌ AVOID: Complex, unfamiliar
var doc = ToonJsonConverter.FromJson(json);  // What is ToonDocument?
var encoder = new ToonEncoder();              // Why do I need this?
string toon = encoder.Encode(doc);           // Encode? Not Serialize?
```

---

## 💡 **Why This Matters**

**Impact:**
- ⏱️ **Short learning curve** - if you know System.Text.Json, the method names will be familiar
- 🚀 **Faster adoption** - developers feel at home immediately
- 📖 **Less documentation needed** - API is self-explanatory
- 🐛 **Fewer errors** - familiar patterns = fewer mistakes

---

## 🔄 **Complete Conversion Matrix**

| From | To | Method | Example |
|------|-----|--------|---------|
| **C# Object** | **TOON** | `Serialize()` | `ToonSerializer.Serialize(person)` |
| **TOON** | **C# Object** | `Deserialize<T>()` | `ToonSerializer.Deserialize<Person>(toon)` |
| **JSON** | **TOON** | `FromJson()` | `ToonConvert.FromJson(json)` |
| **TOON** | **JSON** | `ToJson()` | `ToonConvert.ToJson(toon)` |
| **JSON** | **C# Object** | `DeserializeFromJson<T>()` | `ToonConvert.DeserializeFromJson<Person>(json)` (System.Text.Json) |
| **JSON** | **C# Object** (via TOON) | `ParseJson<T>()` | `ToonConvert.ParseJson<Person>(json)` |
| **C# Object** | **JSON** | `SerializeToJson()` | `ToonConvert.SerializeToJson(person)` |

---

## 🔄 **Async & Streaming Serialization**

For large datasets (millions of records, database exports, ETL pipelines), ToonNet provides memory-efficient streaming serialization:

### Basic Streaming

```csharp
// Stream large dataset from database without loading all into memory
await ToonSerializer.SerializeStreamAsync(
    items: dbContext.Users.AsAsyncEnumerable(),
    filePath: "users_export.toon",
    cancellationToken: cts.Token
);

// Read back with incremental deserialization
await foreach (var user in ToonSerializer.DeserializeStreamAsync<User>("users_export.toon"))
{
    ProcessUser(user);  // Memory-efficient: only one user in memory at a time
}
```

### Advanced Configuration

```csharp
// Custom separator mode and batch size for optimal performance
await ToonSerializer.SerializeStreamAsync(
    items: GenerateLargeDatasetAsync(),
    filePath: "export.toon",
    options: null,
    writeOptions: new ToonMultiDocumentWriteOptions
    {
        Mode = ToonMultiDocumentSeparatorMode.ExplicitSeparator,  // Use "---" separator
        DocumentSeparator = "---",
        BatchSize = 100  // Buffer 100 items before writing (improves throughput)
    },
    cancellationToken: cts.Token
);
```

### Use Cases

| Use Case | Description |
|----------|-------------|
| **Database Exports** | Stream large result sets (`IAsyncEnumerable<T>`) to a file without materialising them |
| **ETL Pipelines** | Process large files one document at a time |
| **Log Processing** | Read multi-document TOON files incrementally |
| **Data Migration** | Convert large datasets with cancellable operations |

### Performance Characteristics

- **Memory:** Independent of the number of items - roughly `BatchSize × ItemSize` is buffered while writing
- **Batching:** Items are written in batches (`BatchSize`, default 50) to reduce the number of I/O calls
- **Separators:** Documents are separated by a blank line (default) or by `DocumentSeparator` (default `---`) on its own line
- **Cancellation:** Full `CancellationToken` support for long-running operations

**API Methods:**

```csharp
// File-based streaming
ValueTask SerializeStreamAsync<T>(
    IAsyncEnumerable<T> items,
    string filePath,
    ToonSerializerOptions? options = null,
    CancellationToken cancellationToken = default);

// Stream-based with custom options
ValueTask SerializeStreamAsync<T>(
    IAsyncEnumerable<T> items,
    Stream stream,
    ToonSerializerOptions? options,
    ToonMultiDocumentWriteOptions writeOptions,
    CancellationToken cancellationToken = default);

// Incremental deserialization
IAsyncEnumerable<T?> DeserializeStreamAsync<T>(
    string filePath,
    ToonSerializerOptions? options = null,
    CancellationToken cancellationToken = default);
```

---

## ✨ **Summary**

**ToonNet provides a System.Text.Json-style API:**

✅ **Familiar** - Same patterns as System.Text.Json  
✅ **Simple** - One class (`ToonSerializer`), clear methods  
✅ **Powerful** - Full C# ↔ TOON ↔ JSON support  
✅ **Developer-Friendly** - Familiar method names  

**The API you expect:**
```csharp
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;

// Just like JsonSerializer!
string toon = ToonConvert.FromJson(json);
string json = ToonConvert.ToJson(toon);
var obj = ToonSerializer.Deserialize<Person>(toon);
```

**No surprises. No confusion. Just works.** 🚀

---

## ⚠️ **IMPORTANT: Roundtrip Guarantees & Semantic Equivalence**

### Understanding Roundtrip Behavior

ToonNet provides **two types of roundtrip guarantees** depending on your use case:

#### 1️⃣ **Type-Safe Roundtrip** (Strongly-Typed) - ✅ VALUE PRESERVATION

When using **strongly-typed serialization** with C# classes, **property values are preserved**:

```csharp
// Original object
var order = new Order 
{ 
    OrderId = "ORD-123",
    Discount = 35.00m,  // decimal
    Total = 100.50m
};

// Roundtrip through TOON
string toon = ToonSerializer.Serialize(order);
var order2 = ToonSerializer.Deserialize<Order>(toon);

// ✅ Values match: the TOON text contains "Discount: 35", which reads back as 35m
Assert.Equal(35.00m, order2.Discount);  // decimal equality ignores the scale (35.00m == 35m)
```

**Guarantee**: If you serialize a C# object to TOON and deserialize back to the same type, the property values are equal to the originals. Numbers are written in canonical form, so a `decimal`'s scale (trailing zeros such as `35.00`) is not kept, and `long`/`decimal` values are read back exactly.

---

#### 2️⃣ **Format Conversion** (Loosely-Typed) - ⚠️ SEMANTIC EQUIVALENCE

When using **format conversion** between JSON/TOON strings, **semantic equivalence is guaranteed, but format details may change**:

```csharp
// Original JSON
string json = @"{ ""discount"": 35.00 }";

// Convert: JSON → TOON → JSON
string toon = ToonConvert.FromJson(json);   // discount: 35
string json2 = ToonConvert.ToJson(toon);    // {"discount":35}

// ⚠️ Format changed: 35.00 → 35
// ✅ Semantically equivalent: 35.00 == 35 (same value)
```

**What changes in format conversion:**
- ❌ Decimal trailing zeros: `35.00` → `35` (semantically equal)
- ❌ Whitespace: indentation, line breaks (cosmetic)
- ❌ Property order: may be reordered (JSON spec allows this)
- ❌ Number representation: `1e2` → `100` (semantically equal)

**What is guaranteed:**
- ✅ All property names preserved
- ✅ All values preserved (semantic equality)
- ✅ All nested structures preserved
- ✅ null/true/false preserved exactly
- ✅ String content preserved exactly

---

### Why This Matters

**How this compares with JSON DOM round-trips:**

| Library | Decimal Format | Whitespace | Property Order |
|---------|----------------|------------|----------------|
| **System.Text.Json** (`JsonElement`/`JsonNode`) | Preserved | Not preserved | Preserved |
| **Newtonsoft.Json** (`JToken`) | Preserved | Not preserved | Preserved |
| **ToonNet** (JSON ↔ TOON) | Not preserved | Not preserved | Preserved |

**Example from System.Text.Json:**
```csharp
string json1 = @"{ ""value"": 35.00 }";
var obj = JsonSerializer.Deserialize<JsonElement>(json1);
string json2 = JsonSerializer.Serialize(obj);
// Result: {"value":35.00}  ← JsonElement keeps the original number text; ToonNet normalises it
```

---

### Best Practices

#### ✅ **Use Type-Safe Serialization for Production**

```csharp
// ✅ RECOMMENDED: Exact roundtrip guaranteed
var order = ToonSerializer.Deserialize<Order>(toonString);
var modified = order with { Status = "Shipped" };
string toon = ToonSerializer.Serialize(modified);
// Values preserved (Discount reads back as 35m, equal to 35.00m)
```

#### ⚠️ **Use Format Conversion for Data Exchange**

```csharp
// ⚠️ USE CASE: Converting between formats (files, APIs)
string json = await File.ReadAllTextAsync("order.json");
string toon = ToonConvert.FromJson(json);
await File.WriteAllTextAsync("order.toon", toon);
// Data preserved, format details may change (this is OK for data exchange)
```

#### 🚫 **Don't Use String Comparison for Validation**

```csharp
// ❌ BAD: String comparison will fail due to format differences
string json1 = @"{ ""discount"": 35.00 }";
string json2 = ToonConvert.ToJson(ToonConvert.FromJson(json1));
Assert.Equal(json1, json2);  // ❌ FAILS: "35.00" vs "35"

// ✅ GOOD: Semantic comparison
var obj1 = JsonSerializer.Deserialize<JsonElement>(json1);
var obj2 = JsonSerializer.Deserialize<JsonElement>(json2);
Assert.Equal(obj1.GetProperty("discount").GetDecimal(), 
             obj2.GetProperty("discount").GetDecimal());  // ✅ PASSES
```

---

### Summary

| Scenario | Roundtrip Type | Guarantee | Use When |
|----------|---------------|-----------|----------|
| **C# → TOON → C#** | Type-Safe | Value Preservation | Production code, data storage |
| **JSON → TOON → JSON** | Format Conversion | Semantic Equivalence | File conversion, API integration |
| **YAML → TOON → YAML** | Format Conversion | Semantic Equivalence | Config file migration |

**Key Takeaway**: 
- Need **value preservation**? → Use **strongly-typed serialization** ✅
- Need **format conversion**? → Expect **semantic equivalence** (values match, format may differ) ⚠️

JSON consumers that read numbers by value treat `35.00` and `35` as the same number, so the converted data is equivalent even though the text differs.

---

## 🔧 **Manual Object Construction (Advanced)**

Just like `System.Text.Json` allows manual creation of `JsonDocument`, `JsonElement`, and `JsonObject`, ToonNet allows you to manually construct `ToonObject`, `ToonArray`, and other `ToonValue` types.

### System.Text.Json Manual Construction

```csharp
using System.Text.Json.Nodes;

// Manual JsonObject construction
var jsonObject = new JsonObject
{
    ["name"] = "John",
    ["age"] = 30,
    ["isActive"] = true
};

string json = jsonObject.ToJsonString();
// Output: {"name":"John","age":30,"isActive":true}
```

### ToonNet Manual Construction (Same Pattern)

```csharp
using ToonNet.Core.Models;
using ToonNet.Core.Encoding;

// ToonObject construction with implicit conversions
var toonObject = new ToonObject
{
    ["name"] = "John",              // string → ToonString (implicit!)
    ["age"] = 30,                   // int → ToonNumber (implicit!)
    ["isActive"] = true             // bool → ToonBoolean (implicit!)
};

var encoder = new ToonEncoder();
string toon = encoder.Encode(new ToonDocument(toonObject));

// Output:
// name: John
// age: 30
// isActive: true
```
---

### Complete Manual Construction Examples

#### 1️⃣ **Creating a Simple Object**

```csharp
// Create a user object with implicit conversions
var user = new ToonObject
{
    ["id"] = 123,                        // int → ToonNumber
    ["name"] = "Alice",                  // string → ToonString
    ["email"] = "alice@example.com",     // string → ToonString
    ["isVerified"] = true                // bool → ToonBoolean
};

// Encode to TOON string
var document = new ToonDocument(user);
var encoder = new ToonEncoder();
string toon = encoder.Encode(document);

Console.WriteLine(toon);
// Output:
// id: 123
// name: Alice
// email: alice@example.com
// isVerified: true
```

> **Note:** You can also use explicit construction if preferred: `["name"] = new ToonString("Alice")`, but implicit conversions make code cleaner.

#### 2️⃣ **Creating Nested Objects**

```csharp
// Create nested objects with implicit conversions
var address = new ToonObject
{
    ["street"] = "123 Main St",
    ["city"] = "New York",
    ["zipCode"] = "10001"
};

var user = new ToonObject
{
    ["name"] = "Bob",
    ["age"] = 35,
    ["address"] = address  // Nested object
};

var document = new ToonDocument(user);
var encoder = new ToonEncoder();
string toon = encoder.Encode(document);

Console.WriteLine(toon);
// Output:
// name: Bob
// age: 35
// address:
//   street: 123 Main St
//   city: New York
//   zipCode: "10001"   (quoted: an unquoted 10001 would read back as a number)
```

#### 3️⃣ **Creating Arrays**

```csharp
// Create an array with implicit conversions
var numbers = new ToonArray();
numbers.Add(10);       // int → ToonNumber
numbers.Add(20);       // int → ToonNumber
numbers.Add(30);       // int → ToonNumber

var document = new ToonDocument(numbers);
var encoder = new ToonEncoder();
string toon = encoder.Encode(document);

Console.WriteLine(toon);
// Output:
// [3]: 10,20,30
```

#### 4️⃣ **Creating Arrays of Objects**

```csharp
// Create an array of objects with implicit conversions
var users = new ToonArray
{
    Items =
    {
        new ToonObject
        {
            ["name"] = "Alice",
            ["age"] = 25
        },
        new ToonObject
        {
            ["name"] = "Bob",
            ["age"] = 30
        },
        new ToonObject
        {
            ["name"] = "Charlie",
            ["age"] = 35
        }
    }
};

var document = new ToonDocument(users);
var encoder = new ToonEncoder();
string toon = encoder.Encode(document);

Console.WriteLine(toon);
// Output (uniform objects with primitive values use the tabular form):
// [3]{name,age}:
//   Alice,25
//   Bob,30
//   Charlie,35
```

#### 5️⃣ **Complex Nested Structure**

```csharp
// Create a complex order object with implicit conversions
var order = new ToonObject
{
    ["orderId"] = "ORD-123",
    ["customer"] = new ToonObject
    {
        ["name"] = "John Doe",
        ["email"] = "john@example.com"
    },
    ["items"] = new ToonArray
    {
        Items =
        {
            new ToonObject
            {
                ["product"] = "Laptop",
                ["quantity"] = 1,
                ["price"] = 999.99
            },
            new ToonObject
            {
                ["product"] = "Mouse",
                ["quantity"] = 2,
                ["price"] = 25.50
            }
        }
    },
    ["total"] = 1050.99,
    ["isPaid"] = true
};

// For null values, use ToonNull.Instance explicitly
order["notes"] = ToonNull.Instance;

var document = new ToonDocument(order);
var encoder = new ToonEncoder();
string toon = encoder.Encode(document);

Console.WriteLine(toon);
// Output:
// orderId: ORD-123
// customer:
//   name: John Doe
//   email: john@example.com
// items[2]{product,quantity,price}:
//   Laptop,1,999.99
//   Mouse,2,25.5
// total: 1050.99
// isPaid: true
// notes: null
```

---

### Available ToonValue Types

| Type | Constructor | Implicit Conversion | Example |
|------|------------|---------------------|---------|
| **ToonNull** | `ToonNull.Instance` | ❌ (use explicit) | `ToonNull.Instance` |
| **ToonBoolean** | `new ToonBoolean(bool)` | ✅ `bool` | `true` → `ToonBoolean` |
| **ToonNumber** | `new ToonNumber(double)` (also `float`, `long`, `ulong`, `decimal`) | ✅ `int`, `long`, `float`, `double`, `decimal` | `42` → `ToonNumber` |
| **ToonString** | `new ToonString(string)` | ✅ `string` (non-null) | `"Hello"` → `ToonString` |
| **ToonObject** | `new ToonObject()` | ❌ (use explicit) | `new ToonObject { ["key"] = value }` |
| **ToonArray** | `new ToonArray()` | ❌ (use explicit) | `new ToonArray { Items = { value1, value2 } }` |

**Note:** Null strings (`string? value = null`) convert to `ToonNull.Instance` via `ToonValue` implicit operator.

---

### When to Use Manual Construction?

✅ **Use manual construction when:**
- Building TOON documents dynamically from non-C# sources
- Creating test data for unit tests
- Implementing custom serialization logic
- Working with APIs that return structured data
- Building configuration generators
- Creating TOON templates programmatically

✅ **Use high-level serialization when:**
- Converting C# objects to TOON (use `ToonSerializer.Serialize()`)
- Converting JSON to TOON (use `ToonConvert.FromJson()`)
- Working with strongly-typed C# models

---

### Comparison: System.Text.Json vs ToonNet

| Operation | System.Text.Json | ToonNet (Implicit) | ToonNet (Explicit) |
|-----------|------------------|--------------------|--------------------|
| **Create Object** | `new JsonObject()` | `new ToonObject()` | `new ToonObject()` |
| **Add String** | `obj["key"] = "value"` | `obj["key"] = "value"` | `obj["key"] = new ToonString("value")` |
| **Add Number** | `obj["key"] = 42` | `obj["key"] = 42` | `obj["key"] = new ToonNumber(42)` |
| **Add Boolean** | `obj["key"] = true` | `obj["key"] = true` | `obj["key"] = new ToonBoolean(true)` |
| **Add Null** | `obj["key"] = null` | `obj["key"] = ToonNull.Instance` | `obj["key"] = ToonNull.Instance` |
| **Create Array** | `new JsonArray()` | `new ToonArray()` | `new ToonArray()` |
| **Add Item** | `array.Add(42)` | `array.Add(42)` | `array.Add(new ToonNumber(42))` |
| **Encode** | `obj.ToJsonString()` | `encoder.Encode(new ToonDocument(obj))` | Same |

**Result:** ToonNet supports both implicit conversions (like System.Text.Json) and explicit construction.

---

**No surprises. No confusion. Just works.** 🚀
