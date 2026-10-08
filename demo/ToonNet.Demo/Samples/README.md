# ToonNet Comprehensive Sample Data

Bu klasör, **TOON specification'ın desteklediği TÜM veri tiplerini** içeren **gerçek dünya örnekleri** içerir.

## 📁 Samples İçeriği

### 1. E-Commerce Order (Sipariş Sistemi)
**Dosyalar:**
- `ecommerce-order.toon` - TOON format
- `ecommerce-order.json` - JSON format  
- `ecommerce-order.yaml` - YAML format
- `ECommerceModels.cs` - C# models

**İçerik:**
- ✅ Nested objects (Customer, Address, Payment, Shipping)
- ✅ Collections (Items[], Reviews[], CouponCodes[])
- ✅ Primitives (string, int, decimal, double, bool, DateTime)
- ✅ Nullable fields (GiftMessage, ReferralCode)
- ✅ Dictionaries (Product Attributes)
- ✅ Arrays of objects
- ✅ Complex nested structures

**Kullanım Senaryosu:** E-ticaret siparişi - gerçek dünyada kullanılan kompleks veri

---

### 2. Healthcare Patient Record (Hasta Kaydı)
**Dosyalar:**
- `healthcare-patient.toon` - TOON format
- `healthcare-patient.json` - JSON format  
- `healthcare-patient.yaml` - YAML format
- `HealthcareModels.cs` - C# models

**İçerik:**
- ✅ Medical vital signs (blood pressure, heart rate, oxygen saturation)
- ✅ Multiple diagnoses with ICD-10 codes
- ✅ Medications with dosage schedules
- ✅ Lab results with units (CBC, X-Ray, HbA1c)
- ✅ Time-series data (VitalSigns[], LabResults[])
- ✅ Null values (DischargeDate, EndDate)
- ✅ Mixed types (string, int, double, DateTime, bool, null)
- ✅ Complex nested structures (EmergencyContact, Insurance, BloodPressure)
- ✅ Dictionary types (Lab Results, Units)
- ✅ Empty collections (SideEffects[])

**Kullanım Senaryosu:** Electronic medical record (EMR) system - sensitive and complex healthcare data

---

## 🎯 Demo Özellikleri

### ✅ Her format için dönüşüm desteği:

```
TOON ←→ JSON ←→ YAML
  ↓       ↓       ↓
C# Object (her üç format için)
```

> Demo uygulaması (`Program.cs`) yalnızca `.toon` ve `.json` dosyalarını kullanır. YAML dönüşümü için
> `ToonNet.Extensions.Yaml` paketi gerekir; demo projesi bu pakete referans vermez (`.yaml` dosyaları karşılaştırma içindir).

### ✅ İki farklı yaklaşım:

#### **1. String-Based Conversions (Dosya → Dosya)**
```csharp
using ToonNet.Extensions.Json;

// JSON string → TOON string
string json = File.ReadAllText("ecommerce-order.json");
string toon = ToonConvert.FromJson(json);

// TOON string → JSON string
string toon = File.ReadAllText("ecommerce-order.toon");
string json = ToonConvert.ToJson(toon);
```

#### **2. Object-Based Conversions (Type-Safe)**
```csharp
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;

// JSON → C# Object → TOON
var order = ToonConvert.DeserializeFromJson<ECommerceOrder>(jsonString);
string toon = ToonSerializer.Serialize(order);

// TOON → C# Object → JSON
var order = ToonSerializer.Deserialize<ECommerceOrder>(toonString);
string json = ToonConvert.SerializeToJson(order);
```

---

## 🔥 Kompleks Veri Tipleri (TOON Spec Desteği)

### ✅ Desteklenen Tipler:

1. **Primitives**
   - `string` (with escaping: `\"`, `\\`, `\n`, `\r`, `\t`)
   - `int`, `long`, `decimal`, `double`, `float`
   - `bool` (true/false)
   - `null`

2. **Dates & Times**
   - `DateTime` (ISO 8601 format, `Kind` korunur)
   - `DateTimeOffset`
   - `DateOnly`, `TimeOnly`, `TimeSpan`

