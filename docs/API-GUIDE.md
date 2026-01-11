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

// Serialize to TOON
string toon = ToonSerializer.Serialize(person);
string toon = ToonSerializer.Serialize(person, options);

// Deserialize from TOON
Person p = ToonSerializer.Deserialize<Person>(toon);
Person p = ToonSerializer.Deserialize<Person>(toon, options);

// 🆕 JSON ↔ TOON Conversion (NEW!)
string toon = ToonSerializer.FromJson(jsonString);  // JSON → TOON
string json = ToonSerializer.ToJson(toonString);    // TOON → JSON
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
string toon = ToonSerializer.FromJson(json);

// Output:
// name: John
// age: 30
```

### 4. **TOON String → JSON String** (🆕 NEW!)
```csharp
string toon = "name: John\nage: 30";
string json = ToonSerializer.ToJson(toon);

// Output: {"name":"John","age":30}
```

### 5. **JSON String → C# Object** (via TOON)
```csharp
string json = """{"name": "John", "age": 30}""";
var person = ToonSerializer.DeserializeFromJson<Person>(json);
```

### 6. **C# Object → JSON String**
```csharp
var person = new Person { Name = "John", Age = 30 };
string json = ToonSerializer.SerializeToJson(person);

// Output: {"name":"John","age":30}
```

---

## 🔥 **Real-World Examples**

### Example 1: API Response (JSON → TOON)
```csharp
// You receive JSON from an API
var response = await httpClient.GetStringAsync("https://api.example.com/users/123");

// Convert to TOON format (more readable for logs/debugging)
string toonLog = ToonSerializer.FromJson(response);

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
string jsonConfig = ToonSerializer.ToJson(toonConfig);

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
    string toon = ToonSerializer.FromJson(json);
    
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
    string toonPayload = ToonSerializer.FromJson(jsonPayload);
    
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
string toon = ToonSerializer.FromJson(json);
string json = ToonSerializer.ToJson(toon);
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

**Before (Complex):**
```csharp
// Developer needs to learn ToonDocument, ToonEncoder, etc.
var toonDoc = ToonJsonConverter.FromJson(json);
var encoder = new ToonEncoder();
string toon = encoder.Encode(toonDoc);
```

**After (Simple):**
```csharp
// Developer already knows this pattern!
string toon = ToonSerializer.FromJson(json);
```

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
| **JSON** | **TOON** | `FromJson()` | `ToonSerializer.FromJson(json)` |
| **TOON** | **JSON** | `ToJson()` | `ToonSerializer.ToJson(toon)` |
| **JSON** | **C# Object** | `DeserializeFromJson<T>()` | `ToonSerializer.DeserializeFromJson<Person>(json)` |
| **C# Object** | **JSON** | `SerializeToJson()` | `ToonSerializer.SerializeToJson(person)` |

---

## 🎓 **Migration Guide: Old API → New API**

### Old Way (Before)
```csharp
// ❌ Complex
using ToonNet.Extensions.Json;
using ToonNet.Core.Encoding;

var toonDoc = ToonJsonConverter.FromJson(json);
var encoder = new ToonEncoder();
string toon = encoder.Encode(toonDoc);
```

### New Way (Now)
```csharp
// ✅ Simple!
using ToonNet.Core.Serialization;

string toon = ToonSerializer.FromJson(json);
```

**Migration Steps:**
1. Remove `using ToonNet.Extensions.Json;` (if only used for JSON conversion)
2. Remove `using ToonNet.Core.Encoding;` (if only used for encoding)
3. Replace `ToonJsonConverter.FromJson()` + `ToonEncoder` → `ToonSerializer.FromJson()`
4. That's it! ✅

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
string toon = ToonSerializer.FromJson(json);
string json = ToonSerializer.ToJson(toon);
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
string toon = ToonSerializer.FromJson(json);   // Discount: 35.00
string json2 = ToonSerializer.ToJson(toon);    // {"discount": 35}

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
string toon = ToonSerializer.FromJson(json);
await File.WriteAllTextAsync("order.toon", toon);
// Data preserved, format details may change (this is OK for data exchange)
```

#### 🚫 **Don't Use String Comparison for Validation**

```csharp
// ❌ BAD: String comparison will fail due to format differences
string json1 = @"{ ""discount"": 35.00 }";
string json2 = ToonSerializer.ToJson(ToonSerializer.FromJson(json1));
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

**No surprises. No confusion. Just works.** 🚀
