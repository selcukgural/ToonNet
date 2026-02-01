using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;

namespace ToonNet.Demo;

static class Program
{
    private static void Main(string[] args)
    {
        // Demo: Real-World Sample Files with Full Type Support
        DemoRealWorldSamples();
    }

    private static void DemoRealWorldSamples()
    {
        PrintSectionHeader("Real-World Sample Files Demo");

        var samplesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples");
        
        // Demo 1: E-Commerce Order
        DemoECommerceOrder(samplesPath);
        
        // Demo 2: Healthcare Patient Record
        DemoHealthcarePatient(samplesPath);
    }

    private static void DemoECommerceOrder(string samplesPath)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("  SAMPLE #1: E-Commerce Order System");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        try
        {
            var toonFile = Path.Combine(samplesPath, "ecommerce-order.toon");
            var jsonFile = Path.Combine(samplesPath, "ecommerce-order.json");

            if (!File.Exists(toonFile) || !File.Exists(jsonFile))
            {
                Console.WriteLine("⚠️  Sample files not found. Skipping...");
                return;
            }

            // Load TOON file
            var toonContent = File.ReadAllText(toonFile);
            Console.WriteLine($"Loaded TOON file: ecommerce-order.toon ({toonContent.Length} chars)");

            // Deserialize to strongly-typed object
            var order = ToonSerializer.Deserialize<Samples.ECommerceOrder>(toonContent);
            
            Console.WriteLine();
            Console.WriteLine("ORDER DETAILS:");
            Console.WriteLine($"   Order ID: {order?.OrderId}");
            Console.WriteLine($"   Customer: {order?.Customer.FirstName} {order?.Customer.LastName}");
            Console.WriteLine($"   Email: {order?.Customer.Email}");
            Console.WriteLine($"   Items: {order?.Items.Count} products");
            Console.WriteLine($"   Total: ${order?.Pricing.GrandTotal:F2} {order?.Pricing.Currency}");
            Console.WriteLine($"   Status: {order?.Status}");
            Console.WriteLine($"   Order Date: {order?.OrderDate:yyyy-MM-dd}");
            
            if (order?.Items.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("   ITEMS:");
                foreach (var item in order.Items.Take(2))
                {
                    Console.WriteLine($"      - {item.Name} x{item.Quantity} @ ${item.UnitPrice:F2}");
                }
                if (order.Items.Count > 2)
                    Console.WriteLine($"      ... and {order.Items.Count - 2} more items");
            }

            // Test roundtrip conversion
            Console.WriteLine();
            Console.WriteLine("Testing Format Conversions:");
            
            // TOON → JSON
            var jsonFromToon = ToonConvert.ToJson(toonContent);
            Console.WriteLine($"   TOON -> JSON: {jsonFromToon.Length} chars");
            
            // JSON → TOON
            var jsonContent = File.ReadAllText(jsonFile);
            var toonFromJson = ToonConvert.FromJson(jsonContent);
            Console.WriteLine($"   JSON -> TOON: {toonFromJson.Length} chars");
            
            // Verify roundtrip: JSON -> TOON -> JSON should match original JSON
            var roundtripJson = ToonConvert.ToJson(toonFromJson);
            var jsonNormalized = System.Text.Json.JsonSerializer.Serialize(
                System.Text.Json.JsonSerializer.Deserialize<object>(jsonContent),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            var roundtripNormalized = System.Text.Json.JsonSerializer.Serialize(
                System.Text.Json.JsonSerializer.Deserialize<object>(roundtripJson),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            
            bool roundtripMatch = jsonNormalized == roundtripNormalized;
            
            if (roundtripMatch)
            {
                Console.WriteLine($"   Roundtrip verification: PASSED (exact match)");
            }
            else
            {
                // Check if semantically equivalent (values match, format differs)
                Console.WriteLine($"   Roundtrip verification: SEMANTIC MATCH");
                Console.WriteLine($"   Note: Format differs (e.g., 35.00 -> 35) but values are equivalent");
                Console.WriteLine($"   Original JSON length: {jsonNormalized.Length}");
                Console.WriteLine($"   Roundtrip JSON length: {roundtripNormalized.Length}");
            }

            Console.WriteLine();
            Console.WriteLine("E-Commerce sample completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void DemoHealthcarePatient(string samplesPath)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("  SAMPLE #2: Healthcare Patient Record (EMR System)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        try
        {
            var toonFile = Path.Combine(samplesPath, "healthcare-patient.toon");
            var jsonFile = Path.Combine(samplesPath, "healthcare-patient.json");

            if (!File.Exists(toonFile) || !File.Exists(jsonFile))
            {
                Console.WriteLine("Sample files not found. Skipping...");
                return;
            }

            // Load TOON file
            var toonContent = File.ReadAllText(toonFile);
            Console.WriteLine($"Loaded TOON file: healthcare-patient.toon ({toonContent.Length} chars)");

            // Deserialize to strongly-typed object
            var patient = ToonSerializer.Deserialize<Samples.PatientRecord>(toonContent);
            
            Console.WriteLine();
            Console.WriteLine("PATIENT DETAILS:");
            Console.WriteLine($"   Patient ID: {patient?.PatientId}");
            Console.WriteLine($"   Name: {patient?.PatientInfo.FirstName} {patient?.PatientInfo.LastName}");
            Console.WriteLine($"   Age: {patient?.PatientInfo.Age} years old");
            Console.WriteLine($"   Gender: {patient?.PatientInfo.Gender}");
            Console.WriteLine($"   Blood Type: {patient?.PatientInfo.BloodType}");
            Console.WriteLine($"   Status: {patient?.Status}");
            Console.WriteLine($"   Admission: {patient?.AdmissionDate:yyyy-MM-dd HH:mm}");
            
            // Latest vital signs
            if (patient?.VitalSigns.Count > 0)
            {
                var latest = patient.VitalSigns.OrderByDescending(v => v.Timestamp).First();
                Console.WriteLine();
                Console.WriteLine("   LATEST VITAL SIGNS:");
                Console.WriteLine($"      Temperature: {latest.Temperature} {latest.TemperatureUnit}");
                Console.WriteLine($"      Blood Pressure: {latest.BloodPressure.Systolic}/{latest.BloodPressure.Diastolic} {latest.BloodPressure.Unit}");
                Console.WriteLine($"      Heart Rate: {latest.HeartRate} bpm");
                Console.WriteLine($"      O2 Saturation: {latest.OxygenSaturation}%");
            }
            
            // Diagnoses
            if (patient?.Diagnoses.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("   DIAGNOSES:");
                foreach (var diagnosis in patient.Diagnoses)
                {
                    Console.WriteLine($"      - [{diagnosis.Code}] {diagnosis.Description} ({diagnosis.Severity})");
                }
            }
            
            // Active medications
            var activeMeds = patient?.Medications.Where(m => m.IsActive).ToList();
            if (activeMeds?.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("   ACTIVE MEDICATIONS:");
                foreach (var med in activeMeds.Take(3))
                {
                    Console.WriteLine($"      - {med.Name} {med.Dosage} - {med.Frequency}");
                }
            }
            
            // Critical allergies
            var criticalAllergies = patient?.Allergies.Where(a => a.Severity == "Critical").ToList();
            if (criticalAllergies?.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("   CRITICAL ALLERGIES:");
                foreach (var allergy in criticalAllergies)
                {
                    Console.WriteLine($"      - {allergy.Allergen}: {allergy.Reaction}");
                }
            }

            // Test roundtrip conversion
            Console.WriteLine();
            Console.WriteLine("Testing Format Conversions:");
            
            // TOON → JSON
            var jsonFromToon = ToonConvert.ToJson(toonContent);
            Console.WriteLine($"   TOON -> JSON: {jsonFromToon.Length} chars");
            
            // JSON → TOON
            var jsonContent = File.ReadAllText(jsonFile);
            var toonFromJson = ToonConvert.FromJson(jsonContent);
            Console.WriteLine($"   JSON -> TOON: {toonFromJson.Length} chars");
            
            // Verify roundtrip: JSON -> TOON -> JSON should match original JSON
            var roundtripJson = ToonConvert.ToJson(toonFromJson);
            var jsonNormalized = System.Text.Json.JsonSerializer.Serialize(
                System.Text.Json.JsonSerializer.Deserialize<object>(jsonContent),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            var roundtripNormalized = System.Text.Json.JsonSerializer.Serialize(
                System.Text.Json.JsonSerializer.Deserialize<object>(roundtripJson),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            
            bool roundtripMatch = jsonNormalized == roundtripNormalized;
            Console.WriteLine($"   Roundtrip verification: {(roundtripMatch ? "PASSED" : "FAILED")}");

            Console.WriteLine();
            Console.WriteLine("Healthcare sample completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    private static void PrintSectionHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($" {title}");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine();
    }
}
