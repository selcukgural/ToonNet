using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

namespace ToonNet.Tests.Encoding;

public class ToonEncoderTests
{
    [Fact]
    public void Encode_SimpleObject_ReturnsCorrectString()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["name"] = new ToonString("Alice"),
            ["age"] = new ToonNumber(30)
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("name: Alice", result);
        Assert.Contains("age: 30", result);
    }

    [Fact]
    public void Encode_NestedObject_ReturnsIndentedString()
    {
        // Arrange
        var user = new ToonObject
        {
            ["name"] = new ToonString("Alice"),
            ["age"] = new ToonNumber(30)
        };

        var root = new ToonObject
        {
            ["user"] = user
        };

        var doc = new ToonDocument(root);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("user:", result);
        Assert.Contains("  name: Alice", result);
        Assert.Contains("  age: 30", result);
    }

    [Fact]
    public void Encode_PrimitiveArray_ReturnsInlineArray()
    {
        // Arrange
        var array = new ToonArray();
        array.Items.Add(new ToonString("admin"));
        array.Items.Add(new ToonString("user"));
        array.Items.Add(new ToonString("guest"));

        var root = new ToonObject
        {
            ["tags"] = array
        };

        var doc = new ToonDocument(root);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("tags[3]: admin,user,guest", result);
    }

    [Fact]
    public void Encode_TabularArray_ReturnsFormattedArray()
    {
        // Arrange
        var user1 = new ToonObject
        {
            ["id"] = new ToonNumber(1),
            ["name"] = new ToonString("Alice"),
            ["role"] = new ToonString("admin")
        };

        var user2 = new ToonObject
        {
            ["id"] = new ToonNumber(2),
            ["name"] = new ToonString("Bob"),
            ["role"] = new ToonString("user")
        };

        var array = new ToonArray
        {
            FieldNames = ["id", "name", "role"]
        };
        array.Items.Add(user1);
        array.Items.Add(user2);

        var root = new ToonObject
        {
            ["users"] = array
        };

        var doc = new ToonDocument(root);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("users[2]{id,name,role}:", result);
        Assert.Contains("  1,Alice,admin", result);
        Assert.Contains("  2,Bob,user", result);
    }

    [Fact]
    public void Encode_BooleanValues_ReturnsLowerCase()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["active"] = new ToonBoolean(true),
            ["deleted"] = new ToonBoolean(false)
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("active: true", result);
        Assert.Contains("deleted: false", result);
    }

    [Fact]
    public void Encode_NullValue_ReturnsNull()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["optional"] = ToonNull.Instance
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("optional: null", result);
    }

    [Fact]
    public void Encode_StringWithSpaces_QuotesString()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["name"] = new ToonString("Alice Smith")
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("name: \"Alice Smith\"", result);
    }
}