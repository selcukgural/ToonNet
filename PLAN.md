# ToonNet - C# Implementation Plan

## 📋 Proje Özeti

ToonNet, AI iletişimi için optimize edilmiş ToonFormat (TOON) formatını C# ile hızlı ve developer-friendly bir şekilde kullanan bir kütüphanedir. JSON'a göre %30-60 daha az token kullanımı sağlar ve strong-typed C# nesneleri ile TOON formatı arasında çift yönlü dönüşüm yapar.

## 🎯 Temel Özellikler

### 1. Çift Yönlü Dönüşüm
- **C# → TOON**: Class, Record, Struct → TOON string
- **TOON → C#**: TOON string → Strong-typed objects
- **JSON Uyumluluğu**: JSON ↔ TOON dönüşümü

### 2. Type Support
- Primitive types (int, string, bool, double, decimal, DateTime, Guid)
- Collections (List<T>, Array, IEnumerable<T>)
- Nested objects
- Records, Classes, Structs
- Nullable types
- Enums

### 3. Performance
- Source Generator kullanarak compile-time code generation
- Zero-allocation parsing where possible
- Span<char> ve Memory<char> kullanımı
- String pooling ve interning optimizasyonları

## 🏗️ Mimari

```
ToonNet/
├── src/
│   ├── ToonNet.Core/              # Core library
│   │   ├── ToonEncoder.cs         # TOON encoding engine
│   │   ├── ToonDecoder.cs         # TOON parsing engine
│   │   ├── ToonOptions.cs         # Configuration options
│   │   ├── Attributes/            # Custom attributes
│   │   │   ├── ToonPropertyAttribute.cs
│   │   │   ├── ToonIgnoreAttribute.cs
│   │   │   └── ToonArrayAttribute.cs
│   │   └── Models/
│   │       ├── ToonDocument.cs
│   │       ├── ToonValue.cs
│   │       └── ToonArray.cs
│   │
│   ├── ToonNet.SourceGenerator/   # Source generators
│   │   ├── ToonSerializerGenerator.cs
│   │   └── Templates/
│   │
│   └── ToonNet.Abstractions/      # Interfaces & base types
│       ├── IToonSerializable.cs
│       ├── IToonConverter.cs
│       └── IToonFormatter.cs
│
├── tests/
│   ├── ToonNet.Tests/             # Unit tests
│   ├── ToonNet.Benchmarks/        # Performance benchmarks
│   └── ToonNet.IntegrationTests/  # Integration tests
│
└── samples/
    ├── BasicUsage/
    ├── LlmIntegration/
    └── PerformanceComparison/
```

## 📝 Implementation Phases

### Phase 1: Core Parser & Encoder (Week 1-2)
**Hedef**: Temel TOON formatını parse edip encode edebilme

#### 1.1 Lexer/Tokenizer
```csharp
// TOON string'i token'lara ayırma
public class ToonLexer
{
    public IEnumerable<ToonToken> Tokenize(ReadOnlySpan<char> input);
}
```

#### 1.2 Parser
```csharp
// Token'ları ToonDocument'e dönüştürme
public class ToonParser
{
    public ToonDocument Parse(IEnumerable<ToonToken> tokens);
    public ToonDocument Parse(string toonString);
}
```

#### 1.3 Encoder
```csharp
// ToonDocument'ten TOON string üretme
public class ToonEncoder
{
    public string Encode(ToonDocument document, ToonOptions? options = null);
}
```

**Test Kapsamı**:
- Simple objects (key-value pairs)
- Nested objects
- Primitive arrays
- Tabular arrays
- Quoting rules

### Phase 2: Serialization System (Week 3-4)
**Hedef**: Strong-typed C# nesneleri ile TOON arasında dönüşüm

#### 2.1 Serializer
```csharp
public static class ToonSerializer
{
    public static string Serialize<T>(T value, ToonOptions? options = null);
    public static T? Deserialize<T>(string toonString, ToonOptions? options = null);
    public static object? Deserialize(string toonString, Type type, ToonOptions? options = null);
}
```

#### 2.2 Type Converters
```csharp
public interface IToonConverter<T>
{
    void Write(ToonWriter writer, T value);
    T Read(ref ToonReader reader);
}

// Built-in converters
- PrimitiveConverter
- StringConverter
- ArrayConverter<T>
- ListConverter<T>
- DictionaryConverter<TKey, TValue>
- ObjectConverter
- NullableConverter<T>
- EnumConverter<T>
```

#### 2.3 Reflection-based Serialization
```csharp
// Runtime reflection ile serialization
public class ReflectionToonConverter<T> : IToonConverter<T>
{
    // Property'leri dinamik olarak okuma/yazma
}
```

**Test Kapsamı**:
- POCO serialization
- Record serialization
- Struct serialization
- Collection serialization
- Nested object serialization
- Null handling

### Phase 3: Source Generator (Week 5-6)
**Hedef**: Compile-time code generation ile performans optimizasyonu

#### 3.1 Generator
```csharp
[Generator]
public class ToonSerializerGenerator : IIncrementalGenerator
{
    // [ToonSerializable] attribute'u olan tipleri tespit et
    // Her tip için özel serializer kodu üret
}
```

#### 3.2 Generated Code Pattern
```csharp
// Input:
[ToonSerializable]
public record User(int Id, string Name, string Role);

// Generated:
partial record User
{
    public static string ToToon(User value) { /* optimized code */ }
    public static User FromToon(string toon) { /* optimized code */ }
}
```

