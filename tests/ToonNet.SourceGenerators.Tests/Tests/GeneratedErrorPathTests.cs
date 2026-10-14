using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
///     Errors raised in generated code carry the same path and message as the reflection-based serializer's, through
///     every entry point (the path is completed as an error leaves each property, item and dictionary entry).
/// </summary>
public class GeneratedErrorPathTests
{
    private static string Error(Action action)
    {
        var ex = Assert.ThrowsAny<ToonException>(action);
        return ex switch
        {
            ToonSerializationException s => $"{s.Message} [{s.Path}]",
            ToonEncodingException e => $"{e.Message} [{e.PropertyPath}]",
            _ => ex.Message
        };
    }

    private static void AssertReadParity(string toon, ToonSerializerOptions? options = null)
    {
        options ??= new ToonSerializerOptions();
        var expected = Error(() => ToonSerializer.Deserialize<PathRoot>(toon, ReflectionOnly.Options(options)));

        Assert.Equal(expected, Error(() => ToonSerializer.Deserialize<PathRoot>(toon, options)));
        Assert.Equal(expected, Error(() => ToonSerializer.Deserialize(toon, PathRoot.ToonTypeInfo, options)));
        Assert.Equal(expected, Error(() => PathRoot.Deserialize(ToonDocument.Parse(toon), options)));
        Assert.Equal(expected, Error(() => ToonSerializer.DeserializeFromValue<PathRoot>(ToonDocument.Parse(toon).Root, options)));
    }

    [Theory]
    [InlineData("Count: many")]
    [InlineData("Child:\n  Count: many")]
    [InlineData("Child:\n  Child:\n    When: someday")]
    [InlineData("Items[2]:\n  - Count: 1\n  - Count: x")]
    [InlineData("Items[1]:\n  - Child:\n      Count: x")]
    [InlineData("Numbers[3]: 1,2,x")]
    [InlineData("Grid[2]:\n  - [1]: 1\n  - [2]: 2,x")]
    [InlineData("ByName:\n  a:\n    Count: 1\n  b:\n    Count: x")]
    [InlineData("ById:\n  7: 1\n  x: 2")]
    [InlineData("ById:\n  7: x")]
    [InlineData("Point:\n  X: 1\n  Y: x")]
    [InlineData("Point:\n  X: 1\n  Y: 2\nChild:\n  Point:\n    X: x")]
    [InlineData("Loose:\n  Value: x")]
    public void ReadingErrors_HaveTheSamePath(string toon) => AssertReadParity(toon);

    [Fact]
    public void DepthErrors_HaveTheSamePath()
    {
        const string toon = "Child:\n  Child:\n    Child:\n      Count: 1";

        AssertReadParity(toon, new ToonSerializerOptions { MaxDepth = 2 });
        AssertReadParity("Items[1]:\n  - Items[1]:\n      - Count: 1", new ToonSerializerOptions { MaxDepth = 2 });
        AssertReadParity("ByName:\n  a:\n    ByName:\n      b:\n        Count: 1", new ToonSerializerOptions { MaxDepth = 3 });
    }

    [Fact]
    public void StrictReadingErrors_HaveTheSamePath()
    {
        var options = new ToonSerializerOptions { UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow, RespectRequiredMembers = true };

        AssertReadParity("Items[1]:\n  - Count: 1\n    Extra: 2", options);
        AssertReadParity("ByName:\n  a:\n    Count: 1\n    Point:\n      X: 1\n      Z: 2", options);
    }

    [Fact]
    public void WritingDepthErrors_HaveTheSamePath()
    {
        var root = new PathRoot { Items = [new PathRoot(), new PathRoot { Child = new PathRoot { ByName = new() { ["k"] = new PathRoot { Child = new PathRoot() } } } }] };
        var options = new ToonSerializerOptions { MaxDepth = 4 };
        var expected = Error(() => ToonSerializer.Serialize(root, ReflectionOnly.Options(options)));

        Assert.Contains("$.Items[1].Child.ByName.k", expected);
        Assert.Equal(expected, Error(() => ToonSerializer.Serialize(root, options)));
        Assert.Equal(expected, Error(() => ToonSerializer.Serialize(root, PathRoot.ToonTypeInfo, options)));
        Assert.Equal(expected, Error(() => PathRoot.Serialize(root, options)));
    }

    [Fact]
    public void ComposedTypeInfos_AddTheIndexOrKey()
    {
        var list = Error(() => ToonSerializer.Deserialize("[2]:\n  - Count: 1\n  - Count: x", ToonTypeInfo.CreateList(PathRoot.ToonTypeInfo)));
        Assert.Equal(Error(() => ToonSerializer.Deserialize<List<PathRoot>>("[2]:\n  - Count: 1\n  - Count: x", ReflectionOnly.Options())), list);

        var dictionary = Error(() => ToonSerializer.Deserialize("a:\n  Count: x", ToonTypeInfo.CreateDictionary(PathRoot.ToonTypeInfo)));
        Assert.Equal(Error(() => ToonSerializer.Deserialize<Dictionary<string, PathRoot>>("a:\n  Count: x", ReflectionOnly.Options())), dictionary);
        Assert.Contains("[$.a.Count]", dictionary);
    }

    [Fact]
    public void ErrorsCreatedByCallers_KeepTheirPath()
    {
        var ex = ToonSerializationException.Create("custom", typeof(int), "Prop");
        var copy = Assert.Throws<ToonSerializationException>(() => PathRoot.Deserialize(ToonDocument.Parse("Custom: 1"), WithThrowingConverter(ex)));

        Assert.Same(ex, copy);
        Assert.Equal("Prop", copy.Path);
        Assert.Equal("custom (target type: Int32, property: Prop)", copy.Message);
    }

    private static ToonSerializerOptions WithThrowingConverter(Exception ex)
    {
        var options = new ToonSerializerOptions();
        options.AddConverter(new ThrowingConverter(ex));
        return options;
    }

    private sealed class ThrowingConverter(Exception ex) : ToonConverter<CustomValue>
    {
        public override ToonValue Write(CustomValue? value, ToonSerializerOptions options) => throw ex;

        public override CustomValue Read(ToonValue value, ToonSerializerOptions options) => throw ex;
    }
}

public sealed class CustomValue;

/// <summary>A plain class (no generated code) inside generated code: the serializer's path continues from it.</summary>
public sealed class LooseValue
{
    public int Value { get; set; }
}

[ToonSerializable]
public partial class PathRoot
{
    public int Count { get; set; }
    public DateTime? When { get; set; }
    public PathRoot? Child { get; set; }
    public List<PathRoot>? Items { get; set; }
    public int[]? Numbers { get; set; }
    public List<int[]>? Grid { get; set; }
    public Dictionary<string, PathRoot>? ByName { get; set; }
    public Dictionary<int, int>? ById { get; set; }
    public PathPoint? Point { get; set; }
    public LooseValue? Loose { get; set; }
    public CustomValue? Custom { get; set; }
}

[ToonSerializable]
public partial record struct PathPoint(int X, int Y);
