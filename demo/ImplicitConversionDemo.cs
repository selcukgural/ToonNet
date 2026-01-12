using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

namespace ToonNet.Demo;

/// <summary>
/// Demonstrates the implicit conversion feature in ToonNet.
/// Shows how ToonNet matches System.Text.Json's clean API.
/// </summary>
public static class ImplicitConversionDemo
{
    /// <summary>
    /// Main demo entry point.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("=== ToonNet Implicit Conversion Demo ===\n");

        SimpleObjectDemo();
        Console.WriteLine();

        NestedObjectDemo();
        Console.WriteLine();

        ArrayDemo();
        Console.WriteLine();

        ComplexOrderDemo();
        Console.WriteLine();

        Console.WriteLine("=== Demo Complete! ===");
    }

    /// <summary>
    /// Demo: Simple object with implicit conversions
    /// </summary>
    private static void SimpleObjectDemo()
    {
        Console.WriteLine("1. Simple Object with Implicit Conversions:");
        Console.WriteLine("-------------------------------------------");

        // Clean syntax with implicit conversions
        var user = new ToonObject
        {
            ["name"] = "Alice",      // string → ToonString (implicit!)
            ["age"] = 30,            // int → ToonNumber (implicit!)
            ["isActive"] = true      // bool → ToonBoolean (implicit!)
        };

        var encoder = new ToonEncoder();
        Console.WriteLine(encoder.Encode(new ToonDocument(user)));
    }

    /// <summary>
    /// Demo: Nested objects
    /// </summary>
    private static void NestedObjectDemo()
    {
        Console.WriteLine("2. Nested Objects:");
        Console.WriteLine("------------------");

        // ✅ Clean syntax with implicit conversions
        var user = new ToonObject
        {
            ["name"] = "Bob",
            ["age"] = 35,
            ["address"] = new ToonObject
            {
                ["street"] = "123 Main St",
                ["city"] = "New York",
                ["zipCode"] = "10001"
            }
        };

        var encoder = new ToonEncoder();
        Console.WriteLine(encoder.Encode(new ToonDocument(user)));
    }

    /// <summary>
    /// Demo: Arrays with implicit conversions
    /// </summary>
    private static void ArrayDemo()
    {
        Console.WriteLine("3. Arrays with Implicit Conversions:");
        Console.WriteLine("------------------------------------");

        // Numbers
        var numbers = new ToonArray();
        numbers.Add(10);       // int → ToonNumber
        numbers.Add(20.5);     // double → ToonNumber
        numbers.Add(30);       // int → ToonNumber

        // Mixed types
        var mixed = new ToonArray();
        mixed.Add("Hello");    // string → ToonString
        mixed.Add(42);         // int → ToonNumber
        mixed.Add(true);       // bool → ToonBoolean

        var encoder = new ToonEncoder();
        Console.WriteLine("Numbers:");
        Console.WriteLine(encoder.Encode(new ToonDocument(numbers)));
        Console.WriteLine();
        Console.WriteLine("Mixed types:");
        Console.WriteLine(encoder.Encode(new ToonDocument(mixed)));
    }

    /// <summary>
    /// Demo: Complex real-world order structure
    /// </summary>
    private static void ComplexOrderDemo()
    {
        Console.WriteLine("4. Complex Order (Real-World Example):");
        Console.WriteLine("---------------------------------------");

        // Clean syntax with implicit conversions
        var order = new ToonObject
        {
            ["orderId"] = "ORD-12345",
            ["orderDate"] = "2024-01-15",
            ["customer"] = new ToonObject
            {
                ["id"] = 123,
                ["name"] = "John Doe",
                ["email"] = "john@example.com",
                ["phone"] = "+1-555-1234"
            },
            ["items"] = new ToonArray
            {
                Items =
                {
                    new ToonObject
                    {
                        ["product"] = "Laptop",
                        ["sku"] = "LAP-001",
                        ["quantity"] = 1,
                        ["unitPrice"] = 999.99,
                        ["discount"] = 50.00
                    },
                    new ToonObject
                    {
                        ["product"] = "Mouse",
                        ["sku"] = "MSE-002",
                        ["quantity"] = 2,
                        ["unitPrice"] = 25.50,
                        ["discount"] = 0.00
                    }
                }
            },
            ["shipping"] = new ToonObject
            {
                ["method"] = "Express",
                ["cost"] = 15.00,
                ["estimatedDays"] = 2
            },
            ["subtotal"] = 1000.99,
            ["tax"] = 85.08,
            ["total"] = 1086.07,
            ["isPaid"] = true,
            ["isShipped"] = false
        };

        // Add null value (explicit ToonNull.Instance required)
        order["trackingNumber"] = ToonNull.Instance;

        var encoder = new ToonEncoder();
        Console.WriteLine(encoder.Encode(new ToonDocument(order)));
    }
}
