# TOON v3.0 EKSİKLİKLERİ - DETAYLI ANALİZ VE ÇÖZÜM PLANI

**Oluşturma Tarihi:** 10 Ocak 2026  
**Durum:** ⚠️ KRİTİK - Üretim öncesi çözülmesi gerekli  
**Toon Spesifikasyon:** v3.0  

---

## 📊 ÖZET

| Metrik | Değer |
|--------|-------|
| **Toplam Test** | 185 |
| **Geçen Test** | 179 ✅ |
| **Eksik Özellik** | 6 ❌ |
| **Başarı Oranı** | 96.76% |
| **Kritiklik** | **YÜKSEK** |

---

## 🔴 KRİTİK EKSİKLİKLER (Üretim Hazırlığını Engeller)

### 1️⃣ **ALINTILI STRING ANAHTARLAR (Quoted String Keys)**

**Durum:** ❌ DESTEKLENMIYOR  
**Öncelik:** 🔴 ÇOKTA KRİTİK  
**Etki:** Gerçek veride çok yaygın

#### Problem
```toon
"key with spaces": value
"quoted-key": "quoted-value"
"key:with:colons": "value,with,commas"
"@special": true
```

Parser bu syntax'ı tanımıyor ve parse hatası veriyor:
```
ToonParseException: Expected ':' after key
```

#### Neden Önemli?
- **Yaygınlık:** Çoğu gerçek TOON dosyasında kullanılır
- **Uyumluluk:** TOON v3.0 spesifikasyonunun §7 bölümüne göre MUST desteklenmeli
- **Veri Kaybı:** Boşluk içeren anahtarlar şifre/config dosyalarında yaygın

#### Spec Referansı
```
TOON v3.0 §7: Strings & Keys
"Quoted strings are used for keys containing spaces or special characters"
```

#### Çözüm Adımları

**Step 1: Lexer'da String Tanıyıcı Güncelleme**
- Dosya: `src/ToonNet.Core/Parsing/ToonLexer.cs`
- Amaç: Anahtardan sonra gelen alıntı string'i tanımak
- İşlem:
  ```csharp
  // Key tanıma kısmında
  if (char == '"') {
    // Quoted key olarak işle
    var key = ReadQuotedString();
    return new ToonToken(TokenType.Key, key, ...);
  }
  ```

**Step 2: Parser'da Key Parse Etme Güncelleme**
- Dosya: `src/ToonNet.Core/Parsing/ToonParser.cs`
- Amaç: Quoted key tokenlerini handle etmek
- İşlem:
  ```csharp
  private void ParseKeyValue() {
    string key;
    if (CurrentToken.Type == TokenType.QuotedString) {
      key = CurrentToken.Value; // Quoted string i direkt al
      Advance();
    } else {
      key = CurrentToken.Value; // Unquoted key
      Advance();
    }
    ExpectToken(TokenType.Colon);
    // ...
  }
  ```

**Step 3: Encoder Güncelleme**
- Dosya: `src/ToonNet.Core/Encoding/ToonEncoder.cs`
- Amaç: Gerekirse key'leri alıntılı çıktısı
- İşlem:
  ```csharp
  private bool NeedsQuoting(string key) {
    return key.Contains(' ') || 
           key.Contains(':') ||
           key.Contains('{') ||
           key.Contains('}') ||
           key.Contains(',');
  }
  
  if (NeedsQuoting(key)) {
    output.Append($"\"{EscapeString(key)}\"");
  } else {
    output.Append(key);
  }
  ```

**Step 4: Testler**
- Mevcut test: `QuotedStrings_SpecialCharacters_PreservedExactly`
- Unmark'a hazır

**Tahmini Süre:** 45 dakika

---

### 2️⃣ **LİSTE ÖĞESİ DİZİLERİ (List Item Arrays)**

**Durum:** ❌ DESTEKLENMIYOR  
**Öncelik:** 🔴 ÇOKTA KRİTİK  
**Etki:** Çok yaygın JSON benzeri format

#### Problem
```toon
products:
  - name: Laptop
    price: 999.99
    inStock: true
  - name: Mouse
    price: 29.99
```

