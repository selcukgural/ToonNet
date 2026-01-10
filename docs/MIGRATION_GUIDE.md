# Migration Guide: Phase 1-2 to Phase 3 Source Generator

**Version:** 1.0  
**Updated:** 2026-01-10  
**Audience:** Developers using ToonNet

---

## Overview

Phase 3 introduces optional compile-time code generation via the `[ToonSerializable]` attribute. This guide helps you understand when and how to migrate from Phase 1-2 reflection-based serialization to Phase 3 generated code.

**Key Point:** Phase 3 is **100% backward compatible**. Existing code continues to work without modification. The `[ToonSerializable]` attribute is optional and only required if you want the performance benefits of generated code.

---

## Should You Migrate?

### Migrate to Phase 3 if You...

✅ **Need Performance:** If serialization is in hot paths or called frequently  
✅ **Want Compile-time Errors:** If catching type errors at build-time is important  
✅ **Target AOT:** If you're using Native AOT or similar ahead-of-time compilation  
✅ **Care About Memory:** If reducing allocations is a priority  
✅ **Debug Serialization:** If you want to inspect generated serialization code  

### Stay with Phase 1-2 Reflection if You...

❌ **Have Dynamic Types:** If your types change at runtime  
❌ **Use Complex Hierarchies:** If you have deeply nested inheritance structures  
❌ **Prefer Convention Over Annotation:** If you want automatic serialization without attributes  
❌ **Have Performance Headroom:** If reflection performance is acceptable  

---

## Migration Path

### Option 1: Gradual Migration (Recommended)

Migrate classes one at a time. Reflection and generated code coexist.

```csharp
// Phase 1-2: Reflection-based
public class Config
{
    public string DatabaseUrl { get; set; }
}

// Migrate to Phase 3: Add attribute and make partial
[ToonSerializable]
public partial class Config
{
    public string DatabaseUrl { get; set; }
}
```

### Option 2: Big Bang Migration

Convert all classes at once. Requires updating all serialization call sites.

**Before:**
```csharp
var config = new Config { DatabaseUrl = "..." };
var json = ToonSerializer.Serialize(config);
var config2 = ToonSerializer.Deserialize<Config>(json);
```

**After:**
```csharp
var config = new Config { DatabaseUrl = "..." };
var doc = Config.Serialize(config);
var config2 = Config.Deserialize(doc);
```

### Option 3: Hybrid Approach

Use generated code where it matters (hot paths), keep reflection elsewhere.

```csharp
[ToonSerializable]
public partial class HighTrafficDTO
{
    // Use generated code for performance-critical operations
}

public class ColdPathConfig
{
    // Keep using ToonSerializer.Serialize/Deserialize for less critical code
}
```

---

## Step-by-Step Migration

### Step 1: Identify Classes for Migration

List all classes you want to migrate:
- Performance-sensitive types
- Types with frequent serialization
- Types in high-throughput code paths

### Step 2: Add Required Namespaces

```csharp
using ToonNet.Serialization.Attributes;  // ← Add this
```

### Step 3: Add Attribute and Make Partial

```csharp
// Before
public class User
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// After
[ToonSerializable]
public partial class User
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

### Step 4: Update Serialization Calls

**Before (Phase 1-2):**
```csharp
var user = new User { Name = "Alice", Age = 30 };
var json = ToonSerializer.Serialize(user);
var user2 = ToonSerializer.Deserialize<User>(json);
```

**After (Phase 3):**
```csharp
var user = new User { Name = "Alice", Age = 30 };
var doc = User.Serialize(user);  // Returns ToonDocument, not string
var user2 = User.Deserialize(doc);  // Takes ToonDocument, not string
```

### Step 5: Handle Type Changes

Note: Generated code returns/accepts `ToonDocument` instead of strings.

```csharp
// If you need a string:
var doc = User.Serialize(user);
var json = ToonEncoder.Encode(doc);  // ← Convert to string

// If you have a string:
var doc = ToonParser.Parse(json);  // ← Parse to ToonDocument
var user = User.Deserialize(doc);
```

### Step 6: Test Round-Trips

Verify that deserialization recovers the original data:

```csharp
var original = new User { Name = "Alice", Age = 30 };
var doc = User.Serialize(original);
var restored = User.Deserialize(doc);

Assert.Equal(original.Name, restored.Name);
Assert.Equal(original.Age, restored.Age);
```

### Step 7: Run Tests

Rebuild and run all tests to ensure no breakage:

```bash
dotnet build
dotnet test
```

---

## API Changes

### ToonDocument vs String

| Aspect | Phase 1-2 | Phase 3 |
|--------|----------|--------|
| **Serialize returns** | `string` | `ToonDocument` |
| **Deserialize accepts** | `string` | `ToonDocument` |
| **Conversion** | N/A | Use `ToonEncoder`/`ToonParser` |

### Property Type Support

Phase 3 generated code supports scalar types directly:

```csharp
// ✅ Generated directly
public string Name { get; set; }
public int Age { get; set; }
public decimal Price { get; set; }

// ⚠️ Reflection fallback
public List<string> Tags { get; set; }
public Dictionary<string, int> Metrics { get; set; }
public CustomObject Nested { get; set; }
```

---

## Performance Migration

### Before Migration

```csharp
public class User
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// Performance: ~5-7µs per operation
var json = ToonSerializer.Serialize(user);
var user2 = ToonSerializer.Deserialize<User>(json);
```

### After Migration

```csharp
[ToonSerializable]
public partial class User
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// Performance: ~1.5µs per operation (3-5x faster)
var doc = User.Serialize(user);
var user2 = User.Deserialize(doc);
```

### Measurement

Use the included benchmarks to measure impact:

```bash
cd src/ToonNet.Benchmarks
dotnet run -c Release
```

---

## Troubleshooting Migration

### Problem: "Class must be partial"

**Error:** Generated code won't compile

**Solution:**
```csharp
// ❌ Wrong
[ToonSerializable]
public class User { }

