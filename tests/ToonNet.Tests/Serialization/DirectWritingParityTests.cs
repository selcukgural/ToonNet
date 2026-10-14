using System.Collections;
using System.Numerics;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     <see cref="ToonSerializer.Serialize(object, Type, ToonSerializerOptions)"/> writes objects straight to text. These
///     tests compare it with encoding the tree from <see cref="ToonSerializer.SerializeToValue(object, Type, ToonSerializerOptions)"/>
///     for many shapes, and check that supported shapes never fall back to the tree-based path.
/// </summary>
public class DirectWritingParityTests
{
    private static string AssertParity<T>(T value, ToonSerializerOptions? options = null)
    {
        options ??= new ToonSerializerOptions();
        var fallbacks = ToonSerializer.DirectWriteFallbacks;

        var direct = ToonSerializer.Serialize(value, typeof(T), options);

        Assert.Equal(fallbacks, ToonSerializer.DirectWriteFallbacks);
        var tree = new ToonEncoder(options.ToonOptions).Encode(new ToonDocument(ToonSerializer.SerializeToValue(value, typeof(T), options)));
        Assert.Equal(tree, direct);
        return direct;
    }

    public interface IAnimal
    {
        string Name { get; }
    }

    public sealed class Dog : IAnimal
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public sealed class Zoo
    {
        public IAnimal? A { get; set; }
        public IAnimal? B { get; set; }
    }

    public sealed class Owner
    {
        public string Name { get; set; } = "";
        public IAnimal? Pet { get; set; }
    }

    /// <summary>Applies to the declared interface only, not to the runtime type.</summary>
    private sealed class AnimalConverter : ToonConverter<IAnimal>
    {
        public override bool CanConvert(Type type) => type == typeof(IAnimal);

        public override ToonValue Write(IAnimal? value, ToonSerializerOptions options) => new ToonString("animal:" + value!.Name);

        public override IAnimal Read(ToonValue value, ToonSerializerOptions options) => new Dog { Name = ((ToonString)value).Value[7..] };
    }

    [Fact]
    public void ConvertersForADeclaredType_ApplyInTablesAndKeyedObjects()
    {
        // Without the converter these would be a keyed table and a table with a nested field group
        var options = new ToonSerializerOptions();
        options.AddConverter(new AnimalConverter());

        var zoo = AssertParity(new Zoo { A = new Dog { Name = "Rex", Age = 3 }, B = new Dog { Name = "Max", Age = 5 } }, options);
        Assert.Contains("animal:Rex", zoo);

        var owners = AssertParity(new List<Owner> { new() { Name = "a", Pet = new Dog { Name = "Rex" } }, new() { Name = "b", Pet = new Dog { Name = "Max" } } },
                                  options);
        Assert.Contains("animal:Max", owners);

        // Dictionary values are written by their runtime type, in both paths
        AssertParity(new Dictionary<string, IAnimal> { ["x"] = new Dog { Name = "Rex" }, ["y"] = new Dog { Name = "Max" } }, options);
    }