Parser bunu **object** olarak parse ediyor, **array** değil:
```
Expected: ToonArray(3 elements)
Actual: ToonObject { "-" key exists }
```

#### Neden Önemli?
- **Yaygınlık:** JSON/YAML benzeri verilerde ÇOKKK yaygın
- **Veri Yapısı:** `-` prefix'i Toon v3.0'da array elemanı anlamı taşır
- **Uyumluluk:** Spec §10 (Objects as List Items) MUST desteklenmeli

#### Spec Referansı
```
TOON v3.0 §10: Objects as List Items
"A line beginning with '- ' (hyphen-space) at a given indentation 
 represents one element in a non-uniform array"
```

#### Çözüm Adımları

**Step 1: Lexer'da Dash Token'ı Tanımak**
- Dosya: `src/ToonNet.Core/Parsing/ToonLexer.cs`
- Amaç: `- ` prefix'ini TokenType.ListItem olarak tanımak
- İşlem:
  ```csharp
  if (current == '-' && PeekNext() == ' ') {
    Advance(2); // '-' ve ' ' geç
    return new ToonToken(TokenType.ListItem, "-", position);
  }
  ```

**Step 2: Parser'da Array Yapısı Oluşturma**
- Dosya: `src/ToonNet.Core/Parsing/ToonParser.cs`
- Amaç: `-` prefix olan satırları array elemanı olarak işleme almak
- İşlem:
  ```csharp
  private ToonValue ParseArray(int indentLevel) {
    var items = new List<ToonValue>();
    
    while (CurrentToken.Type == TokenType.ListItem) {
      Advance(); // ListItem token'ı geç
      var value = ParseValue(indentLevel + 1);
      items.Add(value);
    }
    
    return new ToonArray(items);
  }
  ```

**Step 3: Parser Ana Döngüsü Güncelleme**
- Dosya: `src/ToonNet.Core/Parsing/ToonParser.cs`
- Amaç: Key'den sonra list item'ları detect etmek
- İşlem:
  ```csharp
  private ToonValue ParseValue(int indentLevel) {
    if (CurrentToken.Type == TokenType.ListItem) {
      return ParseArray(indentLevel);
    }
    // ... existing code
  }
  ```

**Step 4: Testler**
- Mevcut test: `ArraysOfObjects_ListItemFormat_Parsed`
- Unmark'a hazır

**Tahmini Süre:** 60 dakika

---

### 3️⃣ **TABELALAR DİZİLER (Tabular Arrays)**

**Durum:** ❌ DESTEKLENMIYOR  
**Öncelik:** 🟡 ORTA  
**Etki:** CSV benzeri yapılı veri

#### Problem
```toon
people{name,age,city}
  Alice, 30, New York
  Bob, 25, Los Angeles
  Charlie, 35, Chicago
```

Parser `{...}` syntax'ını tanımıyor:
```
ToonParseException: Expected ':' after key 'people'
```

#### Neden Önemli?
- **Veri Yapısı:** CSV benzeri tabelalar için ideal format
- **Uyumluluk:** Spec §9.3 (Tabular Arrays) önerilen format
- **Verimliliği:** Çok satırlı veri için daha özlü

#### Spec Referansı
```
TOON v3.0 §9.3: Tabular Arrays
"Field headers and rows: key{field1,field2,field3}"
```

#### Çözüm Adımları

**Step 1: Lexer'da Bracket Token'ları**
- Dosya: `src/ToonNet.Core/Parsing/ToonLexer.cs`
- Amaç: `{field1,field2,field3}` yapısını parse etmek
- İşlem:
  ```csharp
  // '{' tokenı
  if (current == '{') {
    return new ToonToken(TokenType.LeftBrace, "{", position);
  }
  // '}' tokenı
  if (current == '}') {
    return new ToonToken(TokenType.RightBrace, "}", position);
  }
  ```

