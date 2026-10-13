using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using static ToonNet.Tests.Serialization.DirectWritingParityTests;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     <see cref="ToonSerializer.Deserialize(string, Type, ToonSerializerOptions)"/> reads objects straight from the
///     text. These tests compare it with converting the parsed tree (<see cref="ToonSerializer.DeserializeFromValue(ToonValue, Type, ToonSerializerOptions)"/>)
///     and check when it falls back to the tree-based path.
/// </summary>
public class DirectReadingParityTests
{
    private static T? AssertParity<T>(string toon, ToonSerializerOptions? options = null, bool expectFallback = false)
    {
        options ??= new ToonSerializerOptions();
        var fallbacks = ToonSerializer.DirectReadFallbacks;

        var direct = ToonSerializer.Deserialize<T>(toon, options);

        Assert.Equal(fallbacks + (expectFallback ? 1 : 0), ToonSerializer.DirectReadFallbacks);
        var tree = ToonSerializer.DeserializeFromValue<T>(ToonDocument.Parse(toon, options.ToonOptions).Root, options);
        Assert.Equivalent(tree, direct, strict: true);
        Assert.Equal(ToonSerializer.Serialize(tree, options), ToonSerializer.Serialize(direct, options));
        return direct;
    }

    private static T? RoundTrip<T>(T value, ToonSerializerOptions? options = null) =>
        AssertParity<T>(ToonSerializer.Serialize(value, options), options);