3. **Collections**
   - `List<T>`, `HashSet<T>`, immutable collections
   - `T[]` (arrays)
   - `Dictionary<TKey, T>` (string ve primitive key'ler)

4. **Nested Objects**
   - Derin nesting (`ToonOptions.MaxDepth` sınırına kadar)
   - Complex object graphs
   - Circular reference'lar property path ile birlikte exception fırlatır (`$.Next` gibi)

5. **Nullable Types**
   - `string?`
   - `int?`
   - `DateTime?`

6. **Special Types**
   - `Guid`
   - Enums
   - Records
   - Structs

---

## 📖 Developer Guide

### Senaryo 1: Dosya Bazlı Dönüşüm

```csharp
using ToonNet.Core.Serialization;

using ToonNet.Extensions.Json;

// Örnek 1: JSON dosyası → TOON dosyası
var jsonContent = await File.ReadAllTextAsync("ecommerce-order.json");
var toonContent = ToonConvert.FromJson(jsonContent);
await File.WriteAllTextAsync("output.toon", toonContent);

// Örnek 2: TOON dosyası → JSON dosyası
var toonContent = await File.ReadAllTextAsync("ecommerce-order.toon");
var jsonContent = ToonConvert.ToJson(toonContent);
await File.WriteAllTextAsync("output.json", jsonContent);
```

### Senaryo 2: Type-Safe Dönüşüm

```csharp
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;
using ToonNet.Demo.Samples;

// JSON → C# Object
var jsonString = await File.ReadAllTextAsync("ecommerce-order.json");
var order = ToonConvert.DeserializeFromJson<ECommerceOrder>(jsonString);

// C# Object → TOON
string toonString = ToonSerializer.Serialize(order);
await File.WriteAllTextAsync("order.toon", toonString);

// TOON → C# Object
var toonString = await File.ReadAllTextAsync("ecommerce-order.toon");
var order = ToonSerializer.Deserialize<ECommerceOrder>(toonString);

// C# Object → JSON
string jsonString = ToonConvert.SerializeToJson(order);
```

### Senaryo 3: Çift Taraflı Roundtrip Test

```csharp
// Original data
var original = new ECommerceOrder { /* ... */ };

// Object → TOON → Object
string toon1 = ToonSerializer.Serialize(original);
var fromToon = ToonSerializer.Deserialize<ECommerceOrder>(toon1);

// Object → JSON → Object  
string json1 = ToonConvert.SerializeToJson(original);
var fromJson = ToonConvert.DeserializeFromJson<ECommerceOrder>(json1);

// JSON → TOON → JSON (string-based roundtrip)
string json2 = await File.ReadAllTextAsync("ecommerce-order.json");
string toon2 = ToonConvert.FromJson(json2);
string json3 = ToonConvert.ToJson(toon2);

// Verify
Console.WriteLine($"TOON roundtrip: {original.OrderId == fromToon.OrderId}");
Console.WriteLine($"JSON roundtrip: {original.OrderId == fromJson.OrderId}");
Console.WriteLine($"String roundtrip: {json2 == json3}");
```

---

## 🎓 Öğrenme Noktaları

### 1. **TOON Format Özellikleri**
- Okunabilir (JSON'dan daha az noktalama)
- Compact (bu örneklerde JSON'dan küçük, YAML ile benzer boyutta)
- TOON spec v3.3.2 formatını izler
- Human-friendly (kolay düzenlenebilir)

### 2. **Nested Arrays**

Sample dosyalarındaki list formu (`ToonSerializer`, yalnızca primitive alanlı uniform object dizilerini
tabular formda yazar: `Reviews[2]{Rating,Comment}:`):

```toon
Items[3]:
  - ProductId: PROD-12345
    Name: "Premium Headphones"
    Reviews[2]:
      - Rating: 5
        Comment: "Excellent!"
      - Rating: 4
        Comment: "Good"
```

### 3. **Null Handling**
```toon
DischargeDate: null
GiftMessage: null
```

### 4. **Mixed Collections**
```toon
Attributes:
  Color: Black
  Warranty: "2 years"
  InStock: "true"
```

---

## ✅ Doğrulama

Her sample için:
1. ✅ **Parse Edilebilir**: TOON/JSON/YAML dosyaları okunabiliyor (demo `.toon` ve `.json` dosyalarını okur)
2. ✅ **Roundtrip**: JSON → TOON → JSON değerleri korur (sayı formatı değişebilir, aşağıya bakın)
3. ✅ **Çeşitli Tipler**: string, sayı, bool, null, tarih, liste, dictionary, nested object
4. ✅ **Real-World**: Gerçek kullanım senaryoları
5. ✅ **Developer-Friendly**: Açık, anlaşılır, kafa karıştırmayan

---

## 🚀 Çalıştırma

```bash
cd demo/ToonNet.Demo
dotnet run
```

Demo otomatik olarak:
1. `ecommerce-order.toon` ve `healthcare-patient.toon` dosyalarını okuyup typed object'lere deserialize eder
2. TOON → JSON ve JSON → TOON dönüşümleri yapar
3. JSON → TOON → JSON roundtrip kontrolünü değer bazında yapar (`35.00` ile `35` eşit sayılır)
4. Sonuçları console'a yazdırır; bir değer farklıysa ilk farkın yolunu yazar ve exit code 1 ile çıkar

---

## 📊 Karşılaştırma

### E-Commerce Order Sample
| Format | Dosya Boyutu |
|--------|--------------|
| TOON   | 2.6 KB       |
| JSON   | 3.6 KB       |
| YAML   | 2.7 KB       |

### Healthcare Patient Record Sample
| Format | Dosya Boyutu |
|--------|--------------|
| TOON   | 4.5 KB       |
| JSON   | 6.2 KB       |
| YAML   | 4.8 KB       |

**TOON avantajları:**
- Bu örneklerde indent'li JSON'dan yaklaşık %27-28 daha küçük
- YAML'dan biraz daha küçük (%3-7)
- Human-readable ve kolay düzenlenebilir

Parse hızı ölçülmedi; performans için [benchmark projesine](../../../benchmark/ToonNet.Benchmarks/README.md) bakın.

---

## 💉 Healthcare Örneği - Özel Kullanım

Healthcare sample'ı için gerçek dünya senaryosu:

```csharp
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;
using ToonNet.Demo.Samples;

// Load patient record from TOON file
var toonData = await File.ReadAllTextAsync("healthcare-patient.toon");
var patient = ToonSerializer.Deserialize<PatientRecord>(toonData);

// Access nested data easily
Console.WriteLine($"Patient: {patient.PatientInfo.FirstName} {patient.PatientInfo.LastName}");
Console.WriteLine($"Blood Type: {patient.PatientInfo.BloodType}");
Console.WriteLine($"Status: {patient.Status}");

// Work with time-series data
var latestVitals = patient.VitalSigns.OrderByDescending(v => v.Timestamp).First();
Console.WriteLine($"Latest Temperature: {latestVitals.Temperature}°{latestVitals.TemperatureUnit}");
Console.WriteLine($"Blood Pressure: {latestVitals.BloodPressure.Systolic}/{latestVitals.BloodPressure.Diastolic}");

// Filter active medications
var activeMeds = patient.Medications.Where(m => m.IsActive).ToList();
Console.WriteLine($"\nActive Medications: {activeMeds.Count}");
foreach (var med in activeMeds)
{
    Console.WriteLine($"  - {med.Name} ({med.Dosage}): {med.Frequency}");
}

// Check critical allergies
var criticalAllergies = patient.Allergies.Where(a => a.Severity == "Critical").ToList();
if (criticalAllergies.Any())
{
    Console.WriteLine("\n⚠️  CRITICAL ALLERGIES:");
    foreach (var allergy in criticalAllergies)
    {
        Console.WriteLine($"  - {allergy.Allergen}: {allergy.Reaction}");
    }
}

// Export to JSON for API integration
var jsonData = ToonConvert.SerializeToJson(patient);
await File.WriteAllTextAsync("patient-export.json", jsonData);

// Convert between formats (EMR integration)
var jsonString = await File.ReadAllTextAsync("healthcare-patient.json");
var toonString = ToonConvert.FromJson(jsonString);
await File.WriteAllTextAsync("patient-converted.toon", toonString);
```

**Key Features Demonstrated:**
- ✅ Complex nested structures (Insurance, EmergencyContact, BloodPressure)
- ✅ Collections with dictionaries (Lab Results with Units)
- ✅ Time-series data (VitalSigns over time)
- ✅ Nullable values (DischargeDate, EndDate)
- ✅ Empty arrays (SideEffects can be [])
- ✅ Mixed data types (string, int, double, DateTime, bool, null)
- ✅ Real-world medical coding (ICD-10, CPT codes)
- ✅ Type-safe deserialization with full IntelliSense support

---

## ⚠️ CRITICAL: Roundtrip Guarantees (Mutlaka Okuyun!)

ToonNet **iki farklı roundtrip garantisi** sunar:

### 1️⃣ Type-Safe Roundtrip (Strongly-Typed) - ✅ TAM KORUMA

**C# class'lar ile çalışırken property değerleri korunur:**

```csharp
// Original object
var order = new ECommerceOrder 
{ 
    OrderId = "ORD-123",
    Pricing = new PricingInfo { GrandTotal = 35.00m }  // decimal precision
};

// TOON'a serialize et, sonra geri deserialize et
string toon = ToonSerializer.Serialize(order);
var order2 = ToonSerializer.Deserialize<ECommerceOrder>(toon);

// ✅ Değerler aynı (decimal eşitliği scale'e bakmaz)
Assert.Equal(35.00m, order2.Pricing.GrandTotal);  // TOON'da "35" yazılır, 35m olarak okunur
```

**Garanti:** C# object → TOON → C# object roundtrip'inde desteklenen tipler için **değer kaybı yok**. Sayılar
kanonik formda yazılır, bu yüzden `decimal` scale'i (`35.00m` → `35m`) korunmaz.

---

### 2️⃣ Format Conversion (String-Based) - ⚠️ SEMANTİK EŞİTLİK

**JSON/TOON string dönüşümlerinde semantik eşitlik garanti, format detayları değişebilir:**

```csharp
// Original JSON
string json = @"{ ""discount"": 35.00 }";

// Dönüşüm: JSON → TOON → JSON
string toon = ToonConvert.FromJson(json);   // discount: 35
string json2 = ToonConvert.ToJson(toon);    // {"discount": 35}

// ⚠️ Format değişti: 35.00 → 35
// ✅ Semantik olarak eşit: 35.00 == 35 (aynı değer)
```

**Format conversion'da NELERdeğişebilir:**
- ❌ Decimal trailing zeros: `35.00` → `35` (semantik olarak eşit)
- ❌ Whitespace: girinti, satır sonları (kozmetik)
- ❌ Number gösterimi: `1e2` → `100` (semantik olarak eşit)

**Format conversion'da NELERgaranti edilir:**
- ✅ Tüm property isimleri ve sırası korunur
- ✅ Tüm değerler korunur (semantik eşitlik)
- ✅ Tüm nested yapılar korunur
- ✅ null/true/false tam olarak korunur
- ✅ String içerik tam olarak korunur

---

### Neden Bu Önemli?

**ToonNet sayıları kanonik formda yazar** (TOON spec v3.3.2): trailing zero'lar ve exponent gösterimi normalize
edilir. Bu, sayıyı bir değer olarak okuyup yeniden yazan serializer'larda yaygındır; ancak her kütüphane böyle
davranmaz:

**System.Text.Json'dan örnek (orijinal sayı metnini korur):**
```csharp
string json1 = @"{ ""value"": 35.00 }";
var obj = JsonSerializer.Deserialize<JsonElement>(json1);
string json2 = JsonSerializer.Serialize(obj);
// Sonuç: {"value":35.00}  ← JsonElement ham sayı metnini saklar
```

Bu yüzden JSON → TOON → JSON dönüşümünden sonra string karşılaştırması yerine değer karşılaştırması yapın.

---

### Best Practices (En İyi Uygulamalar)

#### ✅ Production Kodda Type-Safe Serialization Kullan

```csharp
// ✅ ÖNERİLİR: Tam roundtrip garantisi
var order = ToonSerializer.Deserialize<ECommerceOrder>(toonString);
order.Status = "Shipped";
string toon = ToonSerializer.Serialize(order);
// Değerler korunur (GrandTotal == 35.00m; scale 35m olarak okunur)
```

#### ⚠️ Veri Dönüşümünde Format Conversion Kullan

```csharp
// ⚠️ KULLANIM: Format dönüşümü (dosya, API)
string json = await File.ReadAllTextAsync("order.json");
string toon = ToonConvert.FromJson(json);
await File.WriteAllTextAsync("order.toon", toon);
// Data korunur, format detayları değişebilir (veri dönüşümünde OK)
```

#### 🚫 Doğrulama İçin String Karşılaştırma Kullanma

```csharp
// ❌ KÖTÜ: String karşılaştırma format farkları yüzünden fail olur
string json1 = @"{ ""discount"": 35.00 }";
string json2 = ToonConvert.ToJson(ToonConvert.FromJson(json1));
Assert.Equal(json1, json2);  // ❌ FAIL: "35.00" vs "35"

// ✅ İYİ: Semantik karşılaştırma
var obj1 = JsonSerializer.Deserialize<JsonElement>(json1);
var obj2 = JsonSerializer.Deserialize<JsonElement>(json2);
Assert.Equal(obj1.GetProperty("discount").GetDecimal(), 
             obj2.GetProperty("discount").GetDecimal());  // ✅ PASS
```

---

### Özet Tablo

| Senaryo | Roundtrip Tipi | Garanti | Ne Zaman Kullan |
|---------|---------------|---------|-----------------|
| **C# → TOON → C#** | Type-Safe | Tam Koruma | Production kod, veri saklama |
| **JSON → TOON → JSON** | Format Conversion | Semantik Eşitlik | Dosya dönüşümü, API entegrasyonu |
| **YAML → TOON → YAML** | Format Conversion | Semantik Eşitlik | Config dosya migration |

**Ana Nokta**: 
- **Tam veri koruması** gerekiyor? → **Strongly-typed serialization** kullan ✅
- **Format dönüşümü** yapıyorsun? → **Semantik eşitlik** bekle (değerler eşit, format farklı olabilir) ⚠️

JSON RFC 8259, sayıların gösterimini (ör. `35.00` ve `35`) değil değerini tanımlar; bu dönüşüm spec'e uygundur.

---

## 🎯 Sonuç

Bu samples, ToonNet'in:
- ✅ Yaygın .NET veri tiplerini desteklediğini
- ✅ Kompleks nested structures ile çalıştığını
- ✅ Çift taraflı dönüşüm yaptığını
- ✅ Healthcare, E-Commerce gibi gerçekçi veri modelleriyle çalıştığını

**gösterir.**
