# Error Handling Guide

## 🎯 Developer-Friendly Error Messages

ToonNet provides detailed, actionable error messages to help you quickly identify and fix issues.

### Exception Types

#### 1. **ToonParseException**
Thrown when parsing TOON format fails.

**Properties:**
- `Line` - Line number where error occurred
- `Column` - Column number where error occurred
- `ActualToken` - What was found
- `ExpectedToken` - What was expected
- `Suggestion` - How to fix it
- `CodeSnippet` - Example of correct usage

**Example:**
```csharp
try 
{
    var doc = parser.Parse(toonString);
}
catch (ToonParseException ex)
{
    Console.WriteLine($"Parse error at {ex.Line}:{ex.Column}");
    Console.WriteLine(ex.Suggestion); // Shows how to fix
}
```

**Sample Error Message:**
```
Missing colon after key 'name'
  📍 Position: Line 5, Column 10

💡 Suggestion: Add a colon after the key. Example: name: value

📝 Code:
  name: value
```

#### 2. **ToonEncodingException**
Thrown when encoding to TOON format fails.

**Properties:**
- `PropertyPath` - Path to problematic property
- `ProblematicValue` - The value that caused the issue
- `Suggestion` - How to fix it

**Example:**
```csharp
try 
{
    var toon = encoder.Encode(document);
}
catch (ToonEncodingException ex)
{
    Console.WriteLine($"Cannot encode: {ex.PropertyPath}");
    Console.WriteLine(ex.Suggestion);
}
```

**Sample Error Message:**
```
Maximum depth of 64 exceeded during encoding
  📍 Property: User.Profile.Settings.Advanced.Custom
  
💡 Suggestion: Your object has too many nested levels. Consider flattening your structure or increasing MaxDepth option.
```

#### 3. **ToonSerializationException**
Thrown during C# ↔ TOON conversion.

**Properties:**
- `TargetType` - C# type being serialized/deserialized
- `PropertyName` - Property that failed
- `Value` - The problematic value
- `Suggestion` - How to fix it

**Example:**
```csharp
try 
{
    var person = ToonSerializer.Deserialize<Person>(toonString);
}
catch (ToonSerializationException ex)
{
    Console.WriteLine($"Cannot convert to {ex.TargetType.Name}");
    Console.WriteLine($"Property: {ex.PropertyName}");
    Console.WriteLine($"Value: {ex.Value}");
    Console.WriteLine(ex.Suggestion);
}
```

**Sample Error Message:**
```
Cannot deserialize value to target type
  🎯 Target Type: Int32
  📍 Property: Age
  ⚠️  Value: not-a-number (String)
  
💡 Suggestion: Ensure the TOON value matches the expected type. Expected a number but got a string.
```

### Common Error Scenarios

#### Missing Colon
```toon
name Alice  ❌
```
**Error:** Missing colon after key 'name'  
**Fix:**
```toon
name: Alice  ✅
```

#### Unterminated String
```toon
name: "Alice  ❌
```
**Error:** Missing closing quote for string  
**Fix:**
```toon
name: "Alice"  ✅
```

#### Invalid Indentation
```toon
user:
      name: Alice  ❌ (too many spaces)
```
**Error:** Unexpected indentation - too many spaces  
**Fix:**
```toon
user:
  name: Alice  ✅ (2 spaces)
```

#### Array Length Mismatch
```toon
items[5]: 1,2,3  ❌
```
**Error:** Array length mismatch: expected 5, got 3  
**Fix:**
```toon
items[3]: 1,2,3  ✅
```

### Best Practices

1. **Always catch specific exceptions:**
```csharp
try 
{
    var result = ToonSerializer.Deserialize<MyClass>(input);
}
catch (ToonParseException ex)
{
    // Handle parsing errors
    logger.Error($"Parse error at {ex.Line}:{ex.Column}", ex);
}
catch (ToonSerializationException ex)
{
    // Handle type conversion errors
    logger.Error($"Cannot convert {ex.PropertyName}", ex);
}
```

2. **Use exception properties for debugging:**
```csharp
catch (ToonParseException ex)
{
    // Log detailed context
    var context = new {
        ex.Line,
        ex.Column,
        ex.ActualToken,
        ex.ExpectedToken,
        ex.Suggestion
    };
    logger.Debug("Parse context", context);
}
```

3. **Show suggestions to end users:**
```csharp
catch (ToonException ex)
{
    if (!string.IsNullOrEmpty(ex.Suggestion))
    {
        Console.WriteLine("💡 " + ex.Suggestion);
    }
}
```

### Configuration Options

Adjust error handling behavior:

```csharp
var options = new ToonOptions 
{
    StrictMode = true,  // Throw on warnings
    MaxDepth = 64       // Prevent stack overflow
};

var serializerOptions = new ToonSerializerOptions
{
    MaxDepth = 32,      // Limit nesting depth
    IgnoreNullValues = true  // Skip problematic nulls
};
```

### Debugging Tips

1. **Enable detailed error messages** (already enabled by default)
2. **Check Line/Column numbers** - Points to exact location
3. **Read the Suggestion** - Tells you exactly how to fix
4. **Look at CodeSnippet** - Shows correct format
5. **Use try-catch during development** - Catch issues early

### Error Message Symbols

- 📍 **Position** - Where the error occurred
- ⚠️  **Value** - The problematic value
- 🎯 **Target** - Expected type/format
- 💡 **Suggestion** - How to fix
- 📝 **Code** - Example of correct usage