// ✅ Correct
[ToonSerializable]
public partial class User { }
```

### Problem: "ToonDocument does not have .Root"

**Error:** Accessing wrong property

**Solution:**
```csharp
// ❌ Wrong
var doc = User.Serialize(user);
var root = doc.AsObject();  // AsObject() doesn't exist on ToonDocument

// ✅ Correct
var doc = User.Serialize(user);
var obj = (ToonObject)doc.Root;
```

### Problem: "No static Serialize method"

**Error:** Generated code not appearing

**Solution:**
1. Rebuild solution (generated code updates at compile time)
2. Check class has `[ToonSerializable]` attribute
3. Check class is `partial`
4. Check there are no compilation errors

### Problem: Different serialization output

**Error:** Generated code produces different TOON than reflection

**Solution:** 
Generated code applies property naming policies at compile time. Ensure options match:

```csharp
// Make sure to pass the same options to both methods
var options = new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase };
var doc = User.Serialize(user, options);
```

---

## Real-World Examples

### Example 1: Web API Response DTO

```csharp
// Before (Phase 1-2)
public class UserResponseDTO
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}

[HttpGet("{id}")]
public IActionResult GetUser(int id)
{
    var user = _userService.GetUser(id);
    var dto = new UserResponseDTO { /* ... */ };
    var json = ToonSerializer.Serialize(dto);
    return Ok(json);
}

// After (Phase 3)
[ToonSerializable]
public partial class UserResponseDTO
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}

[HttpGet("{id}")]
public IActionResult GetUser(int id)
{
    var user = _userService.GetUser(id);
    var dto = new UserResponseDTO { /* ... */ };
    var doc = UserResponseDTO.Serialize(dto);
    var json = ToonEncoder.Encode(doc);
    return Ok(json);
}
```

### Example 2: Configuration Loading

```csharp
// Before (Phase 1-2)
public class AppConfig
{
    public string ConnectionString { get; set; }
    public int MaxRetries { get; set; }
}

public void LoadConfig(string filePath)
{
    var json = File.ReadAllText(filePath);
    var config = ToonSerializer.Deserialize<AppConfig>(json);
    return config;
}

// After (Phase 3)
[ToonSerializable]
public partial class AppConfig
{
    public string ConnectionString { get; set; }
    public int MaxRetries { get; set; }
}

public AppConfig LoadConfig(string filePath)
{
    var json = File.ReadAllText(filePath);
    var doc = ToonParser.Parse(json);
    var config = AppConfig.Deserialize(doc);
    return config;
}
```

### Example 3: Cached Data Structure

```csharp
// Before (Phase 1-2)
public class CacheEntry
{
    public string Key { get; set; }
    public string Value { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public string SerializeForCache(CacheEntry entry)
{
    return ToonSerializer.Serialize(entry);
}

// After (Phase 3) - Performance-critical path
[ToonSerializable]
public partial class CacheEntry
{
    public string Key { get; set; }
    public string Value { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public string SerializeForCache(CacheEntry entry)
{
    var doc = CacheEntry.Serialize(entry);
    return ToonEncoder.Encode(doc);
}
```

---

## Rollback Plan

If you need to revert to Phase 1-2 reflection:

1. **Remove attribute:** Delete `[ToonSerializable]` 
2. **Remove partial:** Change `partial class` to `class`
3. **Revert calls:** Change `Type.Serialize(x)` back to `ToonSerializer.Serialize(x)`
4. **Handle ToonDocument:** Parse `ToonDocument` back to string if needed

```csharp
// Reverting from Phase 3
var json = File.ReadAllText("data.toon");
var doc = ToonParser.Parse(json);
var user = User.Deserialize(doc);

// Back to Phase 1-2
var user = ToonSerializer.Deserialize<User>(json);
```

---

## Frequently Asked Questions

**Q: Do I have to migrate everything at once?**  
A: No. Phase 3 is fully backward compatible. Migrate classes at your own pace.

**Q: Will generated code work with inheritance?**  
A: Partial support. Base class properties are included in generated code.

**Q: Can I mix generated and reflection code?**  
A: Yes. You can use `[ToonSerializable]` on some classes and `ToonSerializer` on others.

**Q: What happens to properties that aren't serializable types?**  
A: Complex/collection types fall back to `ToonSerializer.Deserialize<T>()` which uses reflection.

**Q: Will my benchmarks improve if I don't have many serialization operations?**  
A: Only if serialization is a bottleneck. Profile first, optimize second.

**Q: Can I see the generated code?**  
A: Yes! It's in `obj/Release/net{version}/ToonNet.SourceGenerators/{Type}.g.cs`

---

## Next Steps

1. **Identify** 2-3 classes for pilot migration
2. **Test** the migration process with benchmarks
3. **Roll out** gradually across your codebase
4. **Monitor** performance improvements
5. **Document** any migration patterns specific to your project

---

## Support

For migration issues:
1. Check the troubleshooting section above
2. Review the Source Generator Guide (`docs/PHASE_3_SOURCE_GENERATOR_GUIDE.md`)
3. Examine generated code in `obj/` directory
4. Open an issue on GitHub with minimal reproduction

---

**End of Migration Guide**
