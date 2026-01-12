# ToonNet.Demo

**Real-world sample applications demonstrating ToonNet features**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Samples](https://img.shields.io/badge/samples-2%20real--world-success)](#)

---

## 📦 What is ToonNet.Demo?

ToonNet.Demo showcases **real-world applications** of ToonNet serialization:

- ✅ **E-Commerce Order System** - Complex order management
- ✅ **Healthcare EMR** - Patient records with medical data
- ✅ **Format Conversions** - JSON ↔ TOON roundtrip validation
- ✅ **Production-Quality Models** - Realistic data structures
- ✅ **Complete Examples** - From loading to validation

---

## 🚀 Quick Start

### Running the Demo

```bash
cd demo/ToonNet.Demo
dotnet run
```

### Demo Output

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 Real-World Sample Files Demo
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

═══════════════════════════════════════════════════════════
  SAMPLE #1: E-Commerce Order System
═══════════════════════════════════════════════════════════

Loaded TOON file: ecommerce-order.toon (2731 chars)

ORDER DETAILS:
   Order ID: ORD-2026-00142857
   Customer: Sarah Johnson
   Email: sarah.johnson@example.com
   Items: 3 products
   Total: $838.91 USD
   Status: Processing

Testing Format Conversions:
   TOON -> JSON: 3601 chars
   JSON -> TOON: 2917 chars
   Roundtrip verification: SEMANTIC MATCH

E-Commerce sample completed successfully!

═══════════════════════════════════════════════════════════
  SAMPLE #2: Healthcare Patient Record (EMR System)
═══════════════════════════════════════════════════════════

Loaded TOON file: healthcare-patient.toon (4882 chars)

PATIENT DETAILS:
   Patient ID: MRN-2026-987654
   Name: Michael Chen
   Age: 40 years old
   Blood Type: A+

   LATEST VITAL SIGNS:
      Temperature: 98.7 F
      Heart Rate: 73 bpm
      O2 Saturation: 98%

Testing Format Conversions:
   TOON -> JSON: 6276 chars
   JSON -> TOON: 4906 chars
   Roundtrip verification: PASSED

Healthcare sample completed successfully!
```

---

## 📂 Sample Files

### Sample #1: E-Commerce Order

**Location:** `Samples/ecommerce-order.*`

**Files:**
- `ecommerce-order.toon` (2.7 KB) - TOON format
- `ecommerce-order.json` (2.6 KB) - JSON format
- `ecommerce-order.yaml` (2.2 KB) - YAML format
- `ECommerceModels.cs` - C# model classes

**Models:**
```csharp
public class ECommerceOrder
{
    public string OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public Customer Customer { get; set; }
    public List<OrderItem> Items { get; set; }
    public PaymentInfo Payment { get; set; }
    public ShippingInfo Shipping { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; }
}
```

**Features Demonstrated:**
- Complex nested objects (Customer, Items, Payment, Shipping)
- Collections (List<OrderItem>)
- Dictionaries (Product attributes)
- Decimal precision
- DateTime handling
- Enum-like status strings

**TOON Sample:**
```toon
OrderId: ORD-2026-00142857
OrderDate: 2026-01-11T14:30:00Z
Customer:
  CustomerId: CUST-789012
  Name: Sarah Johnson
  Email: sarah.johnson@example.com
Items[3]:
  - ProductId: PROD-001
    Name: Premium Wireless Headphones
    Quantity: 2
    UnitPrice: 349.99
    SubTotal: 699.98
  - ProductId: PROD-002
    Name: USB-C Charging Cable (3-Pack)
    Quantity: 1
    UnitPrice: 24.99
```

### Sample #2: Healthcare Patient Record

**Location:** `Samples/healthcare-patient.*`

**Files:**
- `healthcare-patient.toon` (4.9 KB) - TOON format
- `healthcare-patient.json` (6.1 KB) - JSON format
- `healthcare-patient.yaml` (4.7 KB) - YAML format
- `HealthcareModels.cs` - C# model classes

**Models:**
```csharp
public class PatientRecord
{
    public string RecordId { get; set; }
    public PatientInfo Patient { get; set; }
    public List<VitalSigns> VitalSignsHistory { get; set; }
    public List<Diagnosis> Diagnoses { get; set; }
    public List<Medication> Medications { get; set; }
    public List<LabResult> LabResults { get; set; }
    public List<Procedure> Procedures { get; set; }
    public List<Allergy> Allergies { get; set; }
    public Physician AttendingPhysician { get; set; }
    public DateTime AdmissionDate { get; set; }
}
```

**Features Demonstrated:**
- Medical data structures
- Time-series data (vital signs history)
- Code systems (ICD-10 diagnosis codes)
- Nullable values (discharge date)
- Units of measurement
- Complex nested hierarchies (14 classes)

**TOON Sample:**
```toon
RecordId: EMR-2026-001
Patient:
  PatientId: MRN-2026-987654
  Name: Michael Chen
  DateOfBirth: 1985-05-15
  Gender: Male
  BloodType: A+
VitalSignsHistory[2]:
  - Timestamp: 2026-01-10T08:00:00Z
    Temperature: 98.6
    BloodPressure:
      Systolic: 120
      Diastolic: 80
    HeartRate: 72
    RespiratoryRate: 16
    OxygenSaturation: 98
```

---

## 🎯 Key Demonstrations

### 1. Type-Safe Serialization

All models use strongly-typed C# classes with full property definitions:

```csharp
// Load TOON file
string toonContent = File.ReadAllText("ecommerce-order.toon");

// Deserialize to typed object
var order = ToonSerializer.Deserialize<ECommerceOrder>(toonContent);

// Access properties with IntelliSense
Console.WriteLine($"Order ID: {order.OrderId}");
Console.WriteLine($"Customer: {order.Customer.Name}");
Console.WriteLine($"Total: ${order.Total:F2}");
```

### 2. Format Conversion

Demonstrates seamless conversion between formats:

```csharp
using ToonNet.Extensions.Json;

// TOON → JSON
string toonString = File.ReadAllText("order.toon");
string jsonString = ToonConvert.ToJson(toonString);

// JSON → TOON
string jsonContent = File.ReadAllText("order.json");
string toonString = ToonConvert.FromJson(jsonContent);
```

### 3. Roundtrip Validation

Verifies data integrity through format conversions:

```csharp
// Original JSON
string originalJson = File.ReadAllText("order.json");

// JSON → TOON → JSON
string toonString = ToonConvert.FromJson(originalJson);
string roundtripJson = ToonConvert.ToJson(toonString);

// Validate semantic equivalence
bool isEquivalent = CompareJsonSemantically(originalJson, roundtripJson);
// Result: true (values match, format may differ)
```

### 4. Complex Data Structures

Shows handling of realistic, production-grade data:

```csharp
// E-Commerce: 10 classes, 40+ properties
// Healthcare: 14 classes, 60+ properties
// Both: Nested objects, collections, dictionaries

var patient = ToonSerializer.Deserialize<PatientRecord>(toonContent);

// Access deeply nested data
var latestVitals = patient.VitalSignsHistory.First();
var systolic = latestVitals.BloodPressure.Systolic;
var diagnoses = patient.Diagnoses
    .Where(d => d.Severity == "Moderate")
    .ToList();
```

---

## 📊 Sample Statistics

| Sample | TOON Size | JSON Size | YAML Size | Models | Properties |
|--------|-----------|-----------|-----------|--------|------------|
| **E-Commerce** | 2.7 KB | 2.6 KB | 2.2 KB | 10 | 40+ |
| **Healthcare** | 4.9 KB | 6.1 KB | 4.7 KB | 14 | 60+ |

**Token Efficiency (approximate):**
- E-Commerce: TOON ~680 tokens vs JSON ~650 tokens
- Healthcare: TOON ~1,220 tokens vs JSON ~1,525 tokens (20% reduction!)

---

## 🔗 Related Samples

### Detailed Sample Documentation

See [`Samples/README.md`](Samples/README.md) for:
- Detailed Turkish documentation
- Code examples and usage patterns
- Roundtrip behavior explanations
- Best practices

### Sample Files

Each sample includes three formats for comparison:
- **TOON** - Human-readable, token-efficient
- **JSON** - Industry standard
- **YAML** - Configuration-friendly

---

## 🧪 Running Tests

Demo includes basic validation:

```bash
# Run demo with validation
dotnet run

# Check exit code
echo $?  # 0 = success, 1 = failure
```

---

## 🔗 Related Packages

**Core:**
- [`ToonNet.Core`](../../src/ToonNet.Core) - Core serialization

**Extensions:**
- [`ToonNet.Extensions.Json`](../../src/ToonNet.Extensions.Json) - JSON conversion
- [`ToonNet.Extensions.Yaml`](../../src/ToonNet.Extensions.Yaml) - YAML conversion

**Testing:**
- [`ToonNet.Tests`](../../tests/ToonNet.Tests) - Comprehensive test suite

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete ToonNet guide
- [API Guide](../../docs/API-GUIDE.md) - Detailed API reference
- [Samples Guide](Samples/README.md) - Detailed sample documentation (Turkish)

---

## 📋 Requirements

- .NET 8.0 or later
- ToonNet.Core
- ToonNet.Extensions.Json (for JSON conversion)
- System.Text.Json (built-in)
- YamlDotNet (for YAML samples)

---

## 🤝 Contributing

Want to add more samples? Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

**Sample Guidelines:**
- Use real-world scenarios (not toy examples)
- Include all three formats (TOON, JSON, YAML)
- Provide complete C# models with XML docs
- Add validation and roundtrip tests

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

**Part of the [ToonNet](../../README.md) serialization library family.**
