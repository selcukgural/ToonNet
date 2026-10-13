using ToonNet.Core.Models;

namespace ToonNet.Tests.Models;

public class ToonValueTests
{
    [Fact]
    public void ToonNull_IsSingleton()
    {
        Assert.Same(ToonNull.Instance, ToonNull.Instance);
    }

    [Fact]
    public void ToonNull_HasCorrectValueType()
    {
        Assert.Equal(ToonValueType.Null, ToonNull.Instance.ValueType);
    }

    [Fact]
    public void ToonNull_ToString_ReturnsNull()
    {
        Assert.Equal("null", ToonNull.Instance.ToString());
    }

    [Fact]
    public void ToonBoolean_True_HasCorrectValue()
    {
        var v = new ToonBoolean(true);
        Assert.True(v.Value);
        Assert.Equal(ToonValueType.Boolean, v.ValueType);
    }

    [Fact]
    public void ToonBoolean_False_HasCorrectValue()
    {
        var v = new ToonBoolean(false);
        Assert.False(v.Value);
    }

    [Fact]
    public void ToonBoolean_ToString_ReturnsLowercase()
    {
        Assert.Equal("true", new ToonBoolean(true).ToString());
        Assert.Equal("false", new ToonBoolean(false).ToString());
    }

    [Fact]
    public void ToonNumber_Integer_StoresCorrectly()
    {
        var v = new ToonNumber(42);
        Assert.Equal(42, v.Value);
        Assert.Equal(ToonValueType.Number, v.ValueType);
    }

    [Fact]
    public void ToonNumber_Decimal_StoresCorrectly()
    {
        Assert.Equal(3.14159, new ToonNumber(3.14159).Value);
    }

    [Fact]
    public void ToonNumber_Negative_StoresCorrectly()
    {
        Assert.Equal(-123.45, new ToonNumber(-123.45).Value);
    }

    [Fact]
    public void ToonNumber_Zero_StoresCorrectly()
    {
        Assert.Equal(0, new ToonNumber(0).Value);
    }

    [Fact]
    public void ToonNumber_ToString_UsesInvariantCulture()
    {
        Assert.Equal("1234.56", new ToonNumber(1234.56).ToString());
    }

    [Fact]
    public void ToonString_EmptyString_IsValid()
    {
        var v = new ToonString("");
        Assert.Equal("", v.Value);
        Assert.Equal(ToonValueType.String, v.ValueType);
    }

    [Fact]
    public void ToonString_SimpleText_StoresCorrectly()
    {
        Assert.Equal("Hello, World!", new ToonString("Hello, World!").Value);
    }

    [Fact]
    public void ToonString_WithSpecialCharacters_StoresCorrectly()
    {
        var v = new ToonString("Line1\nLine2\tTab");
        Assert.Contains("\n", v.Value);
    }

    [Fact]
    public void ToonString_WithUnicode_StoresCorrectly()
    {
        Assert.Equal("Hello 世界 🌍", new ToonString("Hello 世界 🌍").Value);
    }

    [Fact]
    public void ToonString_ToString_ReturnsValue()
    {
        Assert.Equal("Test", new ToonString("Test").ToString());
    }

    [Fact]
    public void ToonObject_Empty_IsValid()
    {
        var obj = new ToonObject();
        Assert.Empty(obj.Properties);
        Assert.Equal(ToonValueType.Object, obj.ValueType);
    }

    [Fact]
    public void ToonObject_WithDictionary_StoresProperties()
    {
        var obj = new ToonObject(new Dictionary<string, ToonValue> { ["k1"] = new ToonString("v1") });
        Assert.Single(obj.Properties);
    }

    [Fact]
    public void ToonObject_Indexer_Get_ReturnsValue()
    {
        var obj = new ToonObject();
        obj.Properties["test"] = new ToonString("value");
        Assert.NotNull(obj["test"]);
    }

    [Fact]
    public void ToonObject_Indexer_Get_NonExistent_ReturnsNull()
    {
        Assert.Null(new ToonObject()["nonexistent"]);
    }

    [Fact]
    public void ToonObject_Indexer_Set_AddsValue()
    {
        var obj = new ToonObject();
        obj["key"] = new ToonNumber(123);
        Assert.Single(obj.Properties);
    }

    [Fact]
    public void ToonObject_Indexer_Set_UpdatesExisting()
    {
        var obj = new ToonObject();
        obj["k"] = new ToonString("old");
        obj["k"] = new ToonString("new");
        Assert.Single(obj.Properties);
        Assert.Equal("new", ((ToonString)obj["k"]!).Value);
    }

