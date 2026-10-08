using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     Covers the .NET type mapping of the reflection-based serializer.
/// </summary>
public class ToonSerializerTypeSupportTests
{
    public record Person(string Name, int Age, List<string> Tags);

    public record struct Point(int X, int Y);

    public struct MutablePoint
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class WithConstructor
    {
        public WithConstructor(string name, int count = 7)
        {
            Name = name;
            Count = count;
        }

        public string Name { get; }
        public int Count { get; }
        public string? Extra { get; set; }
    }

    public class Numbers
    {
        public int I { get; set; }
        public long L { get; set; }
        public decimal D { get; set; }
        public byte B { get; set; }
    }

    public class Times
    {
        public DateTime Utc { get; set; }
        public DateTime Local { get; set; }
        public DateTimeOffset Offset { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
        public TimeSpan Span { get; set; }
        public Guid Id { get; set; }
        public Uri? Link { get; set; }
        public char C { get; set; }
        public BigInteger Big { get; set; }
    }

    public class Collections
    {
        public HashSet<string> Set { get; set; } = [];
        public IReadOnlyList<int> ReadOnly { get; set; } = [];
        public ISet<int> ISet { get; set; } = new HashSet<int>();
        public ImmutableArray<int> Immutable { get; set; }
        public Dictionary<int, string> ByInt { get; set; } = [];
        public IReadOnlyDictionary<Guid, int> ByGuid { get; set; } = new Dictionary<Guid, int>();
        public SortedDictionary<string, int> Sorted { get; set; } = [];
    }

    public class Holder
    {
        public object? Payload { get; set; }
    }

    public abstract class Shape
    {
        public string Color { get; set; } = "";
    }

    public class Circle : Shape
    {
        public double Radius { get; set; }
    }

    public class Node
    {
        public string Name { get; set; } = "";
        public Node? Next { get; set; }
    }

    public class WithIndexer
    {
        public string Name { get; set; } = "";
        public int this[int index] => index;
    }

    public class Ordered
    {
        [ToonPropertyOrder(2)] public int Second { get; set; }
        [ToonPropertyOrder(1)] public int First { get; set; }
        [ToonProperty("custom_name")] public string Renamed { get; set; } = "";
        [ToonIgnore] public string Ignored { get; set; } = "x";
    }

    public class UpperCaseConverter : ToonConverter<string>
    {
        public override ToonValue? Write(string? value, ToonSerializerOptions options) => new ToonString(value!.ToUpperInvariant());
        public override string? Read(ToonValue value, ToonSerializerOptions options) => ((ToonString)value).Value.ToLowerInvariant();
    }

    public class WithConverterAttribute
    {
        [ToonConverter(typeof(UpperCaseConverter))] public string Code { get; set; } = "";
    }

    public class NullableItems
    {
        public List<string?> Items { get; set; } = [];
        public string? Missing { get; set; }
    }

    [Fact]
    public void PositionalRecord_RoundTrips()
    {
        var person = new Person("Ada", 36, ["math", "code"]);

        var back = ToonSerializer.Deserialize<Person>(ToonSerializer.Serialize(person))!;

        Assert.Equal("Ada", back.Name);
        Assert.Equal(36, back.Age);
        Assert.Equal(["math", "code"], back.Tags);
    }

    [Fact]
    public void Structs_RoundTrip()
    {
        Assert.Equal(new Point(1, 2), ToonSerializer.Deserialize<Point>(ToonSerializer.Serialize(new Point(1, 2))));

        var mutable = ToonSerializer.Deserialize<MutablePoint>("X: 3\nY: 4");
        Assert.Equal(3, mutable.X);
        Assert.Equal(4, mutable.Y);
    }

    [Fact]
    public void ParameterizedConstructor_UsesDefaultsAndSetsRemainingProperties()
    {
        var value = ToonSerializer.Deserialize<WithConstructor>("Name: x\nExtra: e")!;

        Assert.Equal("x", value.Name);
        Assert.Equal(7, value.Count);
        Assert.Equal("e", value.Extra);
    }

    [Fact]
    public void LongAndDecimal_RoundTripExactly()
    {
        var numbers = new Numbers { L = 9007199254740993L, D = 12345678901234567890.123456789m };

        var back = ToonSerializer.Deserialize<Numbers>(ToonSerializer.Serialize(numbers))!;

        Assert.Equal(9007199254740993L, back.L);
        Assert.Equal(12345678901234567890.123456789m, back.D);
    }

    [Theory]
    [InlineData("I: 3000000000", "outside the range of Int32")]
    [InlineData("I: 3.9", "not an integer")]
    [InlineData("B: -1", "outside the range of Byte")]
    [InlineData("I: abc", "is not a number")]
    [InlineData("I: null", "Cannot convert null to the non-nullable type Int32")]
    public void InvalidNumbers_ThrowWithPath(string toon, string expected)
    {
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Numbers>(toon));

        Assert.Contains(expected, ex.Message);
        Assert.Contains("Path: $.", ex.Message);
    }

    [Fact]
    public void QuotedNumber_IsAcceptedForNumericTarget()
    {
        Assert.Equal(42, ToonSerializer.Deserialize<Numbers>("I: \"42\"")!.I);
    }

    [Fact]
    public void DateAndTimeTypes_RoundTripInAnyCulture()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

