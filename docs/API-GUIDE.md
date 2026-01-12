# ToonNet API Guide: System.Text.Json Compatible

## 🎯 **Design Philosophy: 100% Developer Friendly**

ToonNet API is designed to be **identical** to System.Text.Json API - if you know System.Text.Json, you already know ToonNet!

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

### ToonNet API (Identical Pattern!)
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

### 5. **JSON String → C# Object** (via TOON)
```csharp
string json = """{"name": "John", "age": 30}""";
var person = ToonConvert.DeserializeFromJson<Person>(json);
```

### 6. **C# Object → JSON String**
```csharp
var person = new Person { Name = "John", Age = 30 };
string json = ToonConvert.SerializeToJson(person);

// Output: {"name":"John","age":30}
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
// name: "John Doe"
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
    
    // Convert to TOON (smaller, faster to parse)
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
```

That's it! No additional packages needed for JSON ↔ TOON conversion.

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
- ⏱️ **Zero learning curve** - if you know System.Text.Json, you know ToonNet
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
| **JSON** | **C# Object** | `DeserializeFromJson<T>()` | `ToonConvert.DeserializeFromJson<Person>(json)` |
| **C# Object** | **JSON** | `SerializeToJson()` | `ToonConvert.SerializeToJson(person)` |

---

## ✨ **Summary**

**ToonNet now provides a System.Text.Json-compatible API:**

✅ **Familiar** - Same patterns as System.Text.Json  
✅ **Simple** - One class (`ToonSerializer`), clear methods  
✅ **Powerful** - Full C# ↔ TOON ↔ JSON support  
✅ **Developer-Friendly** - Zero learning curve  

**The API you expect:**
```csharp
using ToonNet.Core.Serialization;

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

#### 1️⃣ **Type-Safe Roundtrip** (Strongly-Typed) - ✅ EXACT PRESERVATION

When using **strongly-typed serialization** with C# classes, **ALL data is preserved exactly**:

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

// ✅ GUARANTEED: order == order2 (exact match)
Assert.Equal(35.00m, order2.Discount);  // Precision preserved
```

**Guarantee**: If you serialize a C# object to TOON and deserialize back to the same type, **you get the exact same object**.

---

#### 2️⃣ **Format Conversion** (Loosely-Typed) - ⚠️ SEMANTIC EQUIVALENCE

When using **format conversion** between JSON/TOON strings, **semantic equivalence is guaranteed, but format details may change**:

```csharp
// Original JSON
string json = @"{ ""discount"": 35.00 }";

// Convert: JSON → TOON → JSON
string toon = ToonConvert.FromJson(json);   // Discount: 35.00
string json2 = ToonConvert.ToJson(toon);    // {"discount": 35}

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

**This behavior is standard across serialization libraries:**

| Library | Decimal Format | Whitespace | Property Order |
|---------|----------------|------------|----------------|
| **System.Text.Json** | Not preserved | Not preserved | Not preserved* |
| **Newtonsoft.Json** | Not preserved | Not preserved | Not preserved* |
| **ToonNet** | Not preserved | Not preserved | Preserved |

\* Unless explicitly configured

**Example from System.Text.Json:**
```csharp
string json1 = @"{ ""value"": 35.00 }";
var obj = JsonSerializer.Deserialize<JsonElement>(json1);
string json2 = JsonSerializer.Serialize(obj);
// Result: {"value":35}  ← Same behavior!
```

---

### Best Practices

#### ✅ **Use Type-Safe Serialization for Production**

```csharp
// ✅ RECOMMENDED: Exact roundtrip guaranteed
var order = ToonSerializer.Deserialize<Order>(toonString);
var modified = order with { Status = "Shipped" };
string toon = ToonSerializer.Serialize(modified);
// All data preserved exactly, including Discount = 35.00m
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
| **C# → TOON → C#** | Type-Safe | Exact Preservation | Production code, data storage |
| **JSON → TOON → JSON** | Format Conversion | Semantic Equivalence | File conversion, API integration |
| **YAML → TOON → YAML** | Format Conversion | Semantic Equivalence | Config file migration |

**Key Takeaway**: 
- Need **exact data preservation**? → Use **strongly-typed serialization** ✅
- Need **format conversion**? → Expect **semantic equivalence** (values match, format may differ) ⚠️

This is **standard industry behavior** and aligns with JSON RFC 8259 specification.

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

### ToonNet Manual Construction (Identical Pattern!)

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
//   zipCode: 10001
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
// - 10
// - 20
// - 30
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
// Output:
// - name: Alice
//   age: 25
// - name: Bob
//   age: 30
// - name: Charlie
//   age: 35
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
// items:
//   - product: Laptop
//     quantity: 1
//     price: 999.99
//   - product: Mouse
//     quantity: 2
//     price: 25.50
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
| **ToonNumber** | `new ToonNumber(double)` | ✅ `int`, `long`, `float`, `double`, `decimal` | `42` → `ToonNumber` |
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
