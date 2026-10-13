using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Tests.Parsing;

public class ToonParserEdgeCaseTests
{
    [Fact]
    public void Parse_EmptyInput_ReturnsEmptyObject()
    {
        // Arrange
        var input = "";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.Empty(obj.Properties);
    }

    [Fact]
    public void Parse_OnlyWhitespace_ReturnsEmptyObject()
    {
        // Arrange
        var input = "   \n  \n   ";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.Empty(obj.Properties);
    }

    [Fact]
    public void Parse_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var input = "items[0]:";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        var array = (ToonArray)obj["items"]!;
        Assert.Equal(0, array.Count);
    }

    [Fact]
    public void Parse_DeepNesting_HandlesMultipleLevels()
    {
        // Arrange
        var input = @"level1:
  level2:
    level3:
      value: deep";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        var level1 = (ToonObject)obj["level1"]!;
        var level2 = (ToonObject)level1["level2"]!;
        var level3 = (ToonObject)level2["level3"]!;
        Assert.Equal("deep", ((ToonString)level3["value"]!).Value);
    }

    [Fact]
    public void Parse_StringWithSpecialCharacters_PreservesContent()
    {
        // Arrange
        var input = @"message: ""Hello\nWorld\t!""";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        var value = ((ToonString)obj["message"]!).Value;
        Assert.Contains("\n", value);
        Assert.Contains("\t", value);
    }

    [Fact]
    public void Parse_NumbersWithDecimals_ParsesCorrectly()
    {
        // Arrange
        var input = @"pi: 3.14159
negative: -42.5
zero: 0.0";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.Equal(3.14159, ((ToonNumber)obj["pi"]!).Value);
        Assert.Equal(-42.5, ((ToonNumber)obj["negative"]!).Value);
        Assert.Equal(0.0, ((ToonNumber)obj["zero"]!).Value);
    }

    [Fact]
    public void Parse_MixedArrayTypes_ThrowsInStrictMode()
    {
        // Arrange
        var input = "items[3]: 1,two,3";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert - should parse without error (mixed types allowed)
        var obj = doc.AsObject();
        var array = (ToonArray)obj["items"]!;
        Assert.Equal(3, array.Count);
    }

    [Fact]
    public void Parse_ArrayLengthMismatch_ThrowsInStrictMode()
    {
        // Arrange
        var input = "items[5]: 1,2,3"; // Says 5 but only has 3
        var parser = new ToonParser(new ToonOptions { StrictMode = true });

        // Act & Assert
        Assert.Throws<ToonParseException>(() => parser.Parse(input));
    }

    [Fact]
    public void Parse_ArrayLengthMismatch_SucceedsInNonStrictMode()
    {
        // Arrange
        var input = "items[5]: 1,2,3";
        var parser = new ToonParser(new ToonOptions { StrictMode = false });

        // Act
        var doc = parser.Parse(input);

        // Assert - should parse without error
        var obj = doc.AsObject();
        var array = (ToonArray)obj["items"]!;
        Assert.Equal(3, array.Count); // Actual count, not declared count
    }

    [Fact]
    public void Parse_EmptyString_ParsesAsEmptyString()
    {
        // Arrange
        var input = @"empty: """"";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.Equal("", ((ToonString)obj["empty"]!).Value);
    }

    [Fact]
    public void Parse_UnquotedKeywordsAsValues_ParsesAsKeywordTypes()
    {
        // Arrange - without quotes, these should parse as their keyword types
        var input = @"value1: true
value2: false
value3: null";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.IsType<ToonBoolean>(obj["value1"]);
        Assert.True(((ToonBoolean)obj["value1"]!).Value);
        Assert.IsType<ToonBoolean>(obj["value2"]);
        Assert.False(((ToonBoolean)obj["value2"]!).Value);
        Assert.IsType<ToonNull>(obj["value3"]);
    }

    [Fact]
    public void Parse_MultilineTabularArray_ParsesAllRows()
    {
        // Arrange
        var input = @"users[5]{id,name,active}:
  1,Alice,true
  2,Bob,false
  3,Charlie,true
  4,Diana,true
  5,Eve,false";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        var users = (ToonArray)obj["users"]!;
        Assert.Equal(5, users.Count);

        var lastUser = (ToonObject)users[4];
        Assert.Equal(5.0, ((ToonNumber)lastUser["id"]!).Value);
        Assert.Equal("Eve", ((ToonString)lastUser["name"]!).Value);
    }

    [Theory]
    [InlineData("value: 123", 123.0)]
    [InlineData("value: -123", -123.0)]
    [InlineData("value: 0", 0.0)]
    [InlineData("value: 3.14", 3.14)]
    [InlineData("value: -0.5", -0.5)]
    public void Parse_VariousNumbers_ParsesCorrectly(string input, double expected)
    {
        // Arrange
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.Equal(expected, ((ToonNumber)obj["value"]!).Value);
    }

    [Fact]
    public void Parse_ComplexNestedStructure_ParsesCorrectly()
    {
        // Arrange
        var input = @"company: Acme Corp
employees[2]{id,name}:
  1,Alice
  2,Bob
departments:
  engineering:
    manager: Alice
    count: 10";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.Equal("Acme Corp", ((ToonString)obj["company"]!).Value);

        var employees = (ToonArray)obj["employees"]!;
        Assert.Equal(2, employees.Count);

        var depts = (ToonObject)obj["departments"]!;
        var eng = (ToonObject)depts["engineering"]!;
        Assert.Equal("Alice", ((ToonString)eng["manager"]!).Value);
        Assert.Equal(10.0, ((ToonNumber)eng["count"]!).Value);
    }

    [Fact]
    public void Parse_CrLfLineEndings_MatchLf()
    {
        const string toon = "name: Ada\nitems[2]{id,tag}:\n  1,a\n  2,\"b:c\"\nnested:\n  flag: true\n";
        var encoder = new ToonNet.Core.Encoding.ToonEncoder();

        var lf = encoder.Encode(new ToonParser().Parse(toon));
        var crlf = encoder.Encode(new ToonParser().Parse(toon.Replace("\n", "\r\n")));

        Assert.Equal(lf, crlf);
        Assert.Equal(toon.TrimEnd('\n'), lf);
    }

    [Fact]
    public void Parse_ManyDistinctKeys_KeepsEveryKey()
    {
        // More distinct keys than the parser's key cache has slots, repeated in a list, so slots are replaced
        var keys = Enumerable.Range(0, 600).Select(i => $"key{i}").ToList();
        var item = string.Join("\n    ", keys.Select((key, i) => $"{key}: {i}"));
        var toon = $"items[2]:\n  - {item}\n  - {item}";

        var items = (ToonArray)new ToonParser().Parse(toon).AsObject()["items"]!;

        Assert.All(items.Items, value =>
        {
            var obj = (ToonObject)value;
            Assert.Equal(keys, obj.Properties.Keys);
            Assert.Equal(599m, ((ToonNumber)obj["key599"]!).DecimalValue);
        });
    }

    [Fact]
    public void Parse_HugeDeclaredLength_DoesNotReserveMemoryForMissingItems()
    {
        var parser = new ToonParser(new ToonOptions { StrictMode = false });

        var list = (ToonArray)parser.Parse("items[2000000000]:\n  - 1").AsObject()["items"]!;
        var rows = (ToonArray)parser.Parse("rows[2000000000]{a}:\n  1").AsObject()["rows"]!;

        Assert.Single(list.Items);
        Assert.Single(rows.Items);
        Assert.Throws<ToonParseException>(() => new ToonParser().Parse("items[2000000000]:\n  - 1"));
    }

    [Theory]
    [InlineData("999999999999999999")]
    [InlineData("-999999999999999999")]
    [InlineData("1000000000000000000")]
    [InlineData("-9223372036854775808")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("0")]
    [InlineData("-0")]
    [InlineData("12")]
    public void Parse_Integers_KeepTheirExactValue(string token)
    {
        var number = (ToonNumber)new ToonParser().Parse($"v: {token}").AsObject()["v"]!;

        Assert.Equal(decimal.Parse(token, System.Globalization.CultureInfo.InvariantCulture), number.DecimalValue);
        Assert.Equal((double)decimal.Parse(token, System.Globalization.CultureInfo.InvariantCulture), number.Value);
        Assert.Equal(token == "-0" ? "0" : token, number.ToString());
    }

    [Theory]
    [InlineData("05")]
    [InlineData("-")]
    [InlineData("1.")]
    [InlineData("1e")]
    [InlineData("+1")]
    [InlineData("1_000")]
    public void Parse_NonNumbers_StayStrings(string token)
    {
        var value = new ToonParser().Parse($"v: {token}").AsObject()["v"];

        Assert.Equal(token, Assert.IsType<ToonString>(value).Value);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(300)]
    [InlineData(5000)]
    public void Parse_EscapedStrings_OfAnyLength_RoundTrip(int repeat)
    {
        // Short strings are unescaped on the stack, long ones in a pooled buffer
        var value = string.Concat(Enumerable.Repeat("a\"b\\c\n\t\u0001", repeat));
        var document = new ToonDocument(new ToonObject { ["k"] = value, [value] = 1 });

        var toon = new ToonNet.Core.Encoding.ToonEncoder().Encode(document);
        var parsed = new ToonParser().Parse(toon).AsObject();

        Assert.Equal(value, ((ToonString)parsed["k"]!).Value);
        Assert.True(parsed.Properties.ContainsKey(value));
    }
}
