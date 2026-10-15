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

        Assert.Equal("", result);
    }

    [Fact]
    public void Encode_EmptyArray_WithZeroLength()
    {
        var obj = new ToonObject();
        obj["items"] = new ToonArray();
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("items: []", result);
    }

    [Fact]
    public void Encode_ArrayWithFieldNames_IncludesHeaders()
    {
        var array = new ToonArray
        {
            FieldNames = new[] { "name", "age" }
        };

        array.Add(new ToonObject
        {
            ["name"] = new ToonString("Alice"),
            ["age"] = new ToonNumber(30)
        });

        var obj = new ToonObject();
        obj["people"] = array;
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("people[1]{name,age}:\n  Alice,30", result);
    }

    [Fact]
    public void Encode_Null_Value()
    {
        var obj = new ToonObject();
        obj["value"] = ToonNull.Instance;
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("value: null", result);
    }

    [Fact]
    public void Encode_Boolean_True()
    {
        var obj = new ToonObject();
        obj["flag"] = new ToonBoolean(true);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("flag: true", result);
    }

    [Fact]
    public void Encode_Boolean_False()
    {
        var obj = new ToonObject();
        obj["flag"] = new ToonBoolean(false);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("flag: false", result);
    }

    [Fact]
    public void Encode_Number_Integer()
    {
        var obj = new ToonObject();
        obj["count"] = new ToonNumber(42);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("count: 42", result);
    }

    [Fact]
    public void Encode_Number_Decimal()
    {
        var obj = new ToonObject();
        obj["price"] = new ToonNumber(19.99);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("price: 19.99", result);
    }

    [Fact]
    public void Encode_Number_VeryLarge_UsesScientific()
    {
        var obj = new ToonObject();
        obj["big"] = new ToonNumber(1e22);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("big: 1e+22", result);
    }

    [Fact]
    public void Encode_Number_VerySmall_UsesScientific()
    {
        var obj = new ToonObject();
        obj["small"] = new ToonNumber(1e-21);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("small: 1e-21", result);
    }

    [Fact]
    public void Encode_Number_Zero()
    {
        var obj = new ToonObject();
        obj["zero"] = new ToonNumber(0);
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("zero: 0", result);
    }

    [Fact]
    public void Encode_String_WithSpaces()
    {
        var obj = new ToonObject();
        obj["text"] = new ToonString("hello world");
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("text: hello world", result);
    }

    [Fact]
    public void Encode_String_WithColon_GetsQuoted()
    {
        var obj = new ToonObject();
        obj["path"] = new ToonString("C:\\Users");
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("path: \"C:\\\\Users\"", result);
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

        Assert.Equal("nested:\n  name: Inner", result);
    }

    [Fact]
    public void Encode_ArrayOfPrimitives()
    {
        var array = new ToonArray();
        array.Add(new ToonString("a"));
        array.Add(new ToonString("b"));
        array.Add(new ToonString("c"));

        var obj = new ToonObject();
        obj["tags"] = array;
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("tags[3]: a,b,c", result);
    }

    [Fact]
    public void Encode_UniformArrayOfObjects_UsesTabularFormat()
    {
        var array = new ToonArray();
        var item1 = new ToonObject();
        item1["name"] = new ToonString("Item1");
        array.Add(item1);

        var obj = new ToonObject();
        obj["items"] = array;
        var doc = new ToonDocument(obj);
        var encoder = new ToonEncoder();

        var result = encoder.Encode(doc);

        Assert.Equal("items[1]{name}:\n  Item1", result);
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

        Assert.Equal("nested:\n    value: test", result);
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

        Assert.Equal("level2:\n  level3:\n    value: deep", result);
    }
}