        try
        {
            var times = new Times
            {
                Utc = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                Local = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Unspecified),
                Offset = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(3)),
                Date = new DateOnly(2024, 2, 29),
                Time = new TimeOnly(13, 45, 10),
                Span = new TimeSpan(1, 2, 3, 4),
                Id = Guid.NewGuid(),
                Link = new Uri("https://example.com/a?b=c"),
                C = 'ç',
                Big = BigInteger.Parse("123456789012345678901234567890123456789")
            };

            var toon = ToonSerializer.Serialize(times);
            var back = ToonSerializer.Deserialize<Times>(toon)!;

            Assert.Contains("Date: 2024-02-29", toon);
            Assert.Contains("Time: \"13:45:10\"", toon);
            Assert.Equal(times.Utc, back.Utc);
            Assert.Equal(DateTimeKind.Utc, back.Utc.Kind);
            Assert.Equal(DateTimeKind.Unspecified, back.Local.Kind);
            Assert.Equal(times.Offset, back.Offset);
            Assert.Equal(times.Date, back.Date);
            Assert.Equal(times.Time, back.Time);
            Assert.Equal(times.Span, back.Span);
            Assert.Equal(times.Id, back.Id);
            Assert.Equal(times.Link, back.Link);
            Assert.Equal('ç', back.C);
            Assert.Equal(times.Big, back.Big);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void CollectionAndDictionaryTypes_RoundTrip()
    {
        var guid = Guid.NewGuid();
        var collections = new Collections
        {
            Set = ["a", "b"],
            ReadOnly = [1, 2],
            ISet = new HashSet<int> { 3 },
            Immutable = [4, 5],
            ByInt = new Dictionary<int, string> { [1] = "one" },
            ByGuid = new Dictionary<Guid, int> { [guid] = 9 },
            Sorted = new SortedDictionary<string, int> { ["z"] = 1, ["a"] = 2 }
        };

        var back = ToonSerializer.Deserialize<Collections>(ToonSerializer.Serialize(collections))!;

        Assert.Equal(collections.Set, back.Set);
        Assert.Equal([1, 2], back.ReadOnly);
        Assert.Equal([3], back.ISet);
        Assert.Equal([4, 5], back.Immutable);
        Assert.Equal("one", back.ByInt[1]);
        Assert.Equal(9, back.ByGuid[guid]);
        Assert.Equal(2, back.Sorted["a"]);
    }

    [Fact]
    public void ObjectTypedValues_UseRuntimeType()
    {
        Assert.Equal("Payload:\n  Color: red\n  Radius: 2", ToonSerializer.Serialize(new Holder { Payload = new Circle { Color = "red", Radius = 2 } }));
        Assert.Equal("Color: x\nRadius: 1", ToonSerializer.Serialize<Shape>(new Circle { Color = "x", Radius = 1 }));
        Assert.Equal("A: 1", ToonSerializer.Serialize<object>(new { A = 1 }));
    }

    [Fact]
    public void DeserializeToObject_ReturnsDictionariesAndLists()
    {
        var value = ToonSerializer.Deserialize<object>("a: 1\nb[2]: x,2.5\nc:\n  d: true");

        var root = Assert.IsType<Dictionary<string, object?>>(value);
        Assert.Equal(1L, root["a"]);
        Assert.Equal(new List<object?> { "x", 2.5m }, root["b"]);
        Assert.Equal(true, ((Dictionary<string, object?>)root["c"]!)["d"]);
    }

    [Fact]
    public void DeserializeToToonValue_ReturnsTheParsedValue()
    {
        var value = ToonSerializer.Deserialize<ToonObject>("a: 1")!;

        Assert.Equal(1, ((ToonNumber)value["a"]!).Value);
    }

    [Fact]
    public void AbstractTarget_ThrowsHelpfulError()
    {
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Shape>("Color: red"));

        Assert.Contains("register a converter", ex.Message);
    }

    [Fact]
    public void CircularReference_ThrowsWithPath()
    {
        var node = new Node { Name = "a" };
        node.Next = new Node { Name = "b", Next = node };

        var ex = Assert.Throws<ToonEncodingException>(() => ToonSerializer.Serialize(node));

        Assert.Contains("circular reference", ex.Message);
        Assert.Contains("$.Next.Next", ex.Message);
    }

    [Fact]
    public void Indexers_AreSkipped()
    {
        Assert.Equal("Name: n", ToonSerializer.Serialize(new WithIndexer { Name = "n" }));
        Assert.Equal("n", ToonSerializer.Deserialize<WithIndexer>("Name: n")!.Name);
    }

    [Fact]
    public void Attributes_ControlOrderNamesIgnoreAndConverters()
    {
        // Properties without [ToonPropertyOrder] have order 0, like System.Text.Json
        Assert.Equal("custom_name: r\nFirst: 1\nSecond: 2", ToonSerializer.Serialize(new Ordered { First = 1, Second = 2, Renamed = "r" }));

        var toon = ToonSerializer.Serialize(new WithConverterAttribute { Code = "abc" });
        Assert.Equal("Code: ABC", toon);
        Assert.Equal("abc", ToonSerializer.Deserialize<WithConverterAttribute>(toon)!.Code);
    }

    [Fact]
    public void IgnoreNullValues_KeepsNullArrayItems()
    {
        var options = new ToonSerializerOptions { IgnoreNullValues = true };

        var toon = ToonSerializer.Serialize(new NullableItems { Items = ["a", null, "c"] }, options);

        Assert.Equal("Items[3]: a,null,c", toon);
    }

    [Fact]
    public void UnquotedNumberOrBoolean_IsAcceptedForStringTarget()
    {
        var value = ToonSerializer.Deserialize<NullableItems>("Missing: 12345")!;

        Assert.Equal("12345", value.Missing);
    }
}
