using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Models;

public class ToonValueAccessTests
{
    private const string Users = """
                                 users[2]{id,name,joined}:
                                   1,Ada,2024-01-02
                                   2,Bob,2025-03-04
                                 meta:
                                   total: 2
                                   price: 1.50
                                   tags[2]: a,b
                                 """;

    private static ToonValue Root => ToonDocument.Parse(Users).Root;

    [Fact]
    public void Indexers_Chain()
    {
        var root = Root;

        Assert.Equal("Bob", root["users"]![1]["name"]!.GetValue<string>());
        Assert.Equal(2, root["meta"]!["total"]!.GetValue<int>());
        Assert.Equal("b", root["meta"]!["tags"]![1].GetValue<string>());
        Assert.Null(root["missing"]);
    }

    [Fact]
    public void Indexers_Set()
    {
        var root = Root;

        root["meta"]!["total"] = 3;
        root["users"]![0] = "gone";
        root["meta"]!["note"] = null;

        Assert.Equal(3, root["meta"]!["total"]!.GetValue<long>());
        Assert.Equal("gone", root["users"]![0].GetValue<string>());
        Assert.Same(ToonNull.Instance, root["meta"]!["note"]);
    }

    [Fact]
    public void Indexers_OnTheWrongKind_Throw()
    {
        var root = Root;

        var ex = Assert.Throws<InvalidOperationException>(() => root[0]);
        Assert.Equal("The value is an object, not an array.", ex.Message);
        ex = Assert.Throws<InvalidOperationException>(() => root["users"]!["x"]);
        Assert.Equal("The value is an array, not an object.", ex.Message);
        Assert.Throws<InvalidOperationException>(() => root["meta"]!["total"]!["x"] = 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => root["users"]![5]);
    }

    [Fact]
    public void AsObject_AsArray()
    {
        var root = Root;

        Assert.Same(root, root.AsObject());
        Assert.Equal(2, root["users"]!.AsArray().Count);
        Assert.Equal("The value is a number, not an object.",
                     Assert.Throws<InvalidOperationException>(() => root["meta"]!["total"]!.AsObject()).Message);
        Assert.Equal("The value is null, not an array.", Assert.Throws<InvalidOperationException>(() => ToonNull.Instance.AsArray()).Message);
    }

    [Fact]
    public void GetValue_ConvertsLikeTheSerializer()
    {
        var root = Root;

        Assert.Equal(1.5m, root["meta"]!["price"]!.GetValue<decimal>());
        Assert.Equal(1.5, root["meta"]!["price"]!.GetValue<double>());
        Assert.Equal(new DateTime(2025, 3, 4), root["users"]![1]["joined"]!.GetValue<DateTime>());
        Assert.Equal(new DateOnly(2024, 1, 2), root["users"]![0]["joined"]!.GetValue<DateOnly>());
        Assert.Null(ToonNull.Instance.GetValue<int?>());
        Assert.Null(ToonNull.Instance.GetValue<string>());
        Assert.Equal(DayOfWeek.Friday, new ToonString("Friday").GetValue<DayOfWeek>());
        Assert.Equal("2", root["meta"]!["total"]!.GetValue<string>());

        Assert.Throws<ToonSerializationException>(() => root["meta"]!["price"]!.GetValue<int>());
        Assert.Throws<ToonSerializationException>(() => root["meta"]!.GetValue<int>());
        Assert.Throws<ToonSerializationException>(() => ToonNull.Instance.GetValue<int>());
    }

    [Fact]
    public void TryGetValue_DoesNotThrow()
    {
        var root = Root;

        Assert.True(root["meta"]!["total"]!.TryGetValue<int>(out var total));
        Assert.Equal(2, total);
        Assert.False(root["meta"]!["price"]!.TryGetValue<int>(out _));
        Assert.False(root["users"]![0]["name"]!.TryGetValue<Guid>(out _));
        Assert.False(root["meta"]!.TryGetValue<string>(out _));
        Assert.False(root["users"]!.TryGetValue<string>(out _));
        Assert.False(new ToonNumber(300).TryGetValue<byte>(out _));
    }

    private sealed record User(int Id, string Name, DateOnly Joined);