**Step 2: Parser'da Tabular Syntax**
- Dosya: `src/ToonNet.Core/Parsing/ToonParser.cs`
- Amaç: Field header ve row'ları parse etmek
- İşlem:
  ```csharp
  private ToonArray ParseTabularArray(string key, int indentLevel) {
    // Field names: {name,age,city}
    var fieldNames = ParseFieldNames();
    
    // Rows
    var rows = new List<ToonValue>();
    while (IsRowOnNextLine()) {
      var row = ParseTabularRow(fieldNames, indentLevel);
      rows.Add(row);
    }
    
    return new ToonArray(rows, fieldNames);
  }
  
  private string[] ParseFieldNames() {
    ExpectToken(TokenType.LeftBrace);
    var fields = new List<string>();
    
    while (CurrentToken.Type != TokenType.RightBrace) {
      fields.Add(CurrentToken.Value);
      Advance();
      if (CurrentToken.Type == TokenType.Comma) {
        Advance();
      }
    }
    
    ExpectToken(TokenType.RightBrace);
    return fields.ToArray();
  }
  ```

**Step 3: CSV Row Parse Etme**
- İşlem:
  ```csharp
  private ToonObject ParseTabularRow(string[] fieldNames, int indentLevel) {
    var obj = new ToonObject();
    var values = ParseCommaSeparatedValues();
    
    for (int i = 0; i < fieldNames.Length; i++) {
      obj[fieldNames[i]] = values[i];
    }
    
    return obj;
  }
  ```

**Step 4: Testler**
- Mevcut test: `TabularArrays_WithHeaders_Parsed`
- Unmark'a hazır

**Tahmini Süre:** 75 dakika

---

## 🟡 ÖNEMLI EKSİKLİKLER (Veri Bütünlüğü)

### 4️⃣ **KANONİK SAYI FORMATI (Canonical Number Format)**

**Durum:** ⚠️ KISMEN DESTEKLENDI  
**Öncelik:** 🟡 DÜŞÜK  
**Etki:** Round-trip formatı

#### Problem
Encoder çıktısı şu kuralları garantilemiyor:
- ❌ Üstel notation kullanmama (e.g., 1e6)
- ❌ Baştaki sıfırları kaldırma (e.g., 0123 → 123)
- ❌ Sondaki sıfırları kaldırma (e.g., 1.5000 → 1.5)

#### Çözüm Adımları

**Step 1: Number Formatter Oluştur**
- Dosya: `src/ToonNet.Core/Encoding/ToonEncoder.cs`
- İşlem:
  ```csharp
  private string FormatNumber(double value) {
    // Spec §2.1: Canonical format
    
    // Integer mi?
    if (value == Math.Floor(value)) {
      return ((long)value).ToString();
    }
    
    // Decimal: trailing zero'ları kaldır
    var str = value.ToString("G17", CultureInfo.InvariantCulture);
    return RemoveTrailingZeros(str);
  }
  
  private string RemoveTrailingZeros(string number) {
    if (!number.Contains('.')) return number;
    
    number = number.TrimEnd('0');
    if (number.EndsWith('.')) {
      number = number.TrimEnd('.');
    }
    return number;
  }
  ```

**Step 2: Encoder'da Kullan**
- İşlem:
  ```csharp
  if (value is ToonNumber num) {
    output.Append(FormatNumber(num.Value));
  }
  ```

**Step 3: Testler**
- Mevcut test: `NumberFormatting_NoExponents_NoLeadingZeros_NoTrailingZeros`
- Unmark'a hazır

**Tahmini Süre:** 30 dakika

---

### 5️⃣ **ESCAPE SEQUENCE EDGE CASES**

**Durum:** ⚠️ KISMEN DESTEKLENDI  
**Öncelik:** 🟡 ÇOK DÜŞÜK  
**Etki:** Bazı escape kombinasyonları

#### Problem
Bazı escape kombinasyonları round-trip'de yanlış parse:
- `\r\n` kombinasyonu
- Nested backslash'lar
- Unicode escape'ler (varsa)

#### Çözüm Adımları

**Step 1: Escape Parser Güncelleme**
- Dosya: `src/ToonNet.Core/Parsing/ToonLexer.cs`
- Tüm escape kombinasyonlarını kontrol etmek

**Step 2: Escape Encoder Güncelleme**
- Dosya: `src/ToonNet.Core/Encoding/ToonEncoder.cs`
- Çıkış sırasında doğru escape'leme

**Tahmini Süre:** 45 dakika

---

