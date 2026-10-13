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
        array.Add(new ToonString("admin"));
        array.Add(new ToonString("user"));
        array.Add(new ToonString("guest"));

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
        array.Add(user1);
        array.Add(user2);

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
    public void Encode_StringWithInternalSpaces_IsNotQuoted()
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

        // Assert - internal spaces are safe unquoted (spec §7.2)
        Assert.Equal("name: Alice Smith", result);
    }

    [Fact]
    public async Task Encode_SharedInstanceAcrossThreads_ProducesSameOutputAsSequential()
    {
        // A singleton encoder (as registered by AddToonNet) must not mix the output of concurrent calls
        var encoder = new ToonEncoder();
        var documents = Enumerable.Range(0, 64).Select(i => new ToonDocument(new ToonObject
        {
            ["id"] = new ToonNumber(i),
            ["tags"] = Tags(i),
            ["nested"] = new ToonObject { ["name"] = new ToonString(new string('x', i)) }
        })).ToArray();
        var expected = documents.Select(encoder.Encode).ToArray();

        for (var round = 0; round < 20; round++)
        {
            var actual = await Task.WhenAll(documents.Select(doc => Task.Run(() => encoder.Encode(doc))));
            Assert.Equal(expected, actual);
        }

        static ToonArray Tags(int i)
        {
            var array = new ToonArray();
            array.Add(new ToonString("t" + i));
            array.Add(new ToonString("u" + i));
            return array;
        }
    }
}