**Avantajlar**:
- Zero reflection overhead
- Compile-time error checking
- IDE IntelliSense support
- AOT-compatible

### Phase 4: Advanced Features (Week 7-8)
**Hedef**: Production-ready özellikler

#### 4.1 Configuration Options
```csharp
public class ToonOptions
{
    public int IndentSize { get; set; } = 2;
    public char Delimiter { get; set; } = ',';
    public bool StrictMode { get; set; } = true;
    public NamingPolicy PropertyNamingPolicy { get; set; } = NamingPolicy.CamelCase;
    public bool IgnoreNullValues { get; set; } = false;
    public int MaxDepth { get; set; } = 64;
}
```

#### 4.2 Attributes
```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ToonSerializableAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public class ToonPropertyAttribute : Attribute
{
    public string? Name { get; set; }
    public int Order { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class ToonIgnoreAttribute : Attribute { }
```

#### 4.3 Streaming Support
```csharp
public class ToonStreamWriter : IDisposable
{
    public void WriteArray<T>(IEnumerable<T> items);
    public void WriteObject(object value);
}

public class ToonStreamReader : IDisposable
{
    public IAsyncEnumerable<T> ReadArrayAsync<T>();
}
```

#### 4.4 JSON Interoperability
```csharp
public static class ToonJsonConverter
{
    public static string JsonToToon(string json, ToonOptions? options = null);
    public static string ToonToJson(string toon, JsonSerializerOptions? options = null);
}
```

### Phase 5: Performance & Polish (Week 9-10)
**Hedef**: Production-ready quality

#### 5.1 Performance Optimization
- Benchmarking (vs System.Text.Json, Newtonsoft.Json)
- Memory profiling
- Allocation reduction
- StringBuilder pooling
- ArrayPool usage

#### 5.2 Error Handling
```csharp
public class ToonException : Exception { }
public class ToonParseException : ToonException { }
public class ToonSerializationException : ToonException { }
```

#### 5.3 Documentation
- XML documentation comments
- README with examples
- API reference
- Migration guide from JSON
- Best practices guide

## 🚀 API Design Examples

### Basic Usage
```csharp
// Serialization
var user = new User(1, "Alice", "admin");
string toon = ToonSerializer.Serialize(user);
// Output: id: 1\nname: Alice\nrole: admin

// Deserialization
var parsed = ToonSerializer.Deserialize<User>(toon);

// Array serialization
var users = new[] {
    new User(1, "Alice", "admin"),
    new User(2, "Bob", "user")
};
string toon = ToonSerializer.Serialize(users);
// Output: users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user
```

### With Options
```csharp
var options = new ToonOptions
{
    PropertyNamingPolicy = NamingPolicy.SnakeCase,
    IgnoreNullValues = true,
    IndentSize = 4
};

string toon = ToonSerializer.Serialize(data, options);
```

### Source Generator Usage
```csharp
[ToonSerializable]
public partial record User
{
    [ToonProperty(Name = "user_id")]
    public int Id { get; init; }
    
    public string Name { get; init; }
    
    [ToonIgnore]
    public string InternalData { get; init; }
}

// Generated methods available
string toon = User.ToToon(user);
User parsed = User.FromToon(toon);
```

### JSON Conversion
```csharp
string json = """{"id": 1, "name": "Alice"}""";
string toon = ToonJsonConverter.JsonToToon(json);
// Output: id: 1\nname: Alice

string jsonBack = ToonJsonConverter.ToonToJson(toon);
```

## 🎯 Success Criteria

1. **Correctness**: Tüm TOON spec testlerini geçmeli
2. **Performance**: JSON serialization'dan en az %30 daha az token
3. **Developer Experience**: 
   - Fluent API
   - Clear error messages
   - IntelliSense support
4. **Compatibility**: .NET 6+, .NET Standard 2.1
5. **Documentation**: %100 XML documentation coverage

## 📊 Performance Targets

| Metric | Target |
|--------|--------|
| Token reduction vs JSON | 30-60% |
| Serialization speed | ~80% of System.Text.Json |
| Memory allocation | < 2x input size |
| Source Generator compile time | < 500ms incremental |

## 🔧 Tech Stack

- **Target Framework**: .NET 8.0, .NET 6.0, .NET Standard 2.1
- **Source Generator**: Roslyn Incremental Source Generator
- **Testing**: xUnit, FluentAssertions
- **Benchmarking**: BenchmarkDotNet
- **CI/CD**: GitHub Actions
- **Package**: NuGet

## 📦 NuGet Packages

1. **ToonNet** - Core library (all-in-one)
2. **ToonNet.Core** - Core encoding/decoding
3. **ToonNet.SourceGenerator** - Source generator
4. **ToonNet.Abstractions** - Interfaces only

## 🔄 Next Steps

1. ✅ Create solution structure
2. ⬜ Implement lexer/tokenizer
3. ⬜ Implement parser
4. ⬜ Implement encoder
5. ⬜ Add core tests
6. ⬜ Implement serializer
7. ⬜ Add type converters
8. ⬜ Create source generator
9. ⬜ Add benchmarks
10. ⬜ Write documentation

## 📚 References

- [TOON Specification](https://toonformat.dev/reference/spec.html)
- [TOON Format Overview](https://toonformat.dev/guide/format-overview.html)
- [TypeScript Implementation](https://github.com/toon-format/toon)

---

**Estimated Timeline**: 10 weeks
**Team Size**: 1-2 developers
**Priority**: High performance, developer experience, correctness
