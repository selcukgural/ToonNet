using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Tests.Encoding;

public class ToonEncoderEdgeCaseTests
{
    [Fact]
    public void Encode_EmptyObject_ReturnsEmptyString()
    {
        // Arrange
        var obj = new ToonObject();
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Encode_EmptyArray_ReturnsEmptyArrayNotation()
    {
        // Arrange
        var array = new ToonArray();

        var root = new ToonObject
        {
            ["items"] = array
        };
        var doc = new ToonDocument(root);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("items[0]:", result);
    }

    [Fact]
    public void Encode_DeepNesting_ProperIndentation()
    {
        // Arrange
        var level3 = new ToonObject
        {
            ["value"] = new ToonString("deep")
        };

        var level2 = new ToonObject
        {
            ["level3"] = level3
        };

        var level1 = new ToonObject
        {
            ["level2"] = level2
        };

        var root = new ToonObject
        {
            ["level1"] = level1
        };

        var doc = new ToonDocument(root);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("level1:", result);
        Assert.Contains("  level2:", result);
        Assert.Contains("    level3:", result);
        Assert.Contains("      value: deep", result);
    }

    [Fact]
    public void Encode_StringWithEscapeCharacters_EscapesProperly()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["message"] = new ToonString("Hello\nWorld\t!")
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("\\n", result);
        Assert.Contains("\\t", result);
    }

    [Fact]
    public void Encode_NegativeNumbers_FormatsCorrectly()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["negative"] = new ToonNumber(-42.5),
            ["zero"] = new ToonNumber(0)
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("negative: -42.5", result);
        Assert.Contains("zero: 0", result);
    }

    [Fact]
    public void Encode_LargeNumbers_FormatsWithoutScientificNotation()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["large"] = new ToonNumber(1000000)
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("large: 1000000", result);
        Assert.DoesNotContain("e+", result.ToLower());
    }

    [Fact]
    public void Encode_EmptyString_QuotesEmpty()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["empty"] = new ToonString("")
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("empty: \"\"", result);
    }

    [Fact]
    public void Encode_StringLookingLikeNumber_Quotes()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["number"] = new ToonString("123")
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("number: \"123\"", result);
    }

    [Fact]
    public void Encode_StringLookingLikeBoolean_Quotes()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["bool"] = new ToonString("true")
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("bool: \"true\"", result);
    }

    [Fact]
    public void Encode_StringWithColon_Quotes()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["time"] = new ToonString("10:30")
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("time: \"10:30\"", result);
    }

    [Fact]
    public void Encode_StringWithComma_Quotes()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["list"] = new ToonString("a,b,c")
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("list: \"a,b,c\"", result);
    }

    [Fact]
    public void Encode_StringWithLeadingSpace_Quotes()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["spaced"] = new ToonString(" leading")
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("spaced: \" leading\"", result);
    }

    [Fact]
    public void Encode_StringWithTrailingSpace_Quotes()
    {
        // Arrange
        var obj = new ToonObject
        {
            ["spaced"] = new ToonString("trailing ")
        };
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("spaced: \"trailing \"", result);
    }

    [Fact]
    public void Encode_LargeTabularArray_FormatsAllRows()
    {
        // Arrange
        var array = new ToonArray { FieldNames = ["id", "name"] };

        for (var i = 1; i <= 100; i++)
        {
            var row = new ToonObject
            {
                ["id"] = new ToonNumber(i),
                ["name"] = new ToonString($"User{i}")
            };
            array.Items.Add(row);
        }

        var root = new ToonObject
        {
            ["users"] = array
        };
        var doc = new ToonDocument(root);
        var encoder = new ToonEncoder();

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("users[100]{id,name}:", result);
        Assert.Contains("100,User100", result);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(101, lines.Length); // header + 100 rows
    }

    [Fact]
    public void Encode_CustomIndentSize_UsesCorrectIndent()
    {
        // Arrange
        var nested = new ToonObject
        {
            ["value"] = new ToonString("test")
        };

        var root = new ToonObject
        {
            ["nested"] = nested
        };

        var doc = new ToonDocument(root);
        var encoder = new ToonEncoder(new ToonOptions { IndentSize = 4 });

        // Act
        var result = encoder.Encode(doc);

        // Assert
        Assert.Contains("    value: test", result); // 4 spaces
    }

    [Fact]
    public void Encode_MaxDepthExceeded_ThrowsException()
    {
        // Arrange
        var root = new ToonObject();
        var current = root;

        // Create 70 levels of nesting (exceeds default max of 64)
        for (var i = 0; i < 70; i++)
        {
            var next = new ToonObject();
            current[$"level{i}"] = next;
            current = next;
        }

        var doc = new ToonDocument(root);
        var encoder = new ToonEncoder();

        // Act & Assert
        Assert.Throws<ToonEncodingException>(() => encoder.Encode(doc));
    }

    [Fact]
    public void Encode_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new ToonObject
        {
            ["string"] = new ToonString("hello"),
            ["number"] = new ToonNumber(42),
            ["bool"] = new ToonBoolean(true),
            ["null"] = ToonNull.Instance
        };

        var array = new ToonArray();
        array.Items.Add(new ToonNumber(1));
        array.Items.Add(new ToonNumber(2));
        original["array"] = array;

        var doc = new ToonDocument(original);
        var encoder = new ToonEncoder();
        var parser = new ToonParser();

        // Act
        var encoded = encoder.Encode(doc);
        var decoded = parser.Parse(encoded);
        var result = decoded.AsObject();

        // Assert
        Assert.Equal("hello", ((ToonString)result["string"]!).Value);
        Assert.Equal(42.0, ((ToonNumber)result["number"]!).Value);
        Assert.True(((ToonBoolean)result["bool"]!).Value);
        Assert.IsType<ToonNull>(result["null"]);

        var resultArray = (ToonArray)result["array"]!;
        Assert.Equal(2, resultArray.Count);
    }
}