    [Fact]
    public void ToonObject_Indexer_Set_Null_StoresToonNull()
    {
        var obj = new ToonObject();
        obj["k"] = null;
        Assert.Same(ToonNull.Instance, obj.Properties["k"]);
        Assert.Equal("k: null", new ToonNet.Core.Encoding.ToonEncoder().Encode(new ToonDocument(obj)));
    }

    [Fact]
    public void ToonArray_NullItems_AreToonNull()
    {
        var arr = new ToonArray();
        arr.Add(null);
        arr.Add(new ToonString("x"));
        arr[1] = null!;

        Assert.All(arr.Items, item => Assert.Same(ToonNull.Instance, item));
    }

    [Fact]
    public void ToonObject_KeyAddedAfterRemoval_IsWrittenLast()
    {
        var obj = new ToonObject { ["a"] = 1, ["b"] = 2, ["c"] = 3 };

        obj.Properties.Remove("b");
        obj["d"] = 4;
        obj["a"] = 5; // replacing keeps the position

        Assert.Equal(["a", "c", "d"], obj.Properties.Keys);
        Assert.Equal("a: 5\nc: 3\nd: 4", new ToonNet.Core.Encoding.ToonEncoder().Encode(new ToonDocument(obj)));
    }

    [Fact]
    public void ToonPropertyCollection_BehavesLikeADictionary()
    {
        var properties = new ToonPropertyCollection { { "a", new ToonNumber(1) } };

        Assert.False(properties.TryAdd("a", new ToonNumber(2)));
        Assert.Throws<ArgumentException>(() => properties.Add("a", new ToonNumber(2)));
        Assert.Throws<KeyNotFoundException>(() => properties["missing"]);
        Assert.True(properties.TryGetValue("a", out var value));
        Assert.Equal("1", value.ToString());
        Assert.Equal(ToonNull.Instance, new ToonObject().Properties.GetValueOrDefault("x") ?? ToonNull.Instance);
        Assert.Throws<ArgumentException>(() => new ToonObject(new[] { KeyValuePair.Create("k", (ToonValue)1), KeyValuePair.Create("k", (ToonValue)2) }));

        IDictionary<string, ToonValue> asDictionary = properties;
        asDictionary.Add(new KeyValuePair<string, ToonValue>("b", new ToonNumber(2)));
        Assert.Equal(["a", "b"], asDictionary.Keys);
        Assert.True(properties.Remove("a"));
        Assert.Equal(0, properties.Keys.ToList().IndexOf("b"));
        Assert.True(properties.ContainsKey("b"));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(40)]
    public void ToonPropertyCollection_KeepsOrderAndLookupsAtAnySize(int count)
    {
        // Small collections search linearly, larger ones keep a hash index; both must behave the same
        var properties = new ToonPropertyCollection();
        var keys = Enumerable.Range(0, count).Select(i => $"k{i}").ToList();

        foreach (var key in keys)
        {
            properties.Add(key, new ToonString(key));
        }

        Assert.Equal(keys, properties.Keys);
        Assert.All(keys, key => Assert.Equal(key, ((ToonString)properties[key]).Value));
        Assert.False(properties.ContainsKey("missing"));

        // Remove from the middle, re-add at the end, replace keeps the position
        var middle = keys[count / 2];
        Assert.True(properties.Remove(middle, out var removed));
        Assert.Equal(middle, ((ToonString)removed).Value);
        Assert.False(properties.ContainsKey(middle));
        properties[middle] = new ToonString("again");
        properties[keys[0]] = new ToonString("first");

        var expected = keys.Where(k => k != middle).Append(middle).ToList();
        Assert.Equal(expected, properties.Keys);
        Assert.All(expected.Skip(1).SkipLast(1), key => Assert.Equal(key, ((ToonString)properties[key]).Value));
        Assert.Equal("again", ((ToonString)properties[middle]).Value);
        Assert.Equal("first", ((ToonString)properties[keys[0]]).Value);
        Assert.Equal(expected.Count, properties.Count);

        var copy = new KeyValuePair<string, ToonValue>[count + 1];
        ((ICollection<KeyValuePair<string, ToonValue>>)properties).CopyTo(copy, 1);
        Assert.Equal(expected, copy.Skip(1).Select(entry => entry.Key));

        properties.Clear();
        Assert.Empty(properties);
        Assert.False(properties.ContainsKey(keys[0]));
        properties.Add(keys[0], ToonNull.Instance);
        Assert.Single(properties);
    }

