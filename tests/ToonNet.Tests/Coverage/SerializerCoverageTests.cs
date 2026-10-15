using ToonNet.Core;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Coverage;

/// <summary>
///     Serializer coverage tests to reach 75% overall coverage
///     Focus on ToonSerializer which is currently at 47.5%
/// </summary>
public class SerializerCoverageTests
{
    [Fact]
    public void Serialize_SimpleModel_Success()
    {
        var model = new SimpleModel { Name = "Test", Age = 25, Active = true };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("Name: Test\nAge: 25\nActive: true", result);
    }

    [Fact]
    public void Deserialize_SimpleModel_Success()
    {
        var toon = "Name: Test\nAge: 25\nActive: true";
        var model = ToonSerializer.Deserialize<SimpleModel>(toon);

        Assert.NotNull(model);
        Assert.Equal("Test", model.Name);
        Assert.Equal(25, model.Age);
        Assert.True(model.Active);
    }

    [Fact]
    public void Serialize_WithNulls_HandlesCorrectly()
    {
        var model = new ModelWithNullables
        {
            OptionalString = null,
            OptionalInt = null,
            OptionalDouble = null
        };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("OptionalString: null\nOptionalInt: null\nOptionalDouble: null", result);
    }

    [Fact]
    public void Deserialize_WithNulls_HandlesCorrectly()
    {
        var toon = "OptionalString: null\nOptionalInt: null\nOptionalDouble: null";
        var model = ToonSerializer.Deserialize<ModelWithNullables>(toon);

        Assert.NotNull(model);
        Assert.Null(model.OptionalString);
        Assert.Null(model.OptionalInt);
        Assert.Null(model.OptionalDouble);
    }

    [Fact]
    public void Serialize_WithSomeNulls_HandlesCorrectly()
    {
        var model = new ModelWithNullables
        {
            OptionalString = "Hello",
            OptionalInt = 42,
            OptionalDouble = null
        };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("OptionalString: Hello\nOptionalInt: 42\nOptionalDouble: null", result);
    }

    [Fact]
    public void Serialize_ListOfStrings_Success()
    {
        var model = new ModelWithCollections
        {
            Tags = new List<string> { "tag1", "tag2", "tag3" }
        };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("Tags[3]: tag1,tag2,tag3\nNumbers: null\nSettings: null", result);
    }

    [Fact]
    public void Deserialize_ListOfStrings_Success()
    {
        var toon = "Tags[3]:\n  - tag1\n  - tag2\n  - tag3";
        var model = ToonSerializer.Deserialize<ModelWithCollections>(toon);

        Assert.NotNull(model);
        Assert.NotNull(model.Tags);
        Assert.Equal(3, model.Tags.Count);
        Assert.Contains("tag1", model.Tags);
        Assert.Contains("tag2", model.Tags);
        Assert.Contains("tag3", model.Tags);
    }

    [Fact]
    public void Serialize_ListOfNumbers_Success()
    {
        var model = new ModelWithCollections
        {
            Numbers = new List<int> { 1, 2, 3, 4, 5 }
        };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("Tags: null\nNumbers[5]: 1,2,3,4,5\nSettings: null", result);
    }

    [Fact]
    public void Serialize_Dictionary_Success()
    {
        var model = new ModelWithCollections
        {
            Settings = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("Tags: null\nNumbers: null\nSettings:\n  key1: value1\n  key2: value2", result);
    }

    [Fact]
    public void Deserialize_Dictionary_Success()
    {
        var toon = "Settings:\n  key1: value1\n  key2: value2";
        var model = ToonSerializer.Deserialize<ModelWithCollections>(toon);

        Assert.NotNull(model);
        Assert.NotNull(model.Settings);
        Assert.Equal(2, model.Settings.Count);
        Assert.Equal("value1", model.Settings["key1"]);
        Assert.Equal("value2", model.Settings["key2"]);
    }

    [Fact]
    public void Serialize_NestedModel_Success()
    {
        var model = new NestedModel
        {
            Name = "Outer",
            Inner = new SimpleModel { Name = "Inner", Age = 30, Active = true }
        };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("Name: Outer\nInner:\n  Name: Inner\n  Age: 30\n  Active: true\nItems: null", result);
    }

    [Fact]
    public void Deserialize_NestedModel_Success()
    {
        var toon = "Name: Outer\nInner:\n  Name: Inner\n  Age: 30\n  Active: true";
        var model = ToonSerializer.Deserialize<NestedModel>(toon);

        Assert.NotNull(model);
        Assert.Equal("Outer", model.Name);
        Assert.NotNull(model.Inner);
        Assert.Equal("Inner", model.Inner.Name);
        Assert.Equal(30, model.Inner.Age);
    }

    [Fact]
    public void Serialize_ListOfObjects_Success()
    {
        var model = new NestedModel
        {
            Name = "Container",
            Items = new List<SimpleModel>
            {
                new() { Name = "Item1", Age = 20, Active = true },
                new() { Name = "Item2", Age = 25, Active = false }
            }
        };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("Name: Container\nInner: null\nItems[2]{Name,Age,Active}:\n  Item1,20,true\n  Item2,25,false", result);
    }

    [Fact]
    public void Deserialize_ListOfObjects_Success()
    {
        var toon = @"
Name: Container
Items[2]:
  - Name: Item1
    Age: 20
    Active: true
  - Name: Item2
    Age: 25
    Active: false";
        var model = ToonSerializer.Deserialize<NestedModel>(toon);

        Assert.NotNull(model);
        Assert.Equal("Container", model.Name);
        Assert.NotNull(model.Items);
        Assert.Equal(2, model.Items.Count);
        Assert.Equal("Item1", model.Items[0].Name);
        Assert.Equal(20, model.Items[0].Age);
        Assert.Equal("Item2", model.Items[1].Name);
        Assert.Equal(25, model.Items[1].Age);
    }

    [Fact]
    public void RoundTrip_SimpleModel_PreservesData()
    {
        var original = new SimpleModel { Name = "Test", Age = 30, Active = true };

        var toon = ToonSerializer.Serialize(original);
        var restored = ToonSerializer.Deserialize<SimpleModel>(toon);

        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Age, restored.Age);
        Assert.Equal(original.Active, restored.Active);
    }

