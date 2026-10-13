using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Tests.Encoding;

public class ToonEncoderEdgeCaseTests
{
    [Theory]
    [InlineData("a", 0xD800, "b")]
    [InlineData("a", 0xDC00, "")]
    [InlineData("x:", 0xD800, "")]
    [InlineData("", 0xD83D, "")]
    public void Encode_UnpairedSurrogate_Throws(string before, int surrogate, string after)
    {
        // Built here: attribute arguments cannot hold a lone surrogate
        var text = before + (char)surrogate + after;
        // A lone surrogate is not a Unicode scalar value, so TOON cannot represent it (spec 4.4 §3)
        var document = new ToonDocument(new ToonObject { ["k"] = new ToonString(text) });

        Assert.Throws<ToonEncodingException>(() => new ToonEncoder().Encode(document));
        Assert.Throws<ToonEncodingException>(() => ToonNet.Core.Serialization.ToonSerializer.Serialize(new Dictionary<string, string> { ["k"] = text }));
    }

    [Theory]
    [InlineData("rocket \ud83d\ude80", "k: rocket \ud83d\ude80")]
    [InlineData("a,\ud83d\ude80", "k: \"a,\ud83d\ude80\"")]
    [InlineData("#tag", "k: \"#tag\"")]
    [InlineData("+1", "k: \"+1\"")]
    [InlineData("\u00a0x\u00a0", "k: \u00a0x\u00a0")]
    [InlineData("\ufeffx", "k: \ufeffx")]
    public void Encode_QuotesStringsPerSpec44(string text, string expected)
    {
        var document = new ToonDocument(new ToonObject { ["k"] = new ToonString(text) });

        Assert.Equal(expected, new ToonEncoder().Encode(document));
    }

    [Fact]
    public void Encode_RootStringStartingWithByteOrderMark_IsQuoted()
    {
        Assert.Equal("\"\ufeffx\"", new ToonEncoder().Encode(new ToonDocument(new ToonString("\ufeffx"))));
        Assert.Equal("\"\ufeffx\"", ToonNet.Core.Serialization.ToonSerializer.Serialize("\ufeffx"));
    }

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

        // Assert - canonical empty array form (spec §9.1)
        Assert.Equal("items: []", result);
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
            array.Add(row);
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

        // Create 110 levels of nesting (exceeds default max of 100)
        for (var i = 0; i < 110; i++)
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
        array.Add(new ToonNumber(1));
        array.Add(new ToonNumber(2));
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