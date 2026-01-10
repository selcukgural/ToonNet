# ToonNet Validation Analysis & Implementation Plan

## 🔍 Mevcut Durum Analizi

### ❌ Kritik Validation Eksiklikleri

#### 1. **ToonOptions** - Hiç validation yok!

```csharp
public sealed class ToonOptions
{
    public int IndentSize { get; set; } = 2;      // ❌ Negatif olabilir! 0 olabilir!
    public char Delimiter { get; set; } = ',';     // ❌ Whitespace olabilir, newline olabilir
    public bool StrictMode { get; set; } = true;   // ✅ Boolean, sorun yok
    public int MaxDepth { get; set; } = 64;        // ❌ Negatif olabilir! 0 veya çok büyük
}
```

**Sorunlar:**
- `IndentSize = -5` → Crash
- `IndentSize = 0` → Infinite loop potansiyeli
- `IndentSize = 1000` → Memory sorunları
- `MaxDepth = -1` → Stack overflow
- `MaxDepth = 0` → Hiçbir şey parse edilemez
- `MaxDepth = int.MaxValue` → Out of memory
- `Delimiter = '\n'` → Parse hatası
- `Delimiter = '\r'` → Parse hatası
- `Delimiter = ' '` → Ambiguity

#### 2. **ToonSerializerOptions** - Kısmi validation

```csharp
public sealed class ToonSerializerOptions
{
    public ToonOptions ToonOptions { get; set; }              // ❌ Null olabilir!
    public int MaxDepth { get; set; } = 64;                   // ❌ Aynı sorunlar
    public bool IgnoreNullValues { get; set; }                // ✅ OK
    public PropertyNamingPolicy PropertyNamingPolicy { get; set; } // ✅ Enum, OK
    public bool IncludeTypeInformation { get; set; }          // ✅ OK
    public bool PublicOnly { get; set; }                      // ✅ OK
    public bool IncludeReadOnlyProperties { get; set; }       // ✅ OK
    public List<IToonConverter> Converters { get; }           // ⚠️ List, null item olabilir
}
```

**Sorunlar:**
- `ToonOptions = null!` → NullReferenceException
- `MaxDepth` aynı sorunlar
- `Converters.Add(null!)` → Runtime crash

#### 3. **Public Constructors** - Input validation eksik

```csharp
// ToonParser
public ToonParser(ToonOptions? options = null)  // ❌ options içeriği validate edilmiyor

// ToonEncoder
public ToonEncoder(ToonOptions? options = null) // ❌ options içeriği validate edilmiyor

// ToonLexer
public ToonLexer(string input)                  // ❌ null check yok!
public ToonLexer(ReadOnlyMemory<char> input)    // ⚠️ Empty olabilir
```

#### 4. **Public Methods** - Partial validation

```csharp
// Parse methods
public ToonDocument Parse(string input)         // ❌ null check var ama empty?
public string Encode(ToonDocument document)     // ❌ null check var ama document.Root?

// Serializer
public static string Serialize<T>(T? value, ...) // ⚠️ Options validate edilmiyor
public static T? Deserialize<T>(string toonString, ...) // ❌ Empty string?
```

---

## 🎯 Validation Stratejisi

### Yaklaşım 1: Property Setter Validation (Önerilen ✅)

**Avantajlar:**
- ✅ Hemen hata yakalar (construction time)
- ✅ Immutability alternatifi
- ✅ Clear error messages
- ✅ Developer-friendly

