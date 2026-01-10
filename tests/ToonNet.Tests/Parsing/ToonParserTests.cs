using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Tests.Parsing;

public class ToonParserTests
{
    [Fact]
    public void Parse_SimpleKeyValue_ReturnsObject()
    {
        // Arrange
        var input = "name: Alice";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.NotNull(obj["name"]);
        Assert.IsType<ToonString>(obj["name"]);
        Assert.Equal("Alice", ((ToonString)obj["name"]!).Value);
    }

    [Fact]
    public void Parse_MultipleFields_ReturnsObjectWithAllFields()
    {
        // Arrange
        var input = @"id: 1
name: Alice
active: true";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.Equal(3, obj.Properties.Count);
        Assert.IsType<ToonNumber>(obj["id"]);
        Assert.Equal(1.0, ((ToonNumber)obj["id"]!).Value);
        Assert.IsType<ToonString>(obj["name"]);
        Assert.Equal("Alice", ((ToonString)obj["name"]!).Value);
        Assert.IsType<ToonBoolean>(obj["active"]);
        Assert.True(((ToonBoolean)obj["active"]!).Value);
    }

    [Fact]
    public void Parse_NestedObject_ReturnsNestedStructure()
    {
        // Arrange
        var input = @"user:
  name: Alice
  age: 30";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.NotNull(obj["user"]);
        Assert.IsType<ToonObject>(obj["user"]);

        var user = (ToonObject)obj["user"]!;
        Assert.Equal("Alice", ((ToonString)user["name"]!).Value);
        Assert.Equal(30.0, ((ToonNumber)user["age"]!).Value);
    }

    [Fact]
    public void Parse_PrimitiveArray_ReturnsArray()
    {
        // Arrange
        var input = "tags[3]: admin,user,guest";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.NotNull(obj["tags"]);
        Assert.IsType<ToonArray>(obj["tags"]);

        var array = (ToonArray)obj["tags"]!;
        Assert.Equal(3, array.Count);
        Assert.Equal("admin", ((ToonString)array[0]).Value);
        Assert.Equal("user", ((ToonString)array[1]).Value);
        Assert.Equal("guest", ((ToonString)array[2]).Value);
    }

    [Fact]
    public void Parse_TabularArray_ReturnsArrayOfObjects()
    {
        // Arrange
        var input = @"users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.NotNull(obj["users"]);
        Assert.IsType<ToonArray>(obj["users"]);

        var array = (ToonArray)obj["users"]!;
        Assert.Equal(2, array.Count);
        Assert.True(array.IsTabular);
        Assert.Equal(["id", "name", "role"], array.FieldNames);

        var firstUser = (ToonObject)array[0];
        Assert.Equal(1.0, ((ToonNumber)firstUser["id"]!).Value);
        Assert.Equal("Alice", ((ToonString)firstUser["name"]!).Value);
        Assert.Equal("admin", ((ToonString)firstUser["role"]!).Value);
    }

    [Fact]
    public void Parse_BooleanValues_ReturnsCorrectBooleans()
    {
        // Arrange
        var input = @"active: true
deleted: false";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.True(((ToonBoolean)obj["active"]!).Value);
        Assert.False(((ToonBoolean)obj["deleted"]!).Value);
    }

    [Fact]
    public void Parse_NullValue_ReturnsNull()
    {
        // Arrange
        var input = "optional: null";
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse(input);

        // Assert
        var obj = doc.AsObject();
        Assert.IsType<ToonNull>(obj["optional"]);
    }
}