using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Tests.Coverage;

/// <summary>
///     Coverage improvement tests - Target 80%+
///     Focus on uncovered edge cases and error paths
/// </summary>
public class ParserCoverageTests
{
    [Fact]
    public void StrictMode_ArrayLengthMismatch_ThrowsException()
    {
        var options = new ToonOptions { StrictMode = true };
        var parser = new ToonParser(options);

        var ex = Assert.Throws<ToonParseException>(() => parser.Parse("items[3]: 1, 2"));
        Assert.Contains("length mismatch", ex.Message);
    }

    [Fact]
    public void NonStrictMode_ArrayLengthMismatch_Succeeds()
    {
        var options = new ToonOptions { StrictMode = false };
        var parser = new ToonParser(options);

        var doc = parser.Parse("items[3]: 1, 2");
        var obj = doc.AsObject();
        var items = (ToonArray)obj["items"];
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void TabularArray_FieldCountMismatch_ThrowsException()
    {
        var parser = new ToonParser();
        var input = "people[1]{name,age,city}:\n  Alice, 30";

        var ex = Assert.Throws<ToonParseException>(() => parser.Parse(input));
        Assert.Contains("expected 3", ex.Message);
    }

    [Fact]
    public void TabularArray_WrongIndentation_ThrowsException()
    {
        var parser = new ToonParser();
        var input = "people[2]{name,age}:\n  Alice, 30\n    Bob, 25";

        Assert.Throws<ToonParseException>(() => parser.Parse(input));
    }

    [Fact]
    public void ListItem_ScalarValues()
    {
        var input = "tags[3]:\n  - tag1\n  - tag2\n  - tag3";
        var doc = new ToonParser().Parse(input);
        var obj = doc.AsObject();
        var tags = (ToonArray)obj["tags"];

        Assert.Equal(3, tags.Count);
        Assert.Equal("tag1", ((ToonString)tags[0]).Value);
    }

    [Fact]
    public void ListItem_WithProperties()
    {
        var input = "items[2]:\n  - key: value1\n  - key: value2";
        var doc = new ToonParser().Parse(input);
        var obj = doc.AsObject();
        var items = (ToonArray)obj["items"];

        Assert.Equal(2, items.Items.Count);
        var item1 = (ToonObject)items[0];
        Assert.Equal("value1", ((ToonString)item1["key"]).Value);
    }

    [Fact]
    public void ListItems_WithoutHeader_StrictMode_ThrowsHelpfulError()
    {
        var ex = Assert.Throws<ToonParseException>(() => new ToonParser().Parse("tags:\n  - a\n  - b"));

        Assert.Contains("require an array header such as 'tags[N]:'", ex.Message);
    }

    [Fact]
    public void ListItems_WithoutHeader_NonStrictMode_ParsesArray()
    {
        var doc = new ToonParser(new ToonOptions { StrictMode = false }).Parse("tags:\n  - a\n  - b: 1\n    c: 2");
        var tags = (ToonArray)doc.AsObject()["tags"]!;

        Assert.Equal(2, tags.Count);
        Assert.Equal("a", ((ToonString)tags[0]).Value);
        Assert.Equal(2, ((ToonObject)tags[1]).Properties.Count);
    }

    [Fact]
    public void NumberOverflowingDouble_DecodesAsString()
    {
        var obj = new ToonParser().Parse("a: 1e400\nb: -0\nc: 12345678901234567890.123456789").AsObject();

        Assert.Equal("1e400", ((ToonString)obj["a"]!).Value);
        Assert.Equal("0", obj["b"]!.ToString());
        Assert.Equal(12345678901234567890.123456789m, ((ToonNumber)obj["c"]!).DecimalValue);
    }

    [Fact]
    public void TabIndentation_NonStrict_CountsAsIndentSize()
    {
        var obj = new ToonParser(new ToonOptions { StrictMode = false, IndentSize = 4 }).Parse("a:\n\tb: 1").AsObject();

        Assert.Equal(1, ((ToonNumber)((ToonObject)obj["a"]!)["b"]!).Value);
    }

    [Fact]
    public void TabularRows_NonStrict_PadMissingCellsWithNull()
    {
        var obj = new ToonParser(new ToonOptions { StrictMode = false }).Parse("t[1]{a,b}:\n  1").AsObject();

        Assert.IsType<ToonNull>(((ToonObject)((ToonArray)obj["t"]!)[0])["b"]);
    }

    [Fact]
    public void CrLfLineEndings_AreAccepted()
    {
        var obj = new ToonParser().Parse("a: 1\r\nb[2]: x,y\r\n").AsObject();

        Assert.Equal(2, ((ToonArray)obj["b"]!).Count);
    }

    [Fact]
    public void SimpleKeyValue_Works()
    {
        var parser = new ToonParser();
        var doc = parser.Parse("key: value");
        var obj = doc.AsObject();
        Assert.Equal("value", ((ToonString)obj["key"]).Value);
    }

    [Fact]
    public void MultipleProperties_AtSameLevel()
    {
        var parser = new ToonParser();
        var input = "key1: value1\nkey2: value2\nkey3: value3";
        var doc = parser.Parse(input);
        var obj = doc.AsObject();
        Assert.Equal(3, obj.Properties.Count);
    }

    [Fact]
    public void DeepNesting_Works()
    {
        var input = "a:\n  b:\n    c:\n      d:\n        e: value";
        var doc = new ToonParser().Parse(input);
        var obj = doc.AsObject();

        var a = (ToonObject)obj["a"];
        var b = (ToonObject)a["b"];
        var c = (ToonObject)b["c"];
        var d = (ToonObject)c["d"];
        Assert.Equal("value", ((ToonString)d["e"]).Value);
    }

    [Fact]
    public void AllPrimitiveTypes_Work()
    {
        var input = @"
str: hello
num: 42
dec: 3.14
bool_t: true
bool_f: false
nul: null";

        var doc = new ToonParser().Parse(input);
        var obj = doc.AsObject();

        Assert.IsType<ToonString>(obj["str"]);
        Assert.IsType<ToonNumber>(obj["num"]);
        Assert.IsType<ToonNumber>(obj["dec"]);
        Assert.IsType<ToonBoolean>(obj["bool_t"]);
        Assert.IsType<ToonBoolean>(obj["bool_f"]);
        Assert.IsType<ToonNull>(obj["nul"]);
    }

    [Fact]
    public void QuotedStrings_PreserveEscapes()
    {
        var input = @"path: ""C:\\Users""";
        var doc = new ToonParser().Parse(input);
        var obj = doc.AsObject();

        var path = ((ToonString)obj["path"]).Value;
        Assert.Contains("\\", path);
    }

    [Fact]
    public void EmptyInput_ReturnsEmptyObject()
    {
        var doc = new ToonParser().Parse("");
        var obj = doc.AsObject();
        Assert.Empty(obj.Properties);
    }

    [Fact]
    public void WhitespaceOnly_ReturnsEmptyObject()
    {
        var doc = new ToonParser().Parse("   \n  \n   ");
        var obj = doc.AsObject();
        Assert.Empty(obj.Properties);
    }

    [Fact]
    public void InlineArray_Works()
    {
        var input = "tags[3]: a, b, c";
        var doc = new ToonParser().Parse(input);
        var obj = doc.AsObject();
        var tags = (ToonArray)obj["tags"];
        Assert.Equal(3, tags.Count);
    }

    [Fact]
    public void TabularArray_StrictMode_LengthMismatch()
    {
        var options = new ToonOptions { StrictMode = true };
        var parser = new ToonParser(options);
        var input = "items[3]:\n  - a\n  - b";

        var ex = Assert.Throws<ToonParseException>(() => parser.Parse(input));
        Assert.Contains("length mismatch", ex.Message);
    }

    [Fact]
    public void TabularArray_NonStrictMode_LengthMismatch()
    {
        var options = new ToonOptions { StrictMode = false };
        var parser = new ToonParser(options);
        var input = "items[3]:\n  - a\n  - b";

        var doc = parser.Parse(input);
        var obj = doc.AsObject();
        var items = (ToonArray)obj["items"];
        Assert.Equal(2, items.Count);
    }
}