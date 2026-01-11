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
        Console.WriteLine("║        ToonNet Comprehensive Demo & Type Support Proof       ║");
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

        // Demo 5: Anonymous Types
        DemoAnonymousTypes();

        // Demo 6: Format Conversions (TOON ↔ JSON ↔ YAML)
        DemoFormatConversions();

        Console.WriteLine();
        Console.WriteLine("✅ All demos completed successfully!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static void DemoPrimitiveTypes()
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

    private static void DemoCollectionsAndNesting()
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

    private static void DemoEnumsAndComplexModels()
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

    private static void DemoAnonymousTypes()
    {
        PrintSectionHeader("5. Anonymous Types Support");

        // Simple anonymous type
        var person = new
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 30,
            Email = "john@example.com"
        };

        Console.WriteLine("Simple Anonymous Type:");
        Console.WriteLine($"  Type: {person.GetType().Name}");
        Console.WriteLine($"  Properties: FirstName={person.FirstName}, Age={person.Age}");
        
        var personToon = ToonSerializer.Serialize(person);
        Console.WriteLine($"  TOON: {personToon}");
        Console.WriteLine();

        // Nested anonymous type
        var company = new
        {
            Name = "TechStartup Inc.",
            FoundedYear = 2020,
            CEO = new
            {
                Name = "Alice Johnson",
                Age = 35,
                Email = "alice@techstartup.com"
            },
            Employees = new[]
            {
                new { Name = "Bob Smith", Role = "Developer", Salary = 80000 },
                new { Name = "Carol White", Role = "Designer", Salary = 75000 }
            }
        };

        Console.WriteLine("Nested Anonymous Type:");
        Console.WriteLine($"  Company: {company.Name} (Founded: {company.FoundedYear})");
        Console.WriteLine($"  CEO: {company.CEO.Name}, Age: {company.CEO.Age}");
        Console.WriteLine($"  Employees: {company.Employees.Length}");
        
        var companyToon = ToonSerializer.Serialize(company);
        Console.WriteLine($"  TOON ({companyToon.Length} chars):");
        Console.WriteLine(TruncateString(companyToon, 600));
        Console.WriteLine();

        // Collection of anonymous types
        var products = new[]
        {
            new { Id = 1, Name = "Laptop", Price = 1299.99m, InStock = true },
            new { Id = 2, Name = "Mouse", Price = 29.99m, InStock = true },
            new { Id = 3, Name = "Keyboard", Price = 89.99m, InStock = false }
        };

        Console.WriteLine("Array of Anonymous Types:");
        foreach (var product in products)
        {
            Console.WriteLine($"  - {product.Name}: ${product.Price} (Stock: {product.InStock})");
        }
        
        var productsToon = ToonSerializer.Serialize(products);
        Console.WriteLine($"  TOON ({productsToon.Length} chars):");
        Console.WriteLine(TruncateString(productsToon, 500));
        Console.WriteLine();

        // Complex anonymous type with mixed collections
        var dashboard = new
        {
            Title = "Sales Dashboard",
            LastUpdated = DateTime.UtcNow,
            Metrics = new
            {
                TotalSales = 125000m,
                OrderCount = 450,
                AverageOrderValue = 277.78m
            },
            TopProducts = new[]
            {
                new { Product = "Laptop", Sales = 45000m },
                new { Product = "Phone", Sales = 38000m },
                new { Product = "Tablet", Sales = 25000m }
            },
            SalesByRegion = new Dictionary<string, decimal>
            {
                { "North", 45000m },
                { "South", 35000m },
                { "East", 25000m },
                { "West", 20000m }
            }
        };

        Console.WriteLine("Complex Anonymous Type with Mixed Collections:");
        Console.WriteLine($"  {dashboard.Title}");
        Console.WriteLine($"  Total Sales: ${dashboard.Metrics.TotalSales:N0}");
        Console.WriteLine($"  Orders: {dashboard.Metrics.OrderCount}");
        Console.WriteLine($"  Top Products: {dashboard.TopProducts.Length}");
        Console.WriteLine($"  Regions: {dashboard.SalesByRegion.Count}");
        
        var dashboardToon = ToonSerializer.Serialize(dashboard);
        Console.WriteLine($"  TOON ({dashboardToon.Length} chars):");
        Console.WriteLine(TruncateString(dashboardToon, 700));
        Console.WriteLine();

        // Dynamic query-like anonymous type (LINQ-style)
        var queryResult = new
        {
            Query = "SELECT * FROM Orders WHERE Status = 'Completed'",
            ExecutionTime = TimeSpan.FromMilliseconds(125),
            RowCount = 234,
            Results = new[]
            {
                new { OrderId = 1001, Customer = "John Doe", Amount = 299.99m },
                new { OrderId = 1002, Customer = "Jane Smith", Amount = 599.50m }
            }
        };

        Console.WriteLine("Query Result Anonymous Type:");
        Console.WriteLine($"  Rows: {queryResult.RowCount}, Time: {queryResult.ExecutionTime.TotalMilliseconds}ms");
        
        var queryToon = ToonSerializer.Serialize(queryResult);
        Console.WriteLine($"  TOON ({queryToon.Length} chars):");
        Console.WriteLine(queryToon);
        Console.WriteLine();

        Console.WriteLine("✓ All anonymous type scenarios serialized successfully!");
        Console.WriteLine("  Note: Anonymous types are read-only and cannot be deserialized back");
        Console.WriteLine("        (they have no public constructor and are compiler-generated)");
        Console.WriteLine();
    }

    private static void DemoFormatConversions()
    {
        PrintSectionHeader("6. Format Conversions (TOON ↔ JSON ↔ YAML)");

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
