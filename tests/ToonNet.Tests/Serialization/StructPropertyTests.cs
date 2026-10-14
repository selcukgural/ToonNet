using ToonNet.Core;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     Primitive properties of structs are read and written in place in the boxed instance, without boxing their
///     values; the text must be that of a class with the same properties.
/// </summary>
public class StructPropertyTests
{
    public enum Level { Low, High }

    public struct Values
    {
        public int Int { get; set; }
        public long Long { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
        public bool Bool { get; set; }
        public char Char { get; set; }
        public string? Text { get; set; }
        public DateTime Date { get; set; }
        public Guid Id { get; set; }
        public TimeSpan Span { get; set; }
        public Level Level { get; set; }
        public int? MaybeInt { get; set; }
        public Level? MaybeLevel { get; set; }
    }

    public class ValuesClass
    {
        public int Int { get; set; }
        public long Long { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
        public bool Bool { get; set; }
        public char Char { get; set; }
        public string? Text { get; set; }
        public DateTime Date { get; set; }
        public Guid Id { get; set; }
        public TimeSpan Span { get; set; }
        public Level Level { get; set; }
        public int? MaybeInt { get; set; }
        public Level? MaybeLevel { get; set; }
    }

    public readonly record struct Point(int X, double Y);

    public readonly struct ReadOnlyPair(int left, int right)
    {
        public int Left { get; } = left;
        public int Right { get; } = right;
    }

    public class Owner
    {
        public Values Values { get; set; }
        public Values? MaybeValues { get; set; }
        public List<Point> Points { get; set; } = [];
        public ReadOnlyPair Pair { get; set; }
    }

    private static Values Create(int i) => new()
    {
        Int = i, Long = -i * 10_000_000_000L, Double = i * 0.25, Decimal = i * 1.10m, Bool = i % 2 == 0, Char = (char)('a' + i), Text = $"t, {i}",
        Date = new DateTime(2026, 10, 14, 1, 2, 3, DateTimeKind.Utc).AddDays(i), Id = new Guid(i, 0, 0, new byte[8]), Span = TimeSpan.FromSeconds(i),
        Level = (Level)(i % 2), MaybeInt = i % 3 == 0 ? null : i, MaybeLevel = i % 2 == 0 ? Level.High : null
    };

    private static ValuesClass Twin(Values v) => new()
    {
        Int = v.Int, Long = v.Long, Double = v.Double, Decimal = v.Decimal, Bool = v.Bool, Char = v.Char, Text = v.Text, Date = v.Date, Id = v.Id,
        Span = v.Span, Level = v.Level, MaybeInt = v.MaybeInt, MaybeLevel = v.MaybeLevel
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StructProperties_AreWrittenLikeClassProperties(bool ignoreNulls)
    {
        var options = new ToonSerializerOptions { IgnoreNullValues = ignoreNulls };

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(ToonSerializer.Serialize(Twin(Create(i)), options), ToonSerializer.Serialize(Create(i), options));
        }

        // A list of structs is a table like a list of the classes
        var structs = Enumerable.Range(0, 4).Select(Create).ToList();
        var classes = structs.Select(Twin).ToList();
        Assert.Equal(ToonSerializer.Serialize(classes, options), ToonSerializer.Serialize(structs, options));
    }

    [Fact]
    public void StructProperties_ReadBack()
    {
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(Create(i), ToonSerializer.Deserialize<Values>(ToonSerializer.Serialize(Create(i))));
        }

        var structs = Enumerable.Range(0, 4).Select(Create).ToList();
        Assert.Equal(structs, ToonSerializer.Deserialize<List<Values>>(ToonSerializer.Serialize(structs)));
    }

    [Fact]
    public void StructsInClasses_RecordStructsAndReadOnlyStructs()
    {
        var owner = new Owner { Values = Create(1), MaybeValues = Create(2), Points = [new Point(1, 0.5), new Point(-2, 1e30)], Pair = new ReadOnlyPair(3, 4) };
        var text = ToonSerializer.Serialize(owner);

        Assert.Contains("Points[2]{X,Y}:", text);
        Assert.Contains("Pair:\n  Left: 3\n  Right: 4", text);

        var read = ToonSerializer.Deserialize<Owner>(text)!;
        Assert.Equal(owner.Values, read.Values);
        Assert.Equal(owner.MaybeValues, read.MaybeValues);
        Assert.Equal(owner.Points, read.Points);

        // Get-only properties are set through the constructor
        Assert.Equal(owner.Pair, read.Pair);
    }

    [Fact]
    public void InvalidValues_ReportTheTreeError()
    {
        const string toon = "Int: x";

        var direct = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Values>(toon));
        var tree = Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeFromValue<Values>(ToonNet.Core.Models.ToonDocument.Parse(toon).Root));
        Assert.Equal(tree.Message, direct.Message);
    }
}
