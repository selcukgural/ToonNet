using System.Text.Json;
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;

namespace ToonNet.Demo;

static class Program
{
    /// <returns>0 when every sample loads and round-trips, 1 otherwise.</returns>
    private static int Main(string[] args)
    {
        // Demo: Real-World Sample Files with Full Type Support
        return DemoRealWorldSamples() ? 0 : 1;
    }

    private static bool DemoRealWorldSamples()
    {
        PrintSectionHeader("Real-World Sample Files Demo");

        var samplesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples");
        
        // Demo 1: E-Commerce Order
        var ecommerceOk = DemoECommerceOrder(samplesPath);
        
        // Demo 2: Healthcare Patient Record
        var healthcareOk = DemoHealthcarePatient(samplesPath);

        return ecommerceOk && healthcareOk;
    }

    private static bool DemoECommerceOrder(string samplesPath)
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
                Console.WriteLine("❌ Sample files not found.");
                return false;
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
            
            if (!VerifyRoundtrip(jsonContent, toonFromJson))
            {
                return false;
            }

            Console.WriteLine();
            Console.WriteLine("E-Commerce sample completed successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return false;
        }
    }

    private static bool DemoHealthcarePatient(string samplesPath)
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
                Console.WriteLine("❌ Sample files not found.");
                return false;
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
            
            if (!VerifyRoundtrip(jsonContent, toonFromJson))
            {
                return false;
            }

            Console.WriteLine();
            Console.WriteLine("Healthcare sample completed successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Converts the TOON produced from <paramref name="originalJson"/> back to JSON and compares the values.
    ///     Numbers are compared by value, because TOON writes them in canonical form (spec §2: <c>35.00</c> becomes <c>35</c>).
    /// </summary>
    private static bool VerifyRoundtrip(string originalJson, string toonFromJson)
    {
        var roundtripJson = ToonConvert.ToJson(toonFromJson);

        using var original = JsonDocument.Parse(originalJson);
        using var roundtrip = JsonDocument.Parse(roundtripJson);

        var difference = FindDifference(original.RootElement, roundtrip.RootElement, "$");

        if (difference == null)
        {
            Console.WriteLine("   Roundtrip verification: PASSED (JSON -> TOON -> JSON keeps every value)");
            return true;
        }

        Console.WriteLine($"   Roundtrip verification: FAILED at {difference}");
        return false;
    }

    /// <summary>
    ///     Returns the path of the first value that differs, or null when both elements hold the same values.
    /// </summary>
    private static string? FindDifference(JsonElement expected, JsonElement actual, string path)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            return $"{path} ({expected.ValueKind} vs {actual.ValueKind})";
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProperties = expected.EnumerateObject().ToList();

                if (expectedProperties.Count != actual.EnumerateObject().Count())
                {
                    return $"{path} (property count)";
                }

                foreach (var property in expectedProperties)
                {
                    if (!actual.TryGetProperty(property.Name, out var actualValue))
                    {
                        return $"{path}.{property.Name} (missing)";
                    }

                    if (FindDifference(property.Value, actualValue, $"{path}.{property.Name}") is { } nested)
                    {
                        return nested;
                    }
                }

                return null;

            case JsonValueKind.Array:
                if (expected.GetArrayLength() != actual.GetArrayLength())
                {
                    return $"{path} (array length)";
                }

                var index = 0;

                foreach (var (expectedItem, actualItem) in expected.EnumerateArray().Zip(actual.EnumerateArray()))
                {
                    if (FindDifference(expectedItem, actualItem, $"{path}[{index++}]") is { } nested)
                    {
                        return nested;
                    }
                }

                return null;

            case JsonValueKind.Number:
                var same = expected.TryGetDecimal(out var left) && actual.TryGetDecimal(out var right)
                    ? left == right
                    : expected.GetDouble().Equals(actual.GetDouble());

                return same ? null : $"{path} ({expected.GetRawText()} vs {actual.GetRawText()})";

            case JsonValueKind.String:
                return expected.GetString() == actual.GetString() ? null : $"{path} (\"{expected.GetString()}\" vs \"{actual.GetString()}\")";

            default:
                return null;
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