## 📋 UYGULAMAYA BAŞLAMA CHECKLIST'İ

### Faz 1: KRİTİK DÜZELTMELER (Hafta 1)

- [ ] **Quoted String Keys** (45 min)
  - [ ] Lexer token'ı ekle
  - [ ] Parser handling
  - [ ] Encoder quote'lama
  - [ ] Test unmark
  - [ ] Build & run tests

- [ ] **List Item Arrays** (60 min)
  - [ ] Lexer ListItem token'ı
  - [ ] Parser array logic
  - [ ] Encoding support
  - [ ] Test unmark
  - [ ] Build & run tests

**Toplam: ~2 saat, 179→191 test passing**

---

### Faz 2: ORTA ÖNCELİK (Hafta 2)

- [ ] **Tabular Arrays** (75 min)
  - [ ] Lexer brace token'ları
  - [ ] Parser tabular logic
  - [ ] Field name parsing
  - [ ] Row parsing
  - [ ] Encoding support
  - [ ] Test unmark
  - [ ] Build & run tests

- [ ] **Number Format** (30 min)
  - [ ] Canonical formatter oluştur
  - [ ] Encoder'a entegre et
  - [ ] Test unmark
  - [ ] Build & run tests

**Toplam: ~1.75 saat, 191→200 test passing**

---

### Faz 3: DÜŞÜK ÖNCELİK (Hafta 3)

- [ ] **Escape Edge Cases** (45 min)
  - [ ] Escape combinations test
  - [ ] Lexer güncelleme
  - [ ] Encoder güncelleme
  - [ ] Test unmark
  - [ ] Build & run tests

**Toplam: 45 min, 200→206 test passing (tümü geçer)**

---

## 🎯 BAŞLAMADAN ÖNCE

### Hazırladığımız Şeyler ✅
- Test suite: `tests/ToonNet.Tests/SpecCompliance/ToonSpecComplianceTests.cs`
- Skip sebepleri: Hepsi test dosyasında belirtildi
- Regression prevention: Tüm testler korunuyor

### Her Adımda
```
1. Kodu değiştir
2. dotnet build ToonNet.sln -c Debug
3. dotnet test ToonNet.sln --no-build
4. Hata varsa geri dön adım 1'e
5. Başarılıysa test'i unmark et
6. Tüm 185 test geçene kadar devam
```

### Commit Strategy
Her faz sonunda:
```bash
git add -A
git commit -m "Fix: [Feature name] - TOON v3.0 spec compliance"
```

---

## 📊 HEDEFLER

| Hedef | Başlangıç | Bitiş | Süre |
|-------|-----------|-------|------|
| Quoted Keys | 179 ✅ | 185 ✅ | 45 min |
| List Items | 185 ✅ | 191 ✅ | 60 min |
| **Faz 1 Toplam** | **179 ✅** | **191 ✅** | **2 saat** |
| Tabular Arrays | 191 ✅ | 197 ✅ | 75 min |
| Number Format | 197 ✅ | 200 ✅ | 30 min |
| **Faz 2 Toplam** | **191 ✅** | **200 ✅** | **1.75 saat** |
| Escape Cases | 200 ✅ | 206 ✅ | 45 min |
| **TOPLAM** | **179 ✅** | **206 ✅** | **~4.5 saat** |

---

## ⚠️ RİSK ANALIZI

### Düşük Risk
- ✅ Quoted keys: Lexer/parser değişikliği, test mevcuttur
- ✅ List items: Parser logic, isolated change

### Orta Risk
- ⚠️ Tabular arrays: Daha karmaşık parser logic
- ⚠️ Number format: Tüm encoder'a etki edebilir

### Mitigation
- Her değişiklik sadece ilgili test'i unmark eder
- Regression suite bütün test'leri koruyor
- Adım adım ilerleme, incremental integration

---

## 📞 HAZIR OLDUĞUNDA

Şu komutları çalıştır:
```bash
cd /Users/selcuk/RiderProjects/ToonNet

# Build check
dotnet build ToonNet.sln -c Debug

# Test check
dotnet test ToonNet.sln --no-build

# Eğer tümü geçerse: ✅ Hazır!
```

**Bekliyorum - başlamaya hazır mısın?**
