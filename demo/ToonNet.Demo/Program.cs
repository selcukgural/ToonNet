using System;
using System.Diagnostics;
using ToonNet.Core.Serialization;
using ToonNet.Demo.Models;
using ToonNet.Demo.Helpers;
using ToonNet.Demo.Converters;

namespace ToonNet.Demo;

static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        ToonNet Comprehensive Demo & Type Support Proof      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Demo 1: Primitive Types
        DemoPrimitiveTypes();

        // Demo 2: Collections and Nested Types
        DemoCollectionsAndNesting();

        // Demo 3: Enums and Complex Models
        DemoEnumsAndComplexModels();

        // Demo 4: Records and Structs
        DemoRecordsAndStructs();

        // Demo 5: Format Conversions (TOON ↔ JSON ↔ YAML)
        DemoFormatConversions();

        Console.WriteLine();
        Console.WriteLine("✅ All demos completed successfully!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static void DemoPrimitiveTypes()
    {
        PrintSectionHeader("1. Primitive Types Support");

        var primitives = DataGenerator.CreatePrimitiveTypes();
        var toonString = ToonSerializer.Serialize(primitives);

        Console.WriteLine("Original Object:");
        Console.WriteLine($"  ByteValue: {primitives.ByteValue}");
        Console.WriteLine($"  FloatValue: {primitives.FloatValue}");
        Console.WriteLine($"  DecimalValue: {primitives.DecimalValue}");
        Console.WriteLine($"  BoolValue: {primitives.BoolValue}");
        Console.WriteLine($"  StringValue: {primitives.StringValue}");
        Console.WriteLine($"  DateTimeValue: {primitives.DateTimeValue}");
        Console.WriteLine($"  GuidValue: {primitives.GuidValue}");
        Console.WriteLine();

        Console.WriteLine("TOON Representation (first 500 chars):");
        Console.WriteLine(TruncateString(toonString, 500));
        Console.WriteLine();

        var deserialized = ToonSerializer.Deserialize<PrimitiveTypesModel>(toonString);
        Console.WriteLine("✓ Deserialization successful!");
        Console.WriteLine($"  Verified: ByteValue = {deserialized?.ByteValue}, BoolValue = {deserialized?.BoolValue}");
        Console.WriteLine();
    }

    static void DemoCollectionsAndNesting()
    {
        PrintSectionHeader("2. Collections & Nested Types");

        var company = DataGenerator.CreateCompany();
        var toonString = ToonSerializer.Serialize(company);

        Console.WriteLine("Company Structure:");
        Console.WriteLine($"  Name: {company.Name}");
        Console.WriteLine($"  City: {company.Address.City}, {company.Address.State}");
        Console.WriteLine($"  Coordinates: ({company.Address.Coordinates.Latitude}, {company.Address.Coordinates.Longitude})");
        Console.WriteLine($"  Departments: {company.Departments.Count}");
        Console.WriteLine($"  Engineering Manager: {company.Departments[0].Manager.FirstName} {company.Departments[0].Manager.LastName}");
        Console.WriteLine($"  Engineering Employees: {company.Departments[0].Employees.Count}");
        Console.WriteLine();

        Console.WriteLine("TOON Representation (first 800 chars):");
        Console.WriteLine(TruncateString(toonString, 800));
        Console.WriteLine();

        var deserialized = ToonSerializer.Deserialize<Company>(toonString);
        Console.WriteLine("✓ Deserialization successful!");
        Console.WriteLine($"  Verified: {deserialized?.Departments.Count} departments, {deserialized?.Departments[0].Employees.Count} employees in Engineering");
        Console.WriteLine();
    }

    static void DemoEnumsAndComplexModels()
    {
        PrintSectionHeader("3. Enums & Complex Models");

        var task = DataGenerator.CreateTask();
        var toonString = ToonSerializer.Serialize(task);

        Console.WriteLine("Task Model:");
        Console.WriteLine($"  ID: {task.Id}");
        Console.WriteLine($"  Title: {task.Title}");
        Console.WriteLine($"  Priority: {task.Priority} (enum value: {(int)task.Priority})");
        Console.WriteLine($"  Status: {task.Status}");
        Console.WriteLine($"  Tags: {string.Join(", ", task.Tags)}");
        Console.WriteLine($"  Metadata entries: {task.Metadata.Count}");
        Console.WriteLine();

        Console.WriteLine("TOON Representation:");
        Console.WriteLine(toonString);
        Console.WriteLine();

        var deserialized = ToonSerializer.Deserialize<TaskModel>(toonString);
        Console.WriteLine("✓ Deserialization successful!");
        Console.WriteLine($"  Verified: Priority={deserialized?.Priority}, Tags={deserialized?.Tags.Count}");
        Console.WriteLine();
    }

    private static void DemoRecordsAndStructs()
    {
        PrintSectionHeader("4. Records & Structs");

        // Record demonstration - Note: Records with primary constructors need special handling
        // For now, we'll show serialization only or use a class-based approach
        var person = new PersonRecord("John", "Doe", 30, "john@example.com");
        var personToon = ToonSerializer.Serialize(person);

        Console.WriteLine("Record (PersonRecord):");
        Console.WriteLine($"  {person}");
        Console.WriteLine($"  TOON: {personToon}");
        Console.WriteLine("  ⚠ Note: Record deserialization requires parameterless constructor or custom handling");
        Console.WriteLine();

        // Struct demonstration
        var rect = new Rectangle
        {
            Width = 1920,
            Height = 1080,
            TopLeft = new Point { X = 0, Y = 0, Z = 0 },
            BottomRight = new Point { X = 1920, Y = 1080, Z = 0 }
        };
        var rectToon = ToonSerializer.Serialize(rect);

        Console.WriteLine("Struct (Rectangle with Point record struct):");
        Console.WriteLine($"  Width: {rect.Width}, Height: {rect.Height}");
        Console.WriteLine($"  TopLeft: {rect.TopLeft}");
        Console.WriteLine($"  TOON: {TruncateString(rectToon, 300)}");

        var rectDeserialized = ToonSerializer.Deserialize<Rectangle>(rectToon);
        Console.WriteLine($"  ✓ Deserialized: Width={rectDeserialized.Width}, Height={rectDeserialized.Height}");
        Console.WriteLine();
    }

    private static void DemoFormatConversions()
    {
        PrintSectionHeader("5. Format Conversions (TOON ↔ JSON ↔ YAML)");

        var company = DataGenerator.CreateCompany();

        // Object → TOON
        var toonString = FormatConverter.ToToon(company);
        Console.WriteLine($"Company TOON Format: ({toonString.Length} chars)");
        Console.WriteLine(TruncateString(toonString, 600));
        Console.WriteLine();

        // TOON → JSON
        var jsonString = FormatConverter.ToonToJson<Company>(toonString);
        Console.WriteLine($"TOON → JSON: ({jsonString.Length} chars)");
        Console.WriteLine(TruncateString(jsonString, 600));
        Console.WriteLine();

        // TOON → YAML
        var yamlString = FormatConverter.ToonToYaml<Company>(toonString);
        Console.WriteLine($"TOON → YAML: ({yamlString.Length} chars)");
        Console.WriteLine(TruncateString(yamlString, 600));
        Console.WriteLine();

        // JSON → TOON
        var toonFromJson = FormatConverter.JsonToToon<Company>(jsonString);
        Console.WriteLine($"✓ JSON → TOON successful ({toonFromJson.Length} chars)");

        // YAML → TOON
        var toonFromYaml = FormatConverter.YamlToToon<Company>(yamlString);
        Console.WriteLine($"✓ YAML → TOON successful ({toonFromYaml.Length} chars)");
        Console.WriteLine();

        // Round-trip verification
        var roundTrip = ToonSerializer.Deserialize<Company>(toonFromJson);
        Console.WriteLine($"✓ Round-trip verification: {roundTrip?.Name}, {roundTrip?.Departments.Count} departments");
        Console.WriteLine();

        Console.WriteLine("✓ All format conversions successful!");
        Console.WriteLine();
    }

    private static void PrintSectionHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($" {title}");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine();
    }

    private static string TruncateString(string str, int maxLength)
    {
        return str.Length <= maxLength ? str : string.Concat(str.AsSpan(0, maxLength), "... (truncated)");
    }
}