    private static IEnumerable<ToonSerializerOptions> OptionVariants()
    {
        yield return new ToonSerializerOptions();
        yield return new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase };
        yield return new ToonSerializerOptions { ToonOptions = new ToonOptions { Delimiter = '|', IndentSize = 4 } };
        yield return new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } };
    }

    private static Person CreatePerson() => new()
    {
        Name = "Ada",
        Home = new Address { City = "London" },
        Places = [new Address { City = "A", Zip = "1" }, new Address { City = "B" }],
        Numbers = [1, null, 3],
        Tags = ["x", "y z"],
        Scores = new Dictionary<string, int> { ["math"] = 1, ["needs quote"] = 2 },
        ById = new Dictionary<int, Address> { [7] = new() { City = "C" } },
        Matrix = [[1, 2], [], [3]],
        Mixed = [1L, "two", null],
        Anything = "text",
        Empties = [new Empty(), new Empty()]
    };

    [Fact]
    public void RoundTrips_MatchTheTree()
    {
        foreach (var options in OptionVariants())
        {
            RoundTrip(CreatePerson(), options);
            RoundTrip(new Person(), options);
            RoundTrip(new Derived(), options);
            RoundTrip(new Primitives { FNaN = 1.25f, DInf = -2.5 }, options); // NaN and infinities are written as null
            RoundTrip(new Point { X = 1, Y = 2 }, options);
            RoundTrip(new Positional("p", 3, ["a"]), options);
            RoundTrip(new List<Row> { new() { Id = 1, Name = "a", Score = 1.5, Extra = 5L }, new() { Id = 2 } }, options);
            RoundTrip(new List<Address> { new() { City = "a" }, new() { City = "b", Zip = "z" } }, options);
            RoundTrip(new Dictionary<string, List<int>> { ["a"] = [1], ["b"] = [] }, options);
            RoundTrip(new[] { 1.5, -0.25, 1e-7, 1e21 }, options);
            RoundTrip(new List<decimal?> { 1.50m, null, 79228162514264337593543950335m }, options);
        }
    }

    [Fact]
    public void HandWrittenDocuments_MatchTheTree()
    {
        // Unknown keys are skipped; quoted numbers and numbers for strings take the general conversion
        AssertParity<Address>("City: Paris\nUnknown:\n  Deep[2]: 1,2\nZip: 1.50");
        AssertParity<Row>("Id: \"42\"\nName: 007\nScore: 1e2\nColor: green\nWhen: 2026-01-01T00:00:00Z");
        AssertParity<Row>("Id: 1.0\nName: true\nScore: null");
        AssertParity<Address>("City: \"esc\\\"aped\\n\"\nZip: \"\"");
        AssertParity<List<Address>>("[2]{City,Zip}:\n  a,\n  \"b,c\",null");
        AssertParity<Dictionary<string, object?>>("a: 1\nb[2]: x,y\nc:\n  d: true");
        AssertParity<Primitives>("C: x\nH: 0.5\nBig: 123456789012345678901234567890\nWide: 5\nDay: 2026-10-13\nLink: https://a.b");

        // Dates, times, Guid and enums are read from their tokens: quoted, unquoted, escaped and odd forms
        AssertParity<Primitives>("Day: 2026-10-13\nTime: \"01:02:03.004\"\nSpan: 1.06:00:00\nOffset: \"2026-10-13T01:02:03+03:00\"\n" +
                                 "Id: C56A4180-65AA-42EC-A945-5FD21DEC0538\nMaybeColor: GREEN");
        AssertParity<Primitives>("Time: \"01:0\\u0032\"\nMaybeColor: \"1\"");
        AssertParity<Row>("Color: \" red \"\nWhen: \"2026-01-01\"");
        AssertParity<List<Color?>>("[4]: Red,green,1,null");

        var lenient = new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } };
        AssertParity<List<Address>>("[5]{City,Zip}:\n  a,\n\n  b,c", lenient);
        AssertParity<Person>("Name: x\nTags[1]:\n  - a\n  - b", lenient);

        // Keyed tabular objects and nested field groups (spec 4.4 §9.3, §9.5)
        AssertParity<Dictionary<string, Address>>("[2:]{City,Zip}:\n  a: x,1\n  \"b c\": y,");
        AssertParity<Person>("Name: x\nScores[0:]{v}:\nById[2:]{City,Zip}:\n  7: a,b\n  8: c,d");
        AssertParity<List<Person>>("[2]{Name,Home{City,Zip}}:\n  a,x,1\n  b,y,null");
        AssertParity<Dictionary<string, Person>>("[2:]{Name,Home{City}}:\n  p: a,x\n  q: b,y");
        AssertParity<List<Dictionary<string, object?>>>("[1]{a,g{b,h{c}}}:\n  1,2,3");
        AssertParity<List<Dictionary<string, object?>>>("[1]{a,a,g{b},g{c}}:\n  1,2,3,4", lenient);
        AssertParity<List<Address>>("[1]{City,City}:\n  a,b", lenient, expectFallback: true); // a repeated property
    }

    [Fact]
    public void DocumentsTheReaderHandsBack_MatchTheTree()
    {
        // Keys in another order than the properties, and repeated keys, take the tree-based path
        AssertParity<Address>("Zip: 1\nCity: x", expectFallback: true);
        AssertParity<Address>("City: a\nCity: b", new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } },
                              expectFallback: true);
        AssertParity<Positional>("Age: 1\nAge: 2", new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } },
                                 expectFallback: true);

        // Options with converters always do
        var withConverter = new ToonSerializerOptions();
        withConverter.AddConverter(new UpperCaseStringConverter());
        Assert.Equal("ADA", ToonSerializer.Deserialize<Address>("City: ada", withConverter)!.City);
    }

    [Theory]
    [InlineData("Id: 1\nId: 2")]
    [InlineData("Id: 99999999999")]
    [InlineData("Id:\n  x: 1")]
    [InlineData("Places[1]:\n  - City: a\n    City: b")]
    [InlineData("Places[2]{City}:\n  a")]
    [InlineData("Name: x\n  Bad: indent")]
    [InlineData("Tags[1]: \"unterminated")]
    [InlineData("Scores:\n  a: x")]
    [InlineData("Name: x\nUnknown: \"bad \\q escape\"")]
    [InlineData("When: not-a-date")]
    [InlineData("When: 2026")]
    [InlineData("Color: Purple")]
    [InlineData("Color: 1.5")]
    [InlineData("Places[1]{City{X}}:\n  a")]
    [InlineData("Places[1]{City,Zip}:\n  a")]
    [InlineData("Scores[1:]{v}:\n  a: 1")]
    [InlineData("Scores[2:]{v}:\n  a\n  b: 1")]
    public void Errors_MatchTheTreeBasedPath(string toon)
    {
        var tree = Record.Exception(() =>
            ToonSerializer.DeserializeFromValue<Combined>(ToonDocument.Parse(toon).Root));
        var fallbacks = ToonSerializer.DirectReadFallbacks;
        var direct = Record.Exception(() => ToonSerializer.Deserialize<Combined>(toon));

        Assert.NotNull(direct);
        Assert.Equal(fallbacks + 1, ToonSerializer.DirectReadFallbacks);
        Assert.Equal((tree ?? direct).GetType(), direct.GetType());
        Assert.Equal((tree ?? direct).Message, direct.Message);
    }

    /// <summary>The fields of several test models, so one document can exercise each kind of error.</summary>
    public class Combined
    {
        public int Id { get; set; }
        public DateTime When { get; set; }
        public Color Color { get; set; }
        public string? Name { get; set; }
        public List<Address> Places { get; set; } = [];
        public string[] Tags { get; set; } = [];
        public Dictionary<string, int> Scores { get; set; } = [];
    }

    private sealed class UpperCaseStringConverter : ToonConverter<string>
    {
        public override ToonValue Write(string? value, ToonSerializerOptions options) => new ToonString(value!);

        public override string Read(ToonValue value, ToonSerializerOptions options) => ((ToonString)value).Value.ToUpperInvariant();
    }
}