    private static IEnumerable<ToonSerializerOptions> OptionVariants()
    {
        yield return new ToonSerializerOptions();
        yield return new ToonSerializerOptions { IgnoreNullValues = true };
        yield return new ToonSerializerOptions { IncludeReadOnlyProperties = false };
        yield return new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase };
        yield return new ToonSerializerOptions { ToonOptions = new ToonOptions { Delimiter = '|', IndentSize = 4 } };
        yield return new ToonSerializerOptions { ToonOptions = new ToonOptions { Delimiter = '\t' } };
    }

    #region Models

    public enum Color
    {
        Red,
        Green
    }

    public class Row
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double? Score { get; set; }
        public Color Color { get; set; }
        public DateTime When { get; set; }
        public object? Extra { get; set; }
        public int ReadOnly => Id * 2;
    }

    public class Address
    {
        public string City { get; set; } = "";
        public string? Zip { get; set; }
    }

    public class Holder
    {
        public int Id { get; set; }
        public Address? Home { get; set; }
    }

    /// <summary>Only object properties: a keyed tabular object when both are set (spec 4.4 §9.5).</summary>
    public class Pair
    {
        public Address? A { get; set; }
        public Address? B { get; set; }
    }

    public class Person
    {
        public string Name { get; set; } = "";
        public Address? Home { get; set; }
        public List<Address> Places { get; set; } = [];
        public List<int?> Numbers { get; set; } = [];
        public string[] Tags { get; set; } = [];
        public Dictionary<string, int> Scores { get; set; } = [];
        public Dictionary<int, Address> ById { get; set; } = [];
        public List<List<int>> Matrix { get; set; } = [];
        public List<object?> Mixed { get; set; } = [];
        public object? Anything { get; set; }
        public Empty Nothing { get; set; } = new();
        public List<Empty> Empties { get; set; } = [];
    }

    public class Empty;

    public class Other
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class SameShapeA
    {
        public int Id { get; set; }
    }

    public class SameShapeB
    {
        public int Id { get; set; }
    }

    public class Renamed
    {
        [ToonProperty("x")]
        public int A { get; set; }

        [ToonProperty("x")]
        public int B { get; set; }
    }

    public class Base
    {
        [ToonPropertyOrder(-1)]
        public int First { get; set; }

        public string BaseName { get; set; } = "b";
    }

    public class Derived : Base
    {
        public string DerivedName { get; set; } = "d";

        [ToonIgnore]
        public string Hidden { get; set; } = "h";
    }

    public class WithConverter
    {
        [ToonConverter(typeof(UpperConverter))]
        public string Name { get; set; } = "a";

        public int Id { get; set; }
    }

    public class WithToonValues
    {
        public ToonValue Raw { get; set; } = new ToonObject { ["k"] = 1 };
        public ToonArray List { get; set; } = new([new ToonString("a"), new ToonNumber(1)]);
        public ToonValue Text { get; set; } = new ToonString("t");
    }

    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public record Positional(string Name, int Age, List<string> Tags);

    public class Primitives
    {
        public char C { get; set; } = 'x';
        public float F { get; set; } = 0.1f;
        public float FNaN { get; set; } = float.NaN;
        public double DInf { get; set; } = double.PositiveInfinity;
        public decimal M { get; set; } = 1.50m;
        public Half H { get; set; } = (Half)1.5;
        public BigInteger Big { get; set; } = BigInteger.Pow(10, 40);
        public Int128 Wide { get; set; } = Int128.MaxValue;
        public DateOnly Day { get; set; } = new(2026, 10, 13);
        public TimeOnly Time { get; set; } = new(1, 2, 3, 4);
        public TimeSpan Span { get; set; } = TimeSpan.FromHours(30);
        public DateTimeOffset Offset { get; set; } = new(2026, 10, 13, 1, 2, 3, TimeSpan.FromHours(3));
        public Guid Id { get; set; } = Guid.Parse("c56a4180-65aa-42ec-a945-5fd21dec0538");
        public Uri Link { get; set; } = new("https://example.com/a b");
        public ulong Big64 { get; set; } = ulong.MaxValue;
        public sbyte Small { get; set; } = -1;
        public Color? MaybeColor { get; set; } = Color.Green;
        public string[] Texts { get; set; } = ["", " a", "true", "12", "-x", "a:b", "a|b", "a\tb", "line\nbreak", "\u0001", "ünï", "[x]"];
    }

    private sealed class UpperConverter : ToonConverter<string>
    {
        public override ToonValue Write(string? value, ToonSerializerOptions options) => new ToonString(value!.ToUpperInvariant());

        public override string Read(ToonValue value, ToonSerializerOptions options) => ((ToonString)value).Value;
    }

    private sealed class NullConverter : ToonConverter<Address>
    {
        public override ToonValue? Write(Address? value, ToonSerializerOptions options) => null;

        public override Address Read(ToonValue value, ToonSerializerOptions options) => new();
    }

    private static List<Row> Rows(bool withNulls = false) =>
    [
        new() { Id = 1, Name = "a", Score = 1.5, Color = Color.Red, When = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Extra = 5 },
        new() { Id = 2, Name = withNulls ? null : "b", Score = withNulls ? null : 2, Color = Color.Green, Extra = "x" }
    ];

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
        Mixed = [1, "two", new Address { City = "D" }, null, new List<int> { 4 }],
        Anything = new Other { Id = 9 },
        Empties = [new Empty(), new Empty()]
    };

    #endregion

    [Fact]
    public void Objects_Lists_AndDictionaries_MatchTheTree()
    {
        foreach (var options in OptionVariants())
        {
            AssertParity(CreatePerson(), options);
            AssertParity(new Person(), options);
            AssertParity(new Derived(), options);
            AssertParity(new Primitives(), options);
            AssertParity(new Point { X = 1, Y = 2 }, options);
            AssertParity(new Positional("p", 3, ["a"]), options);
        }
    }

    [Fact]
    public void TabularDecisions_MatchTheTree()
    {
        foreach (var options in OptionVariants())
        {
            var tabular = AssertParity(Rows(), options);
            AssertParity(Rows(withNulls: true), options);
            AssertParity(new { Items = Rows() }, options);
            AssertParity(new List<Row> { new() { Extra = new Address() } }, options); // an object value: list items
            AssertParity(new List<Row> { new() { Extra = null } }, options);
            AssertParity(new List<object> { new Row(), 1 }, options);
            AssertParity(new List<List<Row>> { Rows() }, options); // rows inside a list item are never tabular
            AssertParity(new List<Address> { new() { City = "a" }, new() { City = "b", Zip = "z" } }, options);

            if (options.ToonOptions.Delimiter == ',' && !options.IgnoreNullValues && options.IncludeReadOnlyProperties &&
                options.PropertyNamingPolicy == PropertyNamingPolicy.Default)
            {
                Assert.StartsWith("[2]{Id,Name,Score,Color,When,Extra,ReadOnly}:", tabular);
            }
        }
    }

    [Fact]
    public void NestedFieldGroupsAndKeyedObjects_MatchTheTree()
    {
        static Address At(string city, string? zip = null) => new() { City = city, Zip = zip };

        foreach (var options in OptionVariants())
        {
            // Nested field groups (§9.3)
            AssertParity(new List<Holder> { new() { Id = 1, Home = At("a", "1") }, new() { Id = 2, Home = At("b", "2") } }, options);
            AssertParity(new List<Holder> { new() { Id = 1, Home = At("a") }, new() { Id = 2, Home = At("b", "2") } }, options);
            AssertParity(new List<Holder> { new() { Id = 1, Home = At("a") }, new() { Id = 2 } }, options);
            AssertParity(new { Items = new List<Pair> { new() { A = At("a"), B = At("b") } } }, options);

            // Keyed tabular objects (§9.5): dictionaries and objects whose values are uniform objects
            AssertParity(new Dictionary<string, Address> { ["x"] = At("a", "1"), ["needs quote"] = At("b", "2") }, options);
            AssertParity(new Dictionary<string, Address> { ["x"] = At("a") }, options);
            AssertParity(new Dictionary<string, Address> { ["x"] = At("a"), ["y"] = At("b", "2") }, options);
            AssertParity(new Dictionary<int, Pair> { [1] = new() { A = At("a"), B = At("b") }, [2] = new() { A = At("c"), B = At("d") } }, options);
            AssertParity(new Pair { A = At("a"), B = At("b") }, options);
            AssertParity(new Pair { A = At("a") }, options);
            AssertParity(new { Map = new Dictionary<string, Dictionary<string, int>> { ["a"] = new() { ["x"] = 1 }, ["b"] = new() { ["x"] = 2 } } }, options);
            AssertParity(new List<object> { new { First = new Pair { A = At("a"), B = At("b") }, N = 1 } }, options);
            AssertParity(new Dictionary<string, object?> { ["a"] = At("x"), ["b"] = new Dictionary<string, string?> { ["City"] = "y", ["Zip"] = null } },
                         options);
        }

        // Objects of one type are planned and written straight to text, not through the tree
        var treeShapes = ToonSerializer.DirectWriteTreeShapes;
        Assert.StartsWith("[2]{Id,Home{City,Zip}}:", AssertParity(new List<Holder> { new() { Id = 1, Home = At("a") }, new() { Id = 2, Home = At("b") } }));
        Assert.StartsWith("[2:]{City,Zip}:", AssertParity(new Dictionary<string, Address> { ["x"] = At("a"), ["y"] = At("b") }));
        Assert.StartsWith("[2:]{City,Zip}:", AssertParity(new Pair { A = At("a"), B = At("b") }));
        Assert.Contains("Pairs[2:]{A{City,Zip},B{City,Zip}}:", AssertParity(new
        {
            Pairs = new Dictionary<int, Pair> { [1] = new() { A = At("a"), B = At("b") }, [2] = new() { A = At("c"), B = At("d") } }
        }));
        Assert.Equal(treeShapes, ToonSerializer.DirectWriteTreeShapes);
    }

    public sealed class Scored
    {
        public int Id { get; set; }
        public Dictionary<string, int?>? Scores { get; set; }
    }

    [Fact]
    public void DictionariesAsNestedFieldGroups_MatchTheTree()
    {
        static Dictionary<string, int?> Scores(params (string Key, int? Value)[] entries) => entries.ToDictionary(e => e.Key, e => e.Value);
        static List<Scored> Rows(params Dictionary<string, int?>?[] scores) => [.. scores.Select((s, i) => new Scored { Id = i, Scores = s })];

        foreach (var options in OptionVariants())
        {
            AssertParity(Rows(Scores(("math", 1), ("art", 2)), Scores(("math", 3), ("art", 4))), options);
            AssertParity(Rows(Scores(("math", 1), ("art", 2)), Scores(("art", 4), ("math", 3))), options); // key order differs
            AssertParity(Rows(Scores(("math", 1)), Scores(("art", 4))), options);                           // key sets differ
            AssertParity(Rows(Scores(("math", 1), ("art", 2)), Scores(("math", 3))), options);
            AssertParity(Rows(Scores(("math", 1), ("art", null)), Scores(("math", 3), ("art", 4))), options);
            AssertParity(Rows(Scores(("math", 1), ("art", null)), Scores(("math", 3), ("art", null))), options);
            AssertParity(Rows(Scores(("math", null)), Scores(("math", null))), options);
            AssertParity(Rows(Scores(), Scores()), options);
            AssertParity(Rows(Scores(("math", 1)), null), options);
            AssertParity(Rows(Scores(("needs quote", 1), ("a:b", 2), ("", 3)), Scores(("needs quote", 4), ("a:b", 5), ("", 6))), options);

            // Rows of dictionaries, nested dictionaries, other key and value types
            AssertParity(new List<Dictionary<string, int>> { new() { ["x"] = 1, ["y"] = 2 }, new() { ["y"] = 3, ["x"] = 4 } }, options);
            AssertParity(new List<Dictionary<string, Dictionary<string, int>>>
            {
                new() { ["a"] = new() { ["x"] = 1 }, ["b"] = new() { ["x"] = 2, ["y"] = 3 } },
                new() { ["a"] = new() { ["x"] = 4 }, ["b"] = new() { ["y"] = 5, ["x"] = 6 } }
            }, options);
            AssertParity(new List<object> { new { Id = 1, ById = new Dictionary<int, string> { [7] = "a", [8] = "b" } }, new { Id = 2, ById = new Dictionary<int, string> { [8] = "c", [7] = "d" } } },
                         options);
            AssertParity(new List<object> { new { Map = new Dictionary<Color, Address> { [Color.Red] = new() { City = "a" } } }, new { Map = new Dictionary<Color, Address> { [Color.Red] = new() { City = "b" } } } },
                         options);
            AssertParity(new List<object> { new { Map = new Dictionary<Guid, decimal> { [Guid.Empty] = 1.5m } }, new { Map = new Dictionary<Guid, decimal> { [Guid.Empty] = 2m } } },
                         options);
            AssertParity(new List<object> { new { Map = new Dictionary<string, List<int>> { ["a"] = [1] } }, new { Map = new Dictionary<string, List<int>> { ["a"] = [2] } } },
                         options);
            AssertParity(new List<object> { new { Map = new Dictionary<string, object?> { ["a"] = 1, ["b"] = "x" } }, new { Map = new Dictionary<string, object?> { ["a"] = new Address { City = "c" }, ["b"] = "y" } } },
                         options);

            // Keyed tabular objects whose values are dictionaries
            AssertParity(new Dictionary<string, Dictionary<string, int>> { ["p"] = new() { ["x"] = 1, ["y"] = 2 }, ["q"] = new() { ["y"] = 3, ["x"] = 4 } }, options);
            AssertParity(new { Map = new Dictionary<string, Dictionary<string, int>> { ["p"] = new() { ["x"] = 1 }, ["q"] = new() { ["z"] = 2 } } }, options);

            // With a custom comparer too, the key names decide, as in the tree
            var ignoringCase = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase) { ["MATH"] = 3 };
            AssertParity(Rows(Scores(("math", 1)), ignoringCase), options);
        }

        // Shapes of one dictionary type are planned and written straight to text, not through the tree
        var treeShapes = ToonSerializer.DirectWriteTreeShapes;
        Assert.StartsWith("[2]{Id,Scores{math,art}}:", AssertParity(Rows(Scores(("math", 1), ("art", 2)), Scores(("art", 4), ("math", 3)))));
        Assert.StartsWith("[2]{x,y}:", AssertParity(new List<Dictionary<string, int>> { new() { ["x"] = 1, ["y"] = 2 }, new() { ["y"] = 3, ["x"] = 4 } }));
        Assert.StartsWith("[2:]{x,y}:", AssertParity(new Dictionary<string, Dictionary<string, int>> { ["p"] = new() { ["x"] = 1, ["y"] = 2 }, ["q"] = new() { ["y"] = 3, ["x"] = 4 } }));
        Assert.Equal(treeShapes, ToonSerializer.DirectWriteTreeShapes);
    }

    [Fact]
    public void RootValues_MatchTheTree()
    {
        AssertParity<object?>(null);
        AssertParity(42);
        AssertParity("text: with colon");
        AssertParity(new List<int>());
        AssertParity(new Dictionary<string, object?> { ["a"] = 1, ["b"] = null, ["c"] = new List<int> { 1 } });
        AssertParity(new Dictionary<string, object?> { ["b"] = null }, new ToonSerializerOptions { IgnoreNullValues = true });
        AssertParity(new Empty());
        AssertParity(Enumerable.Range(0, 3).Select(i => new Address { City = $"c{i}" })); // enumerated once
        AssertParity(new[] { new int[0], new[] { 1 } });
        AssertParity(new List<object?> { null, null });
    }

    [Fact]
    public void ValuesTheWriterHandsToTheTree_MatchTheTree()
    {
        AssertParity(new WithConverter());
        AssertParity(new List<WithConverter> { new(), new() });
        AssertParity(new WithToonValues());
        AssertParity(new List<object> { new SameShapeA { Id = 1 }, new SameShapeB { Id = 2 } }); // tabular across types
        AssertParity(new List<object> { new Dictionary<string, int> { ["Id"] = 1 }, new SameShapeA { Id = 2 } });
        AssertParity(new Renamed { A = 1, B = 2 });
        AssertParity(new Hashtable { ["k"] = 1 });
        // Keys that are written alike would lose a value: an error on both paths
        var alike = new Dictionary<object, int> { [1] = 1, ["1"] = 2 };
        Assert.Throws<ToonEncodingException>(() => ToonSerializer.Serialize(alike));
        Assert.Throws<ToonEncodingException>(() => ToonSerializer.SerializeToValue(alike));
        AssertParity(new List<ToonValue> { new ToonString("a"), new ToonObject() });

        var withConverters = new ToonSerializerOptions();
        withConverters.AddConverter(new UpperConverter());
        AssertParity(CreatePerson(), withConverters);
        AssertParity(Rows(), withConverters);

        var dropping = new ToonSerializerOptions();
        dropping.AddConverter(new NullConverter());
        AssertParity(CreatePerson(), dropping);
    }

    [Fact]
    public void Errors_MatchTheTreeBasedPath()
    {
        var cycle = new Node();
        cycle.Next = cycle;
        AssertSameError(cycle, new ToonSerializerOptions());

        var deep = new Node();

        for (var i = 0; i < 20; i++)
        {
            deep = new Node { Next = deep };
        }

        AssertSameError(deep, new ToonSerializerOptions { MaxDepth = 5 });
        AssertSameError(deep, new ToonSerializerOptions { ToonOptions = new ToonOptions { MaxDepth = 5 } });
        AssertSameError(new { List = new List<int> { 1 } }, new ToonSerializerOptions { MaxDepth = 300 });

        static void AssertSameError(object value, ToonSerializerOptions options)
        {
            var tree = Record.Exception(() => new ToonEncoder(options.ToonOptions).Encode(
                                            new ToonDocument(ToonSerializer.SerializeToValue(value, value.GetType(), options))));
            var fallbacks = ToonSerializer.DirectWriteFallbacks;
            var direct = Record.Exception(() => ToonSerializer.Serialize(value, value.GetType(), options));

            // The error comes from the tree-based path, which runs after direct writing gives up
            Assert.Equal(fallbacks + 1, ToonSerializer.DirectWriteFallbacks);
            Assert.NotNull(tree);
            Assert.Equal(tree.GetType(), direct?.GetType());
            Assert.Equal(tree.Message, direct!.Message);
        }
    }

    public class Node
    {
        public Node? Next { get; set; }
    }
}