**Dezavantajlar:**
- ⚠️ Breaking change (mevcut kod çalışmayabilir)
- ⚠️ Performance overhead (her set'te check)

```csharp
public sealed class ToonOptions
{
    private int _indentSize = 2;
    private int _maxDepth = 64;
    
    public int IndentSize
    {
        get => _indentSize;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "IndentSize must be at least 1");
            if (value > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "IndentSize cannot exceed 100");
            _indentSize = value;
        }
    }
}
```

### Yaklaşım 2: Validation Method Pattern

**Avantajlar:**
- ✅ Non-breaking
- ✅ Lazy validation
- ✅ Batch validation mümkün

**Dezavantajlar:**
- ❌ Developer unutabilir
- ❌ Runtime'da hata (daha geç)

```csharp
public sealed class ToonOptions
{
    public int IndentSize { get; set; } = 2;
    public int MaxDepth { get; set; } = 64;
    
    public void Validate()
    {
        if (IndentSize < 1 || IndentSize > 100)
            throw new ArgumentOutOfRangeException(...);
        if (MaxDepth < 1 || MaxDepth > 1000)
            throw new ArgumentOutOfRangeException(...);
    }
}
```

### Yaklaşım 3: Immutable Options with Builder (En Güvenli)

**Avantajlar:**
- ✅ Thread-safe
- ✅ Validation bir kez
- ✅ Defensive programming

**Dezavantajlar:**
- ❌ Major breaking change
- ❌ Daha verbose

```csharp
public sealed class ToonOptions
{
    public int IndentSize { get; }
    public int MaxDepth { get; }
    
    private ToonOptions(int indentSize, int maxDepth)
    {
        IndentSize = indentSize;
        MaxDepth = maxDepth;
    }
    
    public sealed class Builder
    {
        public int IndentSize { get; set; } = 2;
        public int MaxDepth { get; set; } = 64;
        
        public ToonOptions Build()
        {
            if (IndentSize < 1) throw new ArgumentOutOfRangeException(...);
            return new ToonOptions(IndentSize, MaxDepth);
        }
    }
}
```

---

## 📋 Önerilen İmplementasyon Planı

### Faz 1: Property Setter Validation (Breaking Change, Major Version)

#### 1.1 ToonOptions Validation

```csharp
public sealed class ToonOptions
{
    private int _indentSize = 2;
    private int _maxDepth = 64;
    private char _delimiter = ',';

    /// <summary>
    /// Gets or sets the number of spaces per indentation level.
    /// Valid range: 1-100. Default: 2.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when value is less than 1 or greater than 100.
    /// </exception>
    public int IndentSize
    {
        get => _indentSize;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), 
                    $"IndentSize must be at least 1, but was {value}");
            if (value > 100)
                throw new ArgumentOutOfRangeException(nameof(value), 
                    $"IndentSize cannot exceed 100, but was {value}");
            _indentSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum nesting depth allowed.
    /// Valid range: 1-1000. Default: 64.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when value is less than 1 or greater than 1000.
    /// </exception>
    public int MaxDepth
    {
        get => _maxDepth;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), 
                    $"MaxDepth must be at least 1, but was {value}");
            if (value > 1000)
                throw new ArgumentOutOfRangeException(nameof(value), 
                    $"MaxDepth cannot exceed 1000 to prevent stack overflow, but was {value}");
            _maxDepth = value;
        }
    }

    /// <summary>
    /// Gets or sets the delimiter character for array values.
    /// Cannot be whitespace or newline characters.
    /// Default: ',' (comma).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when value is a whitespace or newline character.
    /// </exception>
    public char Delimiter
    {
        get => _delimiter;
        set
        {
            if (char.IsWhiteSpace(value))
                throw new ArgumentException(
                    $"Delimiter cannot be a whitespace character (0x{(int)value:X4})", 
                    nameof(value));
            if (value == '\n' || value == '\r' || value == '\t')
                throw new ArgumentException(
                    $"Delimiter cannot be a newline or tab character (0x{(int)value:X4})", 
                    nameof(value));
            _delimiter = value;
        }
    }

    public bool StrictMode { get; set; } = true;

    public static ToonOptions Default => new();
}
```

#### 1.2 ToonSerializerOptions Validation

```csharp
public sealed class ToonSerializerOptions
{
    private ToonOptions _toonOptions = ToonOptions.Default;
    private int _maxDepth = 64;

    /// <summary>
    /// Gets or sets the TOON parsing/encoding options.
    /// Cannot be null.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when value is null.
    /// </exception>
    public ToonOptions ToonOptions
    {
        get => _toonOptions;
        set => _toonOptions = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the maximum depth for serialization.
    /// Valid range: 1-1000. Default: 64.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when value is out of valid range.
    /// </exception>
    public int MaxDepth
    {
        get => _maxDepth;
        set
        {
            if (value < 1 || value > 1000)
                throw new ArgumentOutOfRangeException(nameof(value), 
                    $"MaxDepth must be between 1 and 1000, but was {value}");
            _maxDepth = value;
        }
    }

    // ... other properties
    
    public List<IToonConverter> Converters { get; } = [];
    
    /// <summary>
    /// Adds a converter to the collection.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when converter is null.</exception>
    public void AddConverter(IToonConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        Converters.Add(converter);
    }
}
```

#### 1.3 Constructor/Method Input Validation

```csharp
// ToonLexer
public ToonLexer(string input)
{
    ArgumentNullException.ThrowIfNull(input);
    _input = input.AsMemory();
}

// ToonParser
public ToonDocument Parse(string input)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(input, nameof(input));
    // ...
}

// ToonEncoder
public string Encode(ToonDocument document)
{
    ArgumentNullException.ThrowIfNull(document);
    if (document.Root == null)
        throw new ArgumentException("Document root cannot be null", nameof(document));
    // ...
}

// ToonSerializer
public static string Serialize<T>(T? value, ToonSerializerOptions? options = null)
{
    options ??= ToonSerializerOptions.Default;
    // options now guaranteed to have valid values via property setters
    // ...
}
```

### Faz 2: Helper Extension Methods (Non-breaking)

```csharp
public static class ToonOptionsExtensions
{
    /// <summary>
    /// Validates that the options contain valid values.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when options contain invalid values.
    /// </exception>
    public static void EnsureValid(this ToonOptions options)
    {
        // Backward compatibility check
        // Throws if somehow invalid values got in
    }
}
```

### Faz 3: Unit Tests

```csharp
public class ToonOptionsValidationTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(1000)]
    public void IndentSize_InvalidValue_ThrowsArgumentOutOfRangeException(int value)
    {
        var options = new ToonOptions();
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.IndentSize = value);
        Assert.Contains("IndentSize", ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(100)]
    public void IndentSize_ValidValue_Succeeds(int value)
    {
        var options = new ToonOptions();
        options.IndentSize = value;
        Assert.Equal(value, options.IndentSize);
    }

    [Theory]
    [InlineData(' ')]
    [InlineData('\n')]
    [InlineData('\r')]
    [InlineData('\t')]
    public void Delimiter_WhitespaceCharacter_ThrowsArgumentException(char value)
    {
        var options = new ToonOptions();
        var ex = Assert.Throws<ArgumentException>(() => options.Delimiter = value);
        Assert.Contains("whitespace", ex.Message.ToLower());
    }
}
```

---

## 🎯 Karar Noktaları

### Soru 1: Breaking Change Kabul Edilebilir mi?
- **Evet ise:** Faz 1'i full implemente et (Property setter validation)
- **Hayır ise:** Faz 2'den başla (Validation method pattern)

### Soru 2: Mevcut Değer Aralıkları Uygun mu?

**Önerilen Limitler:**

| Property | Min | Max | Default | Rationale |
|----------|-----|-----|---------|-----------|
| `IndentSize` | 1 | 100 | 2 | 0 = infinite loop risk, >100 = readability issue |
| `MaxDepth` | 1 | 1000 | 64 | 0 = nothing parseable, >1000 = stack overflow risk |
| `Delimiter` | N/A | N/A | ',' | Cannot be whitespace/newline |

### Soru 3: Error Messages Türkçe mi İngilizce mi?
- **Öneri:** İngilizce (standard practice)
- **Alternatif:** Resource files ile localization

---

## 📊 Risk Analizi

### Yüksek Risk
1. **Breaking Change**: Mevcut production kod patlar
2. **Performance**: Her property set'te validation overhead

### Orta Risk
1. **Backward Compatibility**: Eski sürüm kullananlar etkilenir
2. **Test Coverage**: Tüm edge case'ler test edilmeli

### Düşük Risk
1. **Developer Experience**: Daha iyi error messages
2. **Security**: Invalid input'lardan korunma

---

## ✅ Önerilen Aksiyon Planı

### Adım 1: Karar Al
- [ ] Breaking change kabul edilebilir mi?
- [ ] Hangi yaklaşım: Setter validation mı, Immutable mi?
- [ ] Değer aralıkları onaylandı mı?

### Adım 2: Implementation
- [ ] ToonOptions validation
- [ ] ToonSerializerOptions validation
- [ ] Constructor input validation
- [ ] Method parameter validation

### Adım 3: Testing
- [ ] Unit tests (50+ test case)
- [ ] Integration tests
- [ ] Backward compatibility tests

### Adım 4: Documentation
- [ ] XML comments güncelle
- [ ] Migration guide (breaking change ise)
- [ ] CHANGELOG.md
- [ ] README.md examples

### Adım 5: Release
- [ ] Major version bump (breaking change)
- [ ] Release notes
- [ ] NuGet package update

---

## 💡 Önerim

**Property Setter Validation** yaklaşımını öneriyorum çünkü:

1. ✅ **Fail-fast**: Hatayı hemen yakalar
2. ✅ **Clear**: Developer ne yanlış yaptığını anında görür
3. ✅ **Defensive**: Runtime'da beklenmedik durumlar olmaz
4. ✅ **Standard**: .NET ekosisteminde yaygın pattern

**Trade-off:** Breaking change ama worth it for long-term stability.

---

Ne diyorsunuz? Hangi yaklaşımı seçelim ve implementation'a başlayalım mı?