    [Fact]
    public void RoundTrip_WithCollections_PreservesData()
    {
        var original = new ModelWithCollections
        {
            Tags = new List<string> { "a", "b", "c" },
            Numbers = new List<int> { 1, 2, 3 }
        };

        var toon = ToonSerializer.Serialize(original);
        var restored = ToonSerializer.Deserialize<ModelWithCollections>(toon);

        Assert.NotNull(restored);
        Assert.NotNull(restored.Tags);
        Assert.Equal(original.Tags.Count, restored.Tags.Count);
        Assert.NotNull(restored.Numbers);
        Assert.Equal(original.Numbers.Count, restored.Numbers.Count);
    }

    [Fact]
    public void RoundTrip_NestedModel_PreservesData()
    {
        var original = new NestedModel
        {
            Name = "Test",
            Inner = new SimpleModel { Name = "Inner", Age = 25, Active = true }
        };

        var toon = ToonSerializer.Serialize(original);
        var restored = ToonSerializer.Deserialize<NestedModel>(toon);

        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored.Name);
        Assert.NotNull(restored.Inner);
        Assert.Equal(original.Inner.Name, restored.Inner.Name);
        Assert.Equal(original.Inner.Age, restored.Inner.Age);
    }

    [Fact]
    public void Serialize_EmptyCollections_HandlesCorrectly()
    {
        var model = new ModelWithCollections
        {
            Tags = new List<string>(),
            Numbers = new List<int>()
        };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("Tags: []\nNumbers: []\nSettings: null", result);
    }

    [Fact]
    public void Serialize_NullCollections_HandlesCorrectly()
    {
        var model = new ModelWithCollections
        {
            Tags = null,
            Numbers = null,
            Settings = null
        };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("Tags: null\nNumbers: null\nSettings: null", result);
    }

    [Fact]
    public void Deserialize_InvalidToon_ThrowsException()
    {
        // A single line without a colon is a valid TOON string primitive (spec §5), which cannot become an object
        var invalidToon = "this is not valid toon format { } [ ]";

        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<SimpleModel>(invalidToon));
        Assert.Contains("Expected an object but got String", ex.Message);
    }

    [Fact]
    public void Serialize_NullObject_HandlesGracefully()
    {
        SimpleModel? model = null;

        var result = ToonSerializer.Serialize(model);

        Assert.Equal("null", result);
        Assert.Null(ToonSerializer.Deserialize<SimpleModel>(result));
    }

    [Fact]
    public void Serialize_WithCustomOptions_UsesIndentSize()
    {
        var model = new NestedModel { Name = "Outer", Inner = new SimpleModel { Name = "Inner", Age = 30, Active = false } };

        var options = new ToonSerializerOptions
        {
            ToonOptions = new ToonOptions { IndentSize = 4 }
        };

        var result = ToonSerializer.Serialize(model, options);

        Assert.Equal("Name: Outer\nInner:\n    Name: Inner\n    Age: 30\n    Active: false\nItems: null", result);
    }

    [Fact]
    public void Deserialize_WithStrictMode_ValidatesData()
    {
        // Strict mode (the default) rejects a repeated key, which non-strict mode reads as the last value
        Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<SimpleModel>("Name: A\nName: B"));
        Assert.Equal("B", ToonSerializer.Deserialize<SimpleModel>("Name: A\nName: B",
                                                                   new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } })!.Name);

        var toon = "Name: Test\nAge: 25\nActive: true";

        var options = new ToonSerializerOptions
        {
            ToonOptions = new ToonOptions { StrictMode = true }
        };

        var model = ToonSerializer.Deserialize<SimpleModel>(toon, options);

        Assert.NotNull(model);
        Assert.Equal("Test", model.Name);
    }

    [Fact]
    public void Serialize_BooleanValues_FormatsCorrectly()
    {
        var model = new SimpleModel { Name = "Test", Age = 0, Active = false };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("Name: Test\nAge: 0\nActive: false", result);
    }

    [Fact]
    public void Serialize_NumberZero_FormatsCorrectly()
    {
        var model = new SimpleModel { Name = "Test", Age = 0, Active = true };
        var result = ToonSerializer.Serialize(model);

        Assert.Equal("Name: Test\nAge: 0\nActive: true", result);
    }

    [Fact]
    public void Deserialize_EmptyString_ReturnsDefaultObject()
    {
        var model = ToonSerializer.Deserialize<SimpleModel>("");

        // An empty document is an empty object
        Assert.NotNull(model);
        Assert.Equal((null, 0, false), (model.Name, model.Age, model.Active));
    }

    public class SimpleModel
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public bool Active { get; set; }
    }

    public class ModelWithNullables
    {
        public string? OptionalString { get; set; }
        public int? OptionalInt { get; set; }
        public double? OptionalDouble { get; set; }
    }

    public class ModelWithCollections
    {
        public List<string>? Tags { get; set; }
        public List<int>? Numbers { get; set; }
        public Dictionary<string, string>? Settings { get; set; }
    }

    public class NestedModel
    {
        public string? Name { get; set; }
        public SimpleModel? Inner { get; set; }
        public List<SimpleModel>? Items { get; set; }
    }
}