    [Fact]
    public void ToonPropertyCollection_ModifiedDuringEnumeration_Throws()
    {
        var properties = new ToonPropertyCollection { { "a", ToonNull.Instance }, { "b", ToonNull.Instance } };

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var (key, _) in properties)
            {
                properties[key + "x"] = ToonNull.Instance;
            }
        });
        Assert.Throws<ArgumentNullException>(() => properties.ContainsKey(null!));
        Assert.Throws<ArgumentNullException>(() => properties["c"] = null!);
    }

    [Fact]
    public void ToonArray_Empty_IsValid()
    {
        var arr = new ToonArray();
        Assert.Empty(arr.Items);
        Assert.Equal(0, arr.Count);
        Assert.Equal(ToonValueType.Array, arr.ValueType);
        Assert.False(arr.IsTabular);
    }

    [Fact]
    public void ToonArray_WithItems_StoresCorrectly()
    {
        var arr = new ToonArray(new List<ToonValue> { new ToonNumber(1), new ToonNumber(2) });
        Assert.Equal(2, arr.Count);
    }

    [Fact]
    public void ToonArray_Indexer_Get_ReturnsItem()
    {
        var arr = new ToonArray();
        arr.Add(new ToonString("first"));
        Assert.Equal("first", ((ToonString)arr[0]).Value);
    }

    [Fact]
    public void ToonArray_Indexer_Set_UpdatesItem()
    {
        var arr = new ToonArray();
        arr.Add(new ToonString("old"));
        arr[0] = new ToonString("new");
        Assert.Equal("new", ((ToonString)arr[0]).Value);
    }

    [Fact]
    public void ToonArray_WithFieldNames_IsTabular()
    {
        var arr = new ToonArray { FieldNames = new[] { "id", "name" } };
        Assert.True(arr.IsTabular);
        Assert.Equal(2, arr.FieldNames!.Length);
    }

    [Fact]
    public void ToonArray_WithoutFieldNames_IsNotTabular()
    {
        Assert.False(new ToonArray().IsTabular);
        Assert.Null(new ToonArray().FieldNames);
    }

    [Fact]
    public void ToonArray_WithEmptyFieldNames_IsNotTabular()
    {
        Assert.False(new ToonArray { FieldNames = Array.Empty<string>() }.IsTabular);
    }

    [Fact]
    public void ToonArray_MixedTypes_IsValid()
    {
        var arr = new ToonArray();
        arr.Add(new ToonNumber(42));
        arr.Add(new ToonString("text"));
        arr.Add(new ToonBoolean(true));
        Assert.Equal(3, arr.Count);
    }

    [Fact]
    public void ToonValueType_AllTypesAreDefined()
    {
        Assert.True(Enum.IsDefined(typeof(ToonValueType), ToonValueType.Null));
        Assert.True(Enum.IsDefined(typeof(ToonValueType), ToonValueType.Boolean));
    }

    [Fact]
    public void ComplexStructure_WithAllTypes_WorksTogether()
    {
        var root = new ToonObject();
        root["null"] = ToonNull.Instance;
        root["boolean"] = new ToonBoolean(true);
        root["number"] = new ToonNumber(42.5);
        root["string"] = new ToonString("Hello");
        Assert.Equal(4, root.Properties.Count);
    }

    [Theory]
    [InlineData("42", "42")]
    [InlineData("-0.50", "-0.5")]
    [InlineData("1e3", "1000")]
    [InlineData("12345678901234567890", "12345678901234567890")]
    public void ToonNumber_TryParse_AcceptsTheTokenGrammar(string text, string canonical)
    {
        Assert.True(ToonNumber.TryParse(text, out var number));
        Assert.Equal(canonical, number.ToString());
        Assert.True(number.IsFinite);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("05")]
    [InlineData("+1")]
    [InlineData("1.")]
    [InlineData("NaN")]
    [InlineData("1e400")]
    public void ToonNumber_TryParse_RejectsOtherText(string? text)
    {
        Assert.False(ToonNumber.TryParse(text, out var number));
        Assert.Null(number);
    }

    [Fact]
    public void ToonNumber_IsFinite_IsFalseForNaNAndInfinity()
    {
        Assert.False(new ToonNumber(double.NaN).IsFinite);
        Assert.False(new ToonNumber(double.PositiveInfinity).IsFinite);
        Assert.Equal("null", new ToonNumber(double.NaN).ToString());
    }
}
