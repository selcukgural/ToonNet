using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     Verifies that serialized output uses the canonical TOON forms of spec v3.3.2.
/// </summary>
public class ToonSerializerOutputFormatTests
{
    private sealed class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public bool InStock { get; set; }
    }

    private sealed class Catalog
    {
        public List<Product> Items { get; set; } = [];
        public List<string> Tags { get; set; } = [];
        public List<int> Empty { get; set; } = [];
    }

    [Fact]
    public void Serialize_UniformObjectList_UsesTabularForm()
    {
        var catalog = new Catalog
        {
            Items =
            [
                new Product { Id = 1, Name = "Laptop", Price = 1299.99m, InStock = true },
                new Product { Id = 2, Name = "Mouse", Price = 0.1m, InStock = false }
            ],
            Tags = ["a", "b"]
        };

        var toon = ToonSerializer.Serialize(catalog);

        Assert.Equal("""
                     Items[2]{Id,Name,Price,InStock}:
                       1,Laptop,1299.99,true
                       2,Mouse,0.1,false
                     Tags[2]: a,b
                     Empty: []
                     """.ReplaceLineEndings("\n"), toon);
    }

    [Fact]
    public void Serialize_RootPrimitiveArray_WritesHeader()
    {
        Assert.Equal("[3]: 1,2,3", ToonSerializer.Serialize(new[] { 1, 2, 3 }));
    }

    [Fact]
    public void Serialize_NestedArrays_WritesInnerHeaders()
    {
        var toon = ToonSerializer.Serialize(new { M = new List<List<int>> { new() { 1, 2 }, new() { 3 } } });

        Assert.Equal("M[2]:\n  - [2]: 1,2\n  - [1]: 3", toon);
    }

    [Theory]
    [InlineData(0.1, "0.1")]
    [InlineData(1e21, "1e+21")]
    [InlineData(1e-7, "1e-7")]
    [InlineData(1000000.0, "1000000")]
    [InlineData(0.000001, "0.000001")]
    [InlineData(-0.0, "0")]
    [InlineData(1.5, "1.5")]
    public void Serialize_Double_UsesCanonicalShortestForm(double value, string expected)
    {
        Assert.Equal("V: " + expected, ToonSerializer.Serialize(new { V = value }));
    }

    [Fact]
    public void Serialize_NonFiniteDouble_WritesNull()
    {
        Assert.Equal("A: null\nB: null", ToonSerializer.Serialize(new { A = double.NaN, B = double.PositiveInfinity }));
    }

    [Fact]
    public void Serialize_LargeLongAndDecimal_ArePreservedExactly()
    {
        var toon = ToonSerializer.Serialize(new { L = 9007199254740993L, U = ulong.MaxValue, D = 12345678901234567890.123456789m, F = 0.1f });

        Assert.Equal("L: 9007199254740993\nU: 18446744073709551615\nD: 12345678901234567890.123456789\nF: 0.1", toon);
    }

    [Fact]
    public void Serialize_StringsThatLookLikeOtherTypes_AreQuoted()
    {
        var toon = ToonSerializer.Serialize(new { A = "(5)", B = "- x", C = "05", D = "true", E = "a\tb", F = "plain text" });

        Assert.Equal("A: (5)\nB: \"- x\"\nC: \"05\"\nD: \"true\"\nE: \"a\\tb\"\nF: plain text", toon);
    }

    [Fact]
    public void Serialize_KeysRequiringQuotes_AreQuotedInHeaders()
    {
        var value = new Dictionary<string, object> { ["my-key"] = new[] { 1, 2 }, ["ok_key.1"] = 1 };

        Assert.Equal("\"my-key\"[2]: 1,2\nok_key.1: 1", ToonSerializer.Serialize(value));
    }

    [Theory]
    [InlineData('\t', "Tags[2\t]: a\tb\nItems[1\t]{Id\tName}:\n  1\tx")]
    [InlineData('|', "Tags[2|]: a|b\nItems[1|]{Id|Name}:\n  1|x")]
    public void Serialize_WithTabOrPipeDelimiter_DeclaresItInHeaders(char delimiter, string expected)
    {
        var options = new ToonSerializerOptions { ToonOptions = new ToonOptions { Delimiter = delimiter } };

        var toon = ToonSerializer.Serialize(new { Tags = new[] { "a", "b" }, Items = new[] { new { Id = 1, Name = "x" } } }, options);

        Assert.Equal(expected, toon);
    }

    [Fact]
    public void Encode_OutputHasNoTrailingWhitespaceOrNewline()
    {
        var doc = new ToonDocument(new ToonObject
        {
            ["list"] = new ToonArray([new ToonObject { ["a"] = 1 }, new ToonObject { ["b"] = new ToonObject() }]),
            ["empty"] = new ToonObject()
        });

        var toon = new ToonEncoder().Encode(doc);

        Assert.Equal("list[2]:\n  - a: 1\n  - b:\nempty:", toon);
        Assert.DoesNotContain(" \n", toon);
        Assert.False(toon.EndsWith('\n'));
    }
}
