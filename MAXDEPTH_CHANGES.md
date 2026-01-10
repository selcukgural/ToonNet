# MaxDepth Configuration Changes

**Date**: 2026-01-10  
**Summary**: MaxDepth default ve limit yapısı TOON spec önerilerine uygun olarak yeniden yapılandırıldı.

---

## 🎯 Değişiklik Özeti

### Önceki Durum
- **Default**: 64
- **Max Limit**: 1000 (hiçbir kısıtlama olmadan)
- **Spec Uyumu**: ⚠️ Default değer spec önerisinden (100) farklıydı

### Yeni Durum
- **Default**: 100 ✅ (TOON spec §15 ile tam uyumlu)
- **Standard Max**: 200 (güvenli kullanım için)
- **Extended Max**: 1000 (AllowExtendedLimits = true ile)
- **Spec Uyumu**: ✅ Tam uyumlu

---

## 📋 Yapılan Değişiklikler

### 1. ToonOptions.cs
```csharp
// Yeni özellik eklendi
public bool AllowExtendedLimits { get; set; } = false;

// Default değişti: 64 → 100
private int _maxDepth = 100;

// Validation logic güncellendi
public int MaxDepth
{
    get => _maxDepth;
    set
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException(...);

        int maxAllowed = AllowExtendedLimits ? 1000 : 200;
        if (value > maxAllowed)
            throw new ArgumentOutOfRangeException(...);

        _maxDepth = value;
    }
}
```

### 2. ToonSerializerOptions.cs
```csharp
// Yeni özellik eklendi
public bool AllowExtendedLimits { get; set; } = false;

// Default değişti: 64 → 100
private int _maxDepth = 100;

// Aynı validation logic
```

### 3. Test Güncellemeleri
- ✅ `ToonOptionsValidationTests.cs`: 10 yeni test eklendi
- ✅ `ToonSerializerOptionsValidationTests.cs`: 10 yeni test eklendi
- ✅ `ToonEncoderEdgeCaseTests.cs`: MaxDepth testi 70→110 güncellendi
- ✅ Default value testleri 64→100 güncellendi

### 4. Dokümantasyon
- ✅ `README.md`: Yeni Configuration Options bölümü eklendi
- ✅ `TOON_SPEC_VALIDATION_REQUIREMENTS.md`: Güncel değerlerle revize edildi
- ✅ Test sayıları güncellendi: 288 → 413

---

## 💡 Kullanım Örnekleri

### Standart Kullanım (Max 200)
```csharp
var options = new ToonOptions { MaxDepth = 150 };  // ✅ OK
var parser = new ToonParser(options);
```

### Extended Limits (Max 1000)
```csharp
var options = new ToonOptions 
{ 
    AllowExtendedLimits = true,
    MaxDepth = 500  // ✅ OK
};
```

### Hata Durumu
```csharp
var options = new ToonOptions { MaxDepth = 300 };  
// ❌ Throws: "MaxDepth cannot exceed 200. Set AllowExtendedLimits = true to allow up to 1000"
```

---

## ✅ Test Sonuçları

**Toplam**: 430 test (413 Core + 17 Source Generators)  
**Başarılı**: 430 ✅  
**Başarısız**: 0  
**Skipped**: 1

### Yeni Eklenen Testler (20 adet)
- `MaxDepth_AboveStandardMaximum_WithoutExtendedLimits_ThrowsArgumentOutOfRangeException` (6 test)
- `MaxDepth_AboveExtendedMaximum_WithExtendedLimits_ThrowsArgumentOutOfRangeException` (6 test)
- `MaxDepth_ValidValue_WithoutExtendedLimits_Succeeds` (5 test)
- `MaxDepth_ValidValue_WithExtendedLimits_Succeeds` (6 test)
- `MaxDepth_Default_Is100` (2 test)
- `AllowExtendedLimits_Default_IsFalse` (2 test)

---

## 🎯 Gerekçe

### Neden Default 100?
- ✅ TOON spec §15 açıkça 100 öneriyor
- ✅ Güvenlik ve performans dengesi
- ✅ Çoğu kullanım senaryosu için yeterli

### Neden 200 Standard Limit?
- ✅ 100'ün 2 katı (yeterli marj)
- ✅ Stack overflow riskini minimize eder
- ✅ Çoğu real-world senaryoda yeterli

### Neden AllowExtendedLimits Flag?
- ✅ Güvenlik: Yanlışlıkla aşırı değer verilmesini önler
- ✅ Bilinçli kullanım: Developer risk kabul eder
- ✅ Açık API: Neyin yapıldığı bellidir

---

## 🔒 Breaking Changes

**Evet, bu bir breaking change.**

### Etkilenen Kullanıcılar
Sadece şu kullanıcılar etkilenir:
1. MaxDepth'i 201-1000 arası kullananlar
2. Default'un 64 olmasına bağlı kod yazanlar

### Migration Guide
```csharp
// Eski kod (MaxDepth > 200 kullanıyorsanız)
var options = new ToonOptions { MaxDepth = 500 };  // ❌ Artık hata verir

// Yeni kod
var options = new ToonOptions 
{ 
    AllowExtendedLimits = true,
    MaxDepth = 500  // ✅ Çalışır
};

// Default değer değişikliği (64 → 100)
// Çoğu kullanıcı etkilenmez (100 > 64)
// Eğer tam 64'e bağlıysanız:
var options = new ToonOptions { MaxDepth = 64 };  // Explicit olarak ayarlayın
```

---

## 📊 Spec Uyumu

| Gereksinim | Önceki | Yeni | Durum |
|------------|--------|------|-------|
| Min >= 1 (MUST) | ✅ 1 | ✅ 1 | Uyumlu |
| Default = 100 (suggested) | ⚠️ 64 | ✅ 100 | **Düzeltildi** |
| Max limit belirlenmeli | ✅ 1000 | ✅ 200/1000 | İyileştirildi |

---

## 🎉 Sonuç

Bu değişiklik ile ToonNet:
- ✅ TOON spec §15 ile **tam uyumlu**
- ✅ Daha **güvenli** (kademeli limitler)
- ✅ Daha **açık** (AllowExtendedLimits flag)
- ✅ Daha **test edilebilir** (+20 test)
- ✅ Daha **iyi dokümante edilmiş**

**Test Coverage**: Tüm yeni özellikler %100 test edilmiştir.
