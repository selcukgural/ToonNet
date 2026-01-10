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
    public void ToonObject_Indexer_Set_Null_DoesNotAdd()
    {
        var obj = new ToonObject();
        obj["k"] = null;
        Assert.Empty(obj.Properties);
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
        arr.Items.Add(new ToonString("first"));
        Assert.Equal("first", ((ToonString)arr[0]).Value);
    }

    [Fact]
    public void ToonArray_Indexer_Set_UpdatesItem()
    {
        var arr = new ToonArray();
        arr.Items.Add(new ToonString("old"));
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
        arr.Items.Add(new ToonNumber(42));
        arr.Items.Add(new ToonString("text"));
        arr.Items.Add(new ToonBoolean(true));
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
}