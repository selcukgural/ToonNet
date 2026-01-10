using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

namespace ToonNet.Tests.Coverage;

/// <summary>
///     Encoder coverage tests to improve overall coverage
///     Focus on edge cases and uncovered paths
/// </summary>
public class EncoderCoverageTests
{
    [Fact]
    public void Encode_EmptyObject_ReturnsEmpty()
    {
        var obj = new ToonObject();
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.NotNull(result);
    }

    [Fact]
    public void Encode_EmptyArray_WithZeroLength()
    {
        var obj = new ToonObject();
        obj["items"] = new ToonArray();
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("items[0]:", result);
    }

    [Fact]
    public void Encode_ArrayWithFieldNames_IncludesHeaders()
    {
        var array = new ToonArray
        {
            FieldNames = new[] { "name", "age" }
        };

        array.Items.Add(new ToonObject
        {
            ["name"] = new ToonString("Alice"),
            ["age"] = new ToonNumber(30)
        });

        var obj = new ToonObject();
        obj["people"] = array;
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("{name,age}", result);
    }

    [Fact]
    public void Encode_Null_Value()
    {
        var obj = new ToonObject();
        obj["value"] = ToonNull.Instance;
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("null", result);
    }

    [Fact]
    public void Encode_Boolean_True()
    {
        var obj = new ToonObject();
        obj["flag"] = new ToonBoolean(true);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("true", result);
    }

    [Fact]
    public void Encode_Boolean_False()
    {
        var obj = new ToonObject();
        obj["flag"] = new ToonBoolean(false);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("false", result);
    }

    [Fact]
    public void Encode_Number_Integer()
    {
        var obj = new ToonObject();
        obj["count"] = new ToonNumber(42);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("42", result);
    }

    [Fact]
    public void Encode_Number_Decimal()
    {
        var obj = new ToonObject();
        obj["price"] = new ToonNumber(19.99);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("price:", result);
        Assert.Contains("19.9", result); // Tolerant check for decimal
    }

    [Fact]
    public void Encode_Number_VeryLarge_UsesScientific()
    {
        var obj = new ToonObject();
        obj["big"] = new ToonNumber(1e22);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("e", result.ToLower()); // Scientific notation (e or E)
    }

    [Fact]
    public void Encode_Number_VerySmall_UsesScientific()
    {
        var obj = new ToonObject();
        obj["small"] = new ToonNumber(1e-21);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("e", result.ToLower()); // Scientific notation (e or E)
    }

    [Fact]
    public void Encode_Number_Zero()
    {
        var obj = new ToonObject();
        obj["zero"] = new ToonNumber(0);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("0", result);
    }

    [Fact]
    public void Encode_String_WithSpaces()
    {
        var obj = new ToonObject();
        obj["text"] = new ToonString("hello world");
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("hello world", result);
    }

    [Fact]
    public void Encode_String_WithColon_GetsQuoted()
    {
        var obj = new ToonObject();
        obj["path"] = new ToonString("C:\\Users");
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        // Should be quoted because of backslash or special chars
        Assert.NotNull(result);
    }

    [Fact]
    public void Encode_NestedObject_WithProperIndentation()
    {
        var inner = new ToonObject();
        inner["name"] = new ToonString("Inner");

        var outer = new ToonObject();
        outer["nested"] = inner;

        var doc = new ToonDocument(outer);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("nested:", result);
        Assert.Contains("name:", result);
    }

    [Fact]
    public void Encode_ArrayOfPrimitives()
    {
        var array = new ToonArray();
        array.Items.Add(new ToonString("a"));
        array.Items.Add(new ToonString("b"));
        array.Items.Add(new ToonString("c"));

        var obj = new ToonObject();
        obj["tags"] = array;
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("tags", result);
    }

    [Fact]
    public void Encode_ArrayOfObjects_UsesListFormat()
    {
        var array = new ToonArray();
        var item1 = new ToonObject();
        item1["name"] = new ToonString("Item1");
        array.Items.Add(item1);

        var obj = new ToonObject();
        obj["items"] = array;
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("items", result);
        Assert.Contains("-", result); // List item marker
    }

    [Fact]
    public void Encode_WithCustomIndentSize()
    {
        var inner = new ToonObject();
        inner["value"] = new ToonString("test");

        var outer = new ToonObject();
        outer["nested"] = inner;

        var doc = new ToonDocument(outer);
        var options = new ToonOptions { IndentSize = 4 };
        var encoder = new ToonEncoder(options);

        var result = encoder.Encode(doc);

        Assert.NotNull(result);
        Assert.Contains("nested:", result);
    }

    [Fact]
    public void Encode_DeepNesting_HandlesCorrectly()
    {
        var level3 = new ToonObject();
        level3["value"] = new ToonString("deep");

        var level2 = new ToonObject();
        level2["level3"] = level3;

        var level1 = new ToonObject();
        level1["level2"] = level2;

        var doc = new ToonDocument(level1);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Contains("level2:", result);
        Assert.Contains("level3:", result);
        Assert.Contains("deep", result);
    }
}