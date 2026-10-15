using System.Text.Json;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Paths;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Paths;

/// <summary>Evaluating a path over text must select exactly what evaluating it on the parsed tree selects.</summary>
public class ToonPathTextTests
{
    private static void AssertSameAsTree(ToonPath path, string toon, ToonOptions? options = null)
    {
        var expected = path.Select(ToonDocument.Parse(toon, options).Root);
        var actual = path.SelectFrom(toon, options);

        Assert.True(expected.Count == actual.Count, $"{path} on\n{toon}\nexpected {expected.Count} values, got {actual.Count}");

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.True(ToonValue.DeepEquals(expected[i], actual[i]), $"{path} on\n{toon}\nvalue {i} differs");
        }

        var utf8 = path.SelectFrom(System.Text.Encoding.UTF8.GetBytes(toon), options);
        Assert.Equal(expected.Count, utf8.Count);

        var typed = ToonSerializer.DeserializeAll<ToonValue>(toon, path.Text, new ToonSerializerOptions { ToonOptions = options ?? new ToonOptions() });
        Assert.Equal(expected.Count, typed.Count);

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.True(ToonValue.DeepEquals(expected[i], typed[i]), $"{path} on\n{toon}\ntyped value {i} differs");
        }
    }

    public static TheoryData<string> ComplianceCases()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Paths", "Cts", "cts.json"));
        var data = new TheoryData<string>();

        foreach (var test in JsonDocument.Parse(stream).RootElement.GetProperty("tests").EnumerateArray())
        {
            if (test.TryGetProperty("document", out _) && !test.TryGetProperty("invalid_selector", out _))
            {
                data.Add(test.GetProperty("name").GetString()!);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ComplianceCases))]
    public void ComplianceSuite_DocumentsAsToon(string name)
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Paths", "Cts", "cts.json"));
        var test = JsonDocument.Parse(stream).RootElement.GetProperty("tests").EnumerateArray().First(t => t.GetProperty("name").GetString() == name);
        var toon = new ToonEncoder().Encode(new ToonDocument(ToonPathComplianceTests.ToToon(test.GetProperty("document"))));
        var path = ToonPath.Parse(test.GetProperty("selector").GetString()!);

        AssertSameAsTree(path, toon);
        AssertSameAsTree(path, toon, new ToonOptions { StrictMode = false });
    }

    private const string Shapes = """
                                  id: 1
                                  name: root
                                  tags[3]: a,b,c
                                  orders[4]{id,customer,total,paid}:
                                    1,Ada,250.5,true
                                    2,Bob,80,false
                                    3,Cy,120,true
                                    4,Di,5,false
                                  grouped[2]{id,ship{city,zip}}:
                                    1,Oslo,0150
                                    2,Rome,00100
                                  byId[2:]{customer,total}:
                                    a: Ada,1
                                    b: Bob,2
                                  items[3]:
                                    - id: 1
                                      parts[2]: x,y
                                      meta:
                                        id: 9
                                        deep:
                                          id: 10
                                    - [2]: 1,2
                                    - plain
                                  empty: []
                                  nested:
                                    a:
                                      b:
                                        id: 3
                                  """;

    [Theory]
    [InlineData("$")]
    [InlineData("$.*")]
    [InlineData("$..*")]
    [InlineData("$..id")]
    [InlineData("$.orders[*].customer")]
    [InlineData("$.orders[?@.total > 100]")]
    [InlineData("$.orders[?@.total > 100 && @.paid == true].id")]
    [InlineData("$.orders[?!@.paid]")]
    [InlineData("$.orders[?match(@.customer, '[AB].*')].total")]
    [InlineData("$.orders[?length(@.customer) == 2]")]
    [InlineData("$.orders[?@.missing]")]
    [InlineData("$.orders[?@]")]
    [InlineData("$.orders[?@.total > $.orders[1].total]")]
    [InlineData("$.orders[?count(@.*) == 4]")]
    [InlineData("$.orders[1:3]")]
    [InlineData("$.orders[::2].id")]
    [InlineData("$.orders[::-1].id")]
    [InlineData("$.orders[-1]")]
    [InlineData("$.orders[-2:]")]
    [InlineData("$.orders[0,2].id")]
    [InlineData("$.orders[2,0].id")]
    [InlineData("$.grouped[?@.ship.city == 'Rome'].id")]
    [InlineData("$.grouped[*].ship.zip")]
    [InlineData("$.byId.*")]
    [InlineData("$.byId[?@.total == 2]")]
    [InlineData("$.items[?@.meta.deep.id == 10].parts[1]")]
    [InlineData("$.items[?@.parts]")]
    [InlineData("$.items[1][1]")]
    [InlineData("$.items[*][0]")]
    [InlineData("$.items[0]..id")]
    [InlineData("$[?@ == 1]")]
    [InlineData("$[?@.b]")]
    [InlineData("$.nested[?@.b.id == 3]")]
    [InlineData("$.empty[*]")]
    [InlineData("$.tags[?@ != 'b']")]
    [InlineData("$.name[0]")]
    [InlineData("$.name.x")]
    [InlineData("$['tags','name']")]
    public void Shapes_MatchTheTree(string path)
    {
        AssertSameAsTree(ToonPath.Parse(path), Shapes);
        AssertSameAsTree(ToonPath.Parse(path), Shapes, new ToonOptions { StrictMode = false });
    }

    /// <summary>Generic paths over every valid document of the spec's decode fixtures (all the forms of TOON).</summary>
    [Fact]
    public void SpecFixtures_MatchTheTree()
    {
        string[] paths = ["$", "$.*", "$[*]", "$..*", "$.*.*", "$.*[*]", "$[0]", "$[-1]", "$.*[-1]", "$[1:3]", "$.*[::2]", "$.*[::-1]", "$..[0]",
                          "$.*[?@]", "$..[?@.id]", "$.*[?@.a == 1 || @.b]", "$.*[?@.id > 1]", "$[?@ == 1]", "$.*.*.*", "$..[?@.name]"];
        var root = Path.Combine(AppContext.BaseDirectory, "SpecCompliance", "Fixtures", "v4.4", "decode");
        var documents = 0;

        foreach (var file in Directory.GetFiles(root, "*.json"))
        {
            foreach (var test in JsonDocument.Parse(File.ReadAllText(file)).RootElement.GetProperty("tests").EnumerateArray())
            {
                var input = test.GetProperty("input").GetString()!;
                var options = new ToonOptions();

                if (test.TryGetProperty("options", out var o))
                {
                    if (o.TryGetProperty("strict", out var strict)) options.StrictMode = strict.GetBoolean();
                    if (o.TryGetProperty("indentSize", out var indent)) options.IndentSize = indent.GetInt32();
                }

                if (Record.Exception(() => ToonDocument.Parse(input, options)) != null)
                {
                    continue;
                }

                documents++;

                foreach (var path in paths)
                {
                    AssertSameAsTree(ToonPath.Parse(path), input, options);
                }
            }
        }

        Assert.True(documents > 200, $"only {documents} documents");
    }

    /// <summary>
    ///     Random rows and filters that compare members with literals (tested on tokens, without building values) must
    ///     select what the tree-based evaluation selects.
    /// </summary>
    [Fact]
    public void TokenFilters_MatchTheTree_Fuzz()
    {
        var random = new Random(20261014);
        string[] numbers = ["0", "-0", "1", "-1", "1.5", "1.50", "-2.25", "100", "99.999", "1e2", "1E-2", "123456789012345", "1234567890123456",
                            "0.1", "0.30000000000000004", "79228162514264337593543950335", "792281625142643375935439503351", "1e400"];
        string[] strings = ["a", "b", "ab", "\"a\"", "\"x\\ty\"", "\"\"", "\"1\"", "\"true\"", "é", "\"\uD83D\uDE00\"", "05", "-"];
        string[] others = ["true", "false", "null"];
        string[] literals = ["1", "1.5", "-1", "100", "1e2", "0.1", "79228162514264337593543950335", "1e400", "'a'", "'ab'", "'1'", "''", @"'x\ty'",
                             "true", "false", "null", "'é'"];
        string[] ops = ["==", "!=", "<", "<=", ">", ">="];

        string Cell() => random.Next(3) switch
        {
            0 => numbers[random.Next(numbers.Length)],
            1 => strings[random.Next(strings.Length)],
            _ => others[random.Next(others.Length)]
        };

        for (var round = 0; round < 200; round++)
        {
            var rows = string.Join("\n", Enumerable.Range(0, 20).Select(_ => "  " + Cell() + "," + Cell()));
            var toon = "rows[20]{a,b}:\n" + rows + "\nlist[3]:\n  - a: 1\n    b[1]: x\n  - c: 2\n  - a[2]: a,b\n    c: a";

            for (var f = 0; f < 10; f++)
            {
                var member = random.Next(3) switch { 0 => "@.a", 1 => "@.b", _ => "@.c" };
                var comparison = random.Next(2) == 0
                    ? $"{member} {ops[random.Next(ops.Length)]} {literals[random.Next(literals.Length)]}"
                    : $"{literals[random.Next(literals.Length)]} {ops[random.Next(ops.Length)]} {member}";
                string[] functions = ["starts_with", "ends_with", "contains", "equals_ignore_case"];
                string[] texts = ["'a'", "''", "'b'", "'A'", "'\"'", "'x'", "'é'", "'1'", "'tru'"];
                var test = $"{functions[random.Next(functions.Length)]}({member}, {texts[random.Next(texts.Length)]})";
                var filter = random.Next(6) switch
                {
                    0 => comparison,
                    1 => $"!({comparison})",
                    2 => $"{comparison} && @.a",
                    3 => $"{comparison} || !@.b",
                    4 => test,
                    _ => $"!{test} && {comparison}"
                };

                AssertSameAsTree(ToonPath.Parse($"$.rows[?{filter}]"), toon);
                AssertSameAsTree(ToonPath.Parse($"$.list[?{filter}]"), toon);
            }
        }
    }

    [Fact]
    public void ShortNumbers_AreCorrectlyRounded()
    {
        var random = new Random(7);

        for (var i = 0; i < 100_000; i++)
        {
            var digits = random.Next(1, 16);
            var number = random.NextInt64(1, 999_999_999_999_999).ToString();
            var text = (random.Next(2) == 0 ? "-" : "") + number[..Math.Min(digits, number.Length)];
            var point = random.Next(text.Length + 1);

            if (point > (text[0] == '-' ? 1 : 0) && point < text.Length)
            {
                text = text[..point] + "." + text[point..];
            }

            if (ToonPathTokenFilter.TryParseShortNumber(text, out var value))
            {
                Assert.Equal(double.Parse(text, System.Globalization.CultureInfo.InvariantCulture), value);
            }
        }

        Assert.False(ToonPathTokenFilter.TryParseShortNumber("05", out _));
        Assert.False(ToonPathTokenFilter.TryParseShortNumber("1.", out _));
        Assert.False(ToonPathTokenFilter.TryParseShortNumber(".5", out _));
        Assert.False(ToonPathTokenFilter.TryParseShortNumber("1e2", out _));
        Assert.False(ToonPathTokenFilter.TryParseShortNumber("-", out _));
        Assert.False(ToonPathTokenFilter.TryParseShortNumber("1234567890123456", out _));
        Assert.True(ToonPathTokenFilter.TryParseShortNumber("0.5", out var half) && half == 0.5);
    }

    [Fact]
    public void NonStrictMode_RepeatedKeys_TakeTheLast()
    {
        const string toon = "a: 1\na: 2";
        var options = new ToonOptions { StrictMode = false };

        Assert.Equal(2, Assert.Single(ToonPath.Parse("$.a").SelectFrom(toon, options)).GetValue<int>());
        Assert.Throws<ToonParseException>(() => ToonPath.Parse("$.a").SelectFrom(toon));
    }

    [Fact]
    public void TheWholeDocument_IsChecked()
    {
        Assert.Throws<ToonParseException>(() => ToonPath.Parse("$.a").SelectFrom("a: 1\nb[2]: x"));
        Assert.Throws<ToonParseException>(() => ToonPath.Parse("$.a").SelectFrom("a: 1\n  b: 2"));
    }

    public sealed class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public decimal Total { get; set; }
        public bool Paid { get; set; }
    }

    [Fact]
    public void DeserializeAll_ReadsTheSelectedRows()
    {
        var options = new ToonSerializerOptions { PropertyNameCaseInsensitive = true };
        var orders = ToonSerializer.DeserializeAll<Order>(Shapes, "$.orders[?@.total >= 100]", options);

        Assert.Equal(["Ada", "Cy"], orders.Select(o => o!.Customer));
        Assert.Equal([250.5m, 80m, 120m, 5m], ToonSerializer.DeserializeAll<decimal>(Shapes, "$.orders[*].total"));
        Assert.Equal([4, 3, 2, 1], ToonSerializer.DeserializeAll<int>(Shapes, "$.orders[::-1].id"));
        Assert.Empty(ToonSerializer.DeserializeAll<Order>(Shapes, "$.missing[*]"));
    }

    [Fact]
    public async Task DeserializeAll_Overloads()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(Shapes);

        Assert.Equal(["a", "c"], ToonSerializer.DeserializeAll<string>(utf8, "$.tags[?@ != 'b']"));
        Assert.Equal(["a", "c"], await ToonSerializer.DeserializeAllAsync<string>(new MemoryStream(utf8), "$.tags[?@ != 'b']"));
    }

    [Fact]
    public void DeserializeAll_Errors_HaveThePathInTheDocument()
    {
        const string toon = "data:\n  orders[3]{id,total}:\n    1,5\n    x,200\n    3,300";
        var options = new ToonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Over text, and on a tree (a segment with several selectors)
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeAll<Order>(toon, "$.data.orders[?@.total > 100]", options));
        Assert.Equal("$.data.orders[1].Id", ex.Path);

        ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeAll<Order>(toon, "$.data.orders[2,1]", options));
        Assert.Equal("$.data.orders[1].Id", ex.Path);

        ex = Assert.Throws<ToonSerializationException>(
            () => ToonSerializer.DeserializeAll<Order>(toon, "$.data.orders[?@.total > 100]",
                                                       new ToonSerializerOptions { PropertyNameCaseInsensitive = true, ToonOptions = new ToonOptions { StrictMode = false } }));
        Assert.Equal("$.data.orders[1].Id", ex.Path);
    }
}