    [Fact]
    public void Deserialize_ConvertsWithTheSerializer()
    {
        var users = Root["users"]!.Deserialize<List<User>>(new ToonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal([new User(1, "Ada", new DateOnly(2024, 1, 2)), new User(2, "Bob", new DateOnly(2025, 3, 4))], users);
        Assert.Equal(["a", "b"], Root["meta"]!["tags"]!.Deserialize<string[]>()!);
    }

    [Fact]
    public void DeepClone_CopiesContainers()
    {
        var root = Root;
        var copy = root.DeepClone();

        Assert.NotSame(root, copy);
        Assert.NotSame(root["users"], copy["users"]);
        Assert.NotSame(root["users"]![0], copy["users"]![0]);
        Assert.Same(root["meta"]!["total"], copy["meta"]!["total"]);
        Assert.Equal(["id", "name", "joined"], copy["users"]!.AsArray().FieldNames!);
        Assert.NotSame(root["users"]!.AsArray().FieldNames, copy["users"]!.AsArray().FieldNames!);
        Assert.True(ToonValue.DeepEquals(root, copy));

        copy["meta"]!["total"] = 5;
        copy["users"]!.AsArray().Add("x");
        Assert.Equal(2, root["meta"]!["total"]!.GetValue<int>());
        Assert.Equal(2, root["users"]!.AsArray().Count);
        Assert.Equal(ToonSerializer.Serialize(ToonDocument.Parse(Users).Root), ToonSerializer.Serialize(root));
    }

    [Fact]
    public void DeepClone_KeepsKeyOrder()
    {
        var root = ToonDocument.Parse("b: 1\na: 2\nc: 3").Root;

        Assert.Equal(["b", "a", "c"], root.DeepClone().AsObject().Properties.Keys.ToArray());
    }

    [Fact]
    public void DeepEquals_ComparesData()
    {
        Assert.True(ToonValue.DeepEquals(null, null));
        Assert.False(ToonValue.DeepEquals(null, ToonNull.Instance));
        Assert.True(ToonValue.DeepEquals(ToonNull.Instance, ToonNull.Instance));
        Assert.True(ToonValue.DeepEquals(new ToonNumber(1L), new ToonNumber(1.0)));
        Assert.True(ToonValue.DeepEquals(new ToonNumber(1.50m), ToonDocument.Parse("1.5").Root));
        Assert.False(ToonValue.DeepEquals(new ToonNumber(1L), new ToonString("1")));
        Assert.False(ToonValue.DeepEquals(new ToonNumber(0.1), new ToonNumber(0.1000000001)));
        Assert.True(ToonValue.DeepEquals(ToonDocument.Parse("123456789012345678901234567890").Root,
                                         ToonDocument.Parse("123456789012345678901234567890").Root));
        Assert.False(ToonValue.DeepEquals(ToonDocument.Parse("123456789012345678901234567890").Root,
                                          ToonDocument.Parse("123456789012345678901234567891").Root));

        // Key order does not matter, item order does; field names describe the text only
        Assert.True(ToonValue.DeepEquals(ToonDocument.Parse("a: 1\nb: 2").Root, ToonDocument.Parse("b: 2\na: 1").Root));
        Assert.False(ToonValue.DeepEquals(ToonDocument.Parse("a: 1\nb: 2").Root, ToonDocument.Parse("a: 1").Root));
        Assert.False(ToonValue.DeepEquals(ToonDocument.Parse("a: 1\nb: 2").Root, ToonDocument.Parse("a: 1\nc: 2").Root));
        Assert.False(ToonValue.DeepEquals(ToonDocument.Parse("[2]: 1,2").Root, ToonDocument.Parse("[2]: 2,1").Root));
        Assert.True(ToonValue.DeepEquals(ToonDocument.Parse("[1]{a}:\n  1").Root, ToonDocument.Parse("[1]:\n  - a: 1").Root));
        Assert.False(ToonValue.DeepEquals(ToonDocument.Parse("a[1]: x").Root, ToonDocument.Parse("a[2]: x,x").Root));
    }

    [Fact]
    public void DeepClone_And_DeepEquals_GuardTheStack()
    {
        ToonValue deep = new ToonArray();

        for (var i = 0; i < 1_000_000; i++)
        {
            deep = new ToonArray([deep]);
        }

        Assert.Throws<InsufficientExecutionStackException>(() => deep.DeepClone());
        Assert.Throws<InsufficientExecutionStackException>(() => ToonValue.DeepEquals(deep, deep.AsArray()[0]));
    }
}
