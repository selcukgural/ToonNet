using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

namespace ToonNet.Tests.Models;

/// <summary>
/// Tests for implicit conversion operators in ToonValue types.
/// </summary>
public class ToonValueImplicitConversionTests
{
    [Fact]
    public void ToonObject_WithImplicitConversions_ShouldWorkLikeSystemTextJson()
    {
        // Arrange & Act - Using implicit conversions (like System.Text.Json)
        var user = new ToonObject
        {
            ["name"] = "John Doe",           // string → ToonString
            ["age"] = 30,                     // int → ToonNumber
            ["height"] = 5.9,                 // double → ToonNumber
            ["salary"] = 75000.50m,           // decimal → ToonNumber
            ["isActive"] = true,              // bool → ToonBoolean
            ["department"] = "Engineering"    // string → ToonString
        };

        // Null values need explicit ToonNull.Instance
        user["manager"] = ToonNull.Instance;

        // Assert - Check types
        Assert.IsType<ToonString>(user["name"]);
        Assert.IsType<ToonNumber>(user["age"]);
        Assert.IsType<ToonNumber>(user["height"]);
        Assert.IsType<ToonNumber>(user["salary"]);
        Assert.IsType<ToonBoolean>(user["isActive"]);
        Assert.IsType<ToonString>(user["department"]);
        Assert.IsType<ToonNull>(user["manager"]);

        // Assert - Check values
        Assert.Equal("John Doe", ((ToonString)user["name"]!).Value);
        Assert.Equal(30, ((ToonNumber)user["age"]!).Value);
        Assert.Equal(5.9, ((ToonNumber)user["height"]!).Value);
        Assert.Equal(75000.50, ((ToonNumber)user["salary"]!).Value);
        Assert.True(((ToonBoolean)user["isActive"]!).Value);
        Assert.Equal("Engineering", ((ToonString)user["department"]!).Value);
    }

    [Fact]
    public void ToonObject_NestedWithImplicitConversions_ShouldWork()
    {
        // Arrange & Act - Complex nested structure with implicit conversions
        var order = new ToonObject
        {
            ["orderId"] = "ORD-123",
            ["customer"] = new ToonObject
            {
                ["name"] = "Alice",
                ["email"] = "alice@example.com"
            },
            ["total"] = 1050.99,
            ["isPaid"] = true
        };

        // Assert
        var customer = (ToonObject)order["customer"]!;
        Assert.Equal("Alice", ((ToonString)customer["name"]!).Value);
        Assert.Equal("alice@example.com", ((ToonString)customer["email"]!).Value);
        Assert.Equal(1050.99, ((ToonNumber)order["total"]!).Value);
        Assert.True(((ToonBoolean)order["isPaid"]!).Value);
    }

    [Fact]
    public void ToonArray_WithImplicitConversions_ShouldWork()
    {
        // Arrange & Act
        var numbers = new ToonArray();
        numbers.Add(10);       // int → ToonNumber
        numbers.Add(20.5);     // double → ToonNumber
        numbers.Add(30L);      // long → ToonNumber
        numbers.Add(40.5f);    // float → ToonNumber

        // Assert
        Assert.Equal(4, numbers.Count);
        Assert.Equal(10, ((ToonNumber)numbers[0]).Value);
        Assert.Equal(20.5, ((ToonNumber)numbers[1]).Value);
        Assert.Equal(30, ((ToonNumber)numbers[2]).Value);
        Assert.Equal(40.5, ((ToonNumber)numbers[3]).Value, precision: 1);
    }

    [Fact]
    public void ToonNumber_AllNumericTypes_ShouldConvertImplicitly()
    {
        // Arrange & Act
        ToonNumber fromInt = 42;
        ToonNumber fromLong = 123456789L;
        ToonNumber fromFloat = 3.14f;
        ToonNumber fromDouble = 2.718281828;
        ToonNumber fromDecimal = 99.99m;

        // Assert
        Assert.Equal(42, fromInt.Value);
        Assert.Equal(123456789, fromLong.Value);
        Assert.Equal(3.14, fromFloat.Value, precision: 2);
        Assert.Equal(2.718281828, fromDouble.Value);
        Assert.Equal(99.99, fromDecimal.Value);
    }

    [Fact]
    public void ToonBoolean_ShouldConvertImplicitly()
    {
        // Arrange & Act
        ToonBoolean trueValue = true;
        ToonBoolean falseValue = false;

        // Assert
        Assert.True(trueValue.Value);
        Assert.False(falseValue.Value);

        // Reverse conversion
        bool extractedTrue = trueValue;
        bool extractedFalse = falseValue;
        Assert.True(extractedTrue);
        Assert.False(extractedFalse);
    }

    [Fact]
    public void ToonString_ShouldConvertImplicitly()
    {
        // Arrange & Act
        ToonString hello = "Hello, World!";
        ToonString? nullString = (string?)null;

        // Assert
        Assert.Equal("Hello, World!", hello.Value);
        Assert.Null(nullString);

        // Reverse conversion
        string extracted = hello;
        Assert.Equal("Hello, World!", extracted);
    }

    [Fact]
    public void ToonValue_BaseClass_ShouldSupportImplicitConversions()
    {
        // Arrange & Act - Using ToonValue base type
        ToonValue stringValue = "test";
        ToonValue intValue = 42;
        ToonValue doubleValue = 3.14;
        ToonValue boolValue = true;
        ToonValue? nullValue = (string?)null;

        // Assert
        Assert.IsType<ToonString>(stringValue);
        Assert.IsType<ToonNumber>(intValue);
        Assert.IsType<ToonNumber>(doubleValue);
        Assert.IsType<ToonBoolean>(boolValue);
        Assert.IsType<ToonNull>(nullValue);
    }

    [Fact]
    public void RealWorldExample_UserProfile_WithImplicitConversions()
    {
        // Arrange & Act - Real-world user profile
        var profile = new ToonObject
        {
            ["userId"] = 12345,
            ["username"] = "johndoe",
            ["email"] = "john@example.com",
            ["age"] = 30,
            ["height"] = 5.9,
            ["weight"] = 165.5,
            ["isVerified"] = true,
            ["isPremium"] = false,
            ["bio"] = "Software engineer",
            ["settings"] = new ToonObject
            {
                ["theme"] = "dark",
                ["notifications"] = true,
                ["language"] = "en"
            }
        };
        
        // Null values need explicit ToonNull.Instance
        profile["website"] = ToonNull.Instance;

        var encoder = new ToonEncoder();
        var document = new ToonDocument(profile);
        var toon = encoder.Encode(document);

        // Assert - Check TOON output
        Assert.Contains("userId: 12345", toon);
        Assert.Contains("username: johndoe", toon);
        Assert.Contains("age: 30", toon);
        Assert.Contains("isVerified: true", toon);
        Assert.Contains("website: null", toon);
        Assert.Contains("theme: dark", toon);
    }
}
