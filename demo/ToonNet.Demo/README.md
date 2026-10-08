# ToonNet.Demo

**Real-world sample applications demonstrating ToonNet features**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Samples](https://img.shields.io/badge/samples-2%20real--world-success)](#)

---

## 📦 What is ToonNet.Demo?

ToonNet.Demo showcases **real-world applications** of ToonNet serialization:

- ✅ **E-Commerce Order System** - Complex order management
- ✅ **Healthcare EMR** - Patient records with medical data
- ✅ **Format Conversions** - JSON ↔ TOON roundtrip check with `ToonConvert`
- ✅ **Realistic Models** - Nested objects, lists, dictionaries, nullable values
- ✅ **Complete Examples** - Load a `.toon` file, deserialize it, convert it to JSON and back

---

## 🚀 Quick Start

### Running the Demo

```bash
# From the repository root
dotnet run --project demo/ToonNet.Demo

# Or from the project folder
cd demo/ToonNet.Demo
dotnet run
```

The demo is not interactive: it runs both samples and exits. The sample files are copied to the output folder
(`bin/<Configuration>/net8.0/Samples`) at build time and loaded from there.

### Demo Output

Prices and decimals are formatted with the current culture (shown here with `.` as the decimal separator).

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 Real-World Sample Files Demo
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━


═══════════════════════════════════════════════════════════════════════════════
  SAMPLE #1: E-Commerce Order System
═══════════════════════════════════════════════════════════════════════════════

Loaded TOON file: ecommerce-order.toon (2609 chars)

ORDER DETAILS:
   Order ID: ORD-2026-00142857
   Customer: Sarah Johnson
   Email: sarah.johnson@example.com
   Items: 3 products
   Total: $838.91 USD
   Status: Processing
   Order Date: 2026-01-11

   ITEMS:
      - Premium Wireless Headphones x2 @ $349.99
      - USB-C Charging Cable (3-Pack) x1 @ $24.99
      ... and 1 more items

Testing Format Conversions:
   TOON -> JSON: 2578 chars
   JSON -> TOON: 2600 chars
   Roundtrip verification: PASSED (JSON -> TOON -> JSON keeps every value)

E-Commerce sample completed successfully!

═══════════════════════════════════════════════════════════════════════════════
  SAMPLE #2: Healthcare Patient Record (EMR System)
═══════════════════════════════════════════════════════════════════════════════

Loaded TOON file: healthcare-patient.toon (4476 chars)

PATIENT DETAILS:
   Patient ID: MRN-2026-987654
   Name: Michael Chen
   Age: 40 years old
   Gender: Male
   Blood Type: A+
   Status: Active
   Admission: 2026-01-10 08:15

   LATEST VITAL SIGNS:
      Temperature: 98.7 F
      Blood Pressure: 119/79 mmHg
      Heart Rate: 73 bpm
      O2 Saturation: 98%

   DIAGNOSES:
      - [J18.9] Pneumonia, unspecified organism (Moderate)
      - [E11.9] Type 2 diabetes mellitus without complications (Mild)

   ACTIVE MEDICATIONS:
      - Azithromycin 500mg - Once daily
      - Metformin 1000mg - Twice daily
      - Lisinopril 10mg - Once daily

   CRITICAL ALLERGIES:
      - Shellfish: Anaphylaxis

Testing Format Conversions:
   TOON -> JSON: 4523 chars
   JSON -> TOON: 4475 chars
   Roundtrip verification: PASSED (JSON -> TOON -> JSON keeps every value)

Healthcare sample completed successfully!
```

> **Note:** The roundtrip check compares values, not JSON text: TOON writes numbers in canonical form (spec §2), so
> `35.00` comes back as `35`. If any value differs, the demo prints the path of the first difference and exits with
> code 1 (also when a sample is missing or fails to load).

---

## 📂 Sample Files

### Sample #1: E-Commerce Order

**Location:** `Samples/ecommerce-order.*`

**Files:**
- `ecommerce-order.toon` (2.6 KB) - TOON format
- `ecommerce-order.json` (3.6 KB) - JSON format
- `ecommerce-order.yaml` (2.7 KB) - YAML format
- `ECommerceModels.cs` - C# model classes

**Models:**
```csharp
public class ECommerceOrder
{
    public string OrderId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Customer Customer { get; set; } = new();
    public Address ShippingAddress { get; set; } = new();
    public Address BillingAddress { get; set; } = new();
    public List<OrderItem> Items { get; set; } = new();
    public PaymentInfo PaymentInfo { get; set; } = new();
    public ShippingInfo Shipping { get; set; } = new();
    public PricingInfo Pricing { get; set; } = new();
    public OrderMetadata Metadata { get; set; } = new();
}
```

**Features Demonstrated:**
- Complex nested objects (Customer, addresses, PaymentInfo, Shipping, Pricing)
- Collections (List<OrderItem>, nested List<ProductReview>)
- Dictionaries (Product attributes)
- Decimal precision
- DateTime handling
- Enum-like status strings

**TOON Sample (excerpt; see the `Samples` folder for the full file):**
```toon
OrderId: ORD-2026-00142857
OrderDate: "2026-01-11T15:30:00.0000000Z"
Status: Processing
Customer:
  CustomerId: CUST-98765
  FirstName: Sarah
  LastName: Johnson
  Email: sarah.johnson@example.com
  ...
Items[3]:
  - ProductId: PROD-12345
    Name: Premium Wireless Headphones
    Category: Electronics
    SKU: WH-1000XM5-BLK
    Quantity: 2
    UnitPrice: 349.99
    Discount: 35
    TaxRate: 0.08
    Total: 664.98
    Attributes:
      Color: Black
      Warranty: 2 years
      InStock: "true"
    Reviews[2]{Rating,Comment,Verified}:
      5,Excellent sound quality!,true
      4,Great but a bit pricey,true
  ...
```

### Sample #2: Healthcare Patient Record

**Location:** `Samples/healthcare-patient.*`

**Files:**
- `healthcare-patient.toon` (4.5 KB) - TOON format
- `healthcare-patient.json` (6.2 KB) - JSON format
- `healthcare-patient.yaml` (4.8 KB) - YAML format
- `HealthcareModels.cs` - C# model classes

**Models:**
```csharp
public class PatientRecord
{
    public string PatientId { get; set; } = string.Empty;
    public string RecordNumber { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public PatientInfo PatientInfo { get; set; } = new();
    public List<VitalSigns> VitalSigns { get; set; } = new();
    public List<Diagnosis> Diagnoses { get; set; } = new();
    public List<Medication> Medications { get; set; } = new();
    public List<LabResult> LabResults { get; set; } = new();
    public List<Procedure> Procedures { get; set; } = new();
    public List<Physician> AttendingPhysicians { get; set; } = new();
    public List<Allergy> Allergies { get; set; } = new();
}
```

**Features Demonstrated:**
- Medical data structures
- Time-series data (vital signs history)
- Code systems (ICD-10 diagnosis codes)
- Nullable values (discharge date)
- Units of measurement
- Complex nested hierarchies (12 classes)

**TOON Sample (excerpt; see the `Samples` folder for the full file):**
```toon
PatientId: MRN-2026-987654
RecordNumber: EMR-HSP-00142857
AdmissionDate: "2026-01-10T08:15:00.0000000Z"
DischargeDate: null
Status: Active
PatientInfo:
  FirstName: Michael
  MiddleName: Robert
  LastName: Chen
  DateOfBirth: "1985-06-15T00:00:00.0000000"
  Age: 40
  Gender: Male
  BloodType: A+
  ...
VitalSigns[4]:
  - Timestamp: "2026-01-10T08:30:00.0000000Z"
    Temperature: 98.6
    TemperatureUnit: F
    BloodPressure:
      Systolic: 120
      Diastolic: 80
      Unit: mmHg
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
string toonContent = File.ReadAllText("Samples/ecommerce-order.toon");

// Deserialize to typed object
var order = ToonSerializer.Deserialize<ECommerceOrder>(toonContent);

// Access properties with IntelliSense
Console.WriteLine($"Order ID: {order.OrderId}");
Console.WriteLine($"Customer: {order.Customer.FirstName} {order.Customer.LastName}");
Console.WriteLine($"Total: ${order.Pricing.GrandTotal:F2}");
```

### 2. Format Conversion

Demonstrates seamless conversion between formats:

```csharp
using ToonNet.Extensions.Json;

// TOON → JSON
string toonContent = File.ReadAllText("Samples/ecommerce-order.toon");
string jsonFromToon = ToonConvert.ToJson(toonContent);

// JSON → TOON
string jsonContent = File.ReadAllText("Samples/ecommerce-order.json");
string toonFromJson = ToonConvert.FromJson(jsonContent);
```

### 3. Roundtrip Validation

Verifies data integrity through format conversions:

```csharp
// Original JSON
string originalJson = File.ReadAllText("Samples/healthcare-patient.json");

// JSON → TOON → JSON
string toonString = ToonConvert.FromJson(originalJson);
string roundtripJson = ToonConvert.ToJson(toonString);

// Compare after normalizing both with System.Text.Json (no indentation)
var compact = new JsonSerializerOptions { WriteIndented = false };
string Normalize(string json) =>
    JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(json), compact);

bool roundtripMatch = Normalize(originalJson) == Normalize(roundtripJson);
// Healthcare sample: true. E-commerce sample: false, because 35.00 comes back as 35.
```

### 4. Complex Data Structures

Shows handling of realistic, production-grade data:

```csharp
// E-Commerce: 10 classes, 70+ properties
// Healthcare: 12 classes, 80+ properties
// Both: Nested objects, collections, dictionaries

var patient = ToonSerializer.Deserialize<PatientRecord>(toonContent);

// Access deeply nested data
var latestVitals = patient.VitalSigns.OrderByDescending(v => v.Timestamp).First();
var systolic = latestVitals.BloodPressure.Systolic;
var diagnoses = patient.Diagnoses
    .Where(d => d.Severity == "Moderate")
    .ToList();
```

---

## 📊 Sample Statistics

| Sample | TOON Size | JSON Size | YAML Size | Models | Properties |
|--------|-----------|-----------|-----------|--------|------------|
| **E-Commerce** | 2.6 KB | 3.6 KB | 2.7 KB | 10 | 70+ |
| **Healthcare** | 4.5 KB | 6.2 KB | 4.8 KB | 12 | 80+ |

Sizes are of the indented files in `Samples/` (the JSON uses camelCase keys, the TOON files PascalCase). The demo does
not count tokens; token savings depend on the tokenizer and on the shape of the data.

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

The demo prints its checks ("Roundtrip verification: ...") but does not set an exit code: errors are caught and
printed, and the process always exits with `0`. The real test suite lives in [`tests/`](../../tests):

```bash
# From the repository root
dotnet test ToonNet.slnx
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
- System.Text.Json (package reference 10.0.1)
- YamlDotNet 16.3.0 (used by `Converters/FormatConverter.cs`; the demo's `Main` does not load the `.yaml` files)

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
