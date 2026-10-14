using System.Diagnostics;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Paths;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Paths;

public class ToonPathTests
{
    private const string Orders = """
                                  orders[3]{id,customer,total,paid}:
                                    1,Ada,250.5,true
                                    2,Bob,80,false
                                    3,Cy,120,true
                                  meta:
                                    region: eu
                                    tags[2]: new,vip
                                  """;

    private static ToonValue Root() => ToonDocument.Parse(Orders).Root;

    [Fact]
    public void Select_Filters_Rows()
    {
        var ids = Root().Select("$.orders[?@.total > 100 && @.paid == true].id");

        Assert.Equal([1, 3], ids.Select(v => v.GetValue<int>()));
    }

    [Fact]
    public void Select_FunctionsAndDescendants()
    {
        var root = Root();

        Assert.Equal(["Ada"], root.Select("$.orders[?match(@.customer, 'A.*')].customer").Select(v => v.GetValue<string>()));
        Assert.Equal(["eu", "new", "vip"], root.Select("$.meta..*").Where(v => v is ToonString).Select(v => v.GetValue<string>()));
        Assert.Single(root.Select("$[?length(@) == 2]"));
        Assert.Equal(3, root.Select("$..customer").Count);
        Assert.Empty(root.Select("$.nothing.here"));
    }

    [Fact]
    public void SelectMatches_GivesNormalizedPaths()
    {
        var matches = Root().SelectMatches("$.orders[?@.paid == false]");

        Assert.Equal("$['orders'][1]", Assert.Single(matches).Path);

        var path = ToonPath.Parse(@"$..['it\'s']");
        var weird = ToonDocument.Parse("\"it's\": 1").Root;
        Assert.Equal(@"$['it\'s']", Assert.Single(path.SelectMatches(weird)).Path);
    }

    [Fact]
    public void SelectSingle()
    {
        var root = Root();

        Assert.Equal("eu", root.SelectSingle("$.meta.region")!.GetValue<string>());
        Assert.Null(root.SelectSingle("$.meta.missing"));
        var ex = Assert.Throws<InvalidOperationException>(() => root.SelectSingle("$.orders[*]"));
        Assert.Equal("The path '$.orders[*]' selects 3 values, not one.", ex.Message);
    }

    [Fact]
    public void SetValue_ReplacesAndCreates()
    {
        var root = Root();

        root.SetValue("$.meta.region", "us");
        root.SetValue("$.orders[-1].total", 99);
        root.SetValue("$.meta.tags[2]", "late");
        root.SetValue("$.audit.by", "selcuk");
        root.SetValue("$.audit.history[0].at", 1);
        root.SetValue("$.meta.note", null);

        Assert.Equal("us", root["meta"]!["region"]!.GetValue<string>());
        Assert.Equal(99, root["orders"]![2]["total"]!.GetValue<int>());
        Assert.Equal(["new", "vip", "late"], root["meta"]!["tags"]!.Deserialize<string[]>()!);
        Assert.Equal("selcuk", root["audit"]!["by"]!.GetValue<string>());
        Assert.Equal(1, root["audit"]!["history"]![0]["at"]!.GetValue<int>());
        Assert.Same(ToonNull.Instance, root["meta"]!["note"]);
    }

    [Fact]
    public void SetValue_Errors()
    {
        var root = Root();

        Assert.Equal("The path '$.orders[*].id' can select several values; setting needs member names and indexes only.",
                     Assert.Throws<InvalidOperationException>(() => root.SetValue("$.orders[*].id", 1)).Message);
        Assert.Equal("The root cannot be replaced; set a path below it.", Assert.Throws<InvalidOperationException>(() => root.SetValue("$", 1)).Message);
        Assert.Equal("The index 5 of the path '$.orders[5]' is outside the array of 3 items.",
                     Assert.Throws<InvalidOperationException>(() => root.SetValue("$.orders[5]", 1)).Message);
        Assert.Throws<InvalidOperationException>(() => root.SetValue("$.orders[-4]", 1));
        Assert.Equal("The value at segment 3 of the path '$.meta.region.x' is a string, not an object.",
                     Assert.Throws<InvalidOperationException>(() => root.SetValue("$.meta.region.x", 1)).Message);
        Assert.Equal("The value at segment 2 of the path '$.meta[0]' is an object, not an array.",
                     Assert.Throws<InvalidOperationException>(() => root.SetValue("$.meta[0]", 1)).Message);
    }

    [Fact]
    public void RemoveAll_RemovesItemsAndMembers()
    {
        var root = Root();

        Assert.Equal(2, root.RemoveAll("$.orders[?@.paid == true]"));
        Assert.Equal([2], root.Select("$.orders[*].id").Select(v => v.GetValue<int>()));
        Assert.Equal(2, root.RemoveAll("$.meta.tags[0,1,0]"));
        Assert.Empty(root["meta"]!["tags"]!.AsArray().Items);
        Assert.Equal(1, root.RemoveAll("$..region"));
        Assert.Equal(0, root.RemoveAll("$.missing"));
        Assert.Equal("orders[1]{id,customer,total,paid}:\n  2,Bob,80,false\nmeta:\n  tags: []", ToonSerializer.Serialize(root));
        Assert.Equal("The root cannot be removed.", Assert.Throws<InvalidOperationException>(() => root.RemoveAll("$")).Message);
    }

    [Fact]
    public void Parse_ReportsThePosition()
    {
        var ex = Assert.Throws<ToonPathException>(() => ToonPath.Parse("$.orders[?@.total >]"));

        Assert.Equal(19, ex.Position);
        Assert.Equal("$.orders[?@.total >]", ex.Path);
        Assert.Equal("Expected a query, a literal or a function", ex.Reason);
        Assert.Equal("Expected a query, a literal or a function (position: 19, path: $.orders[?@.total >])", ex.Message);

        Assert.Equal("A path starts with '$'", Assert.Throws<ToonPathException>(() => ToonPath.Parse("orders")).Reason);
        Assert.Equal("Unknown function 'size'", Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?size(@) > 1]")).Reason);
        Assert.Equal("Only a singular query (names and indexes only) can be compared",
                     Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?@.*.a == 1]")).Reason);
        Assert.Throws<ArgumentNullException>(() => ToonPath.Parse(null!));
        Assert.False(ToonPath.TryParse(null, out _));
        Assert.True(ToonPath.TryParse("$.a", out var path));
        Assert.Equal("$.a", path.ToString());
    }

    [Fact]
    public void Parse_CachesCompiledPaths()
    {
        Assert.Same(ToonPath.Parse("$.cached[?@.x]"), ToonPath.Parse("$.cached[?@.x]"));
        Assert.True(ToonPath.Parse("$.a['b'][0]").IsSingular);
        Assert.False(ToonPath.Parse("$.a[0,1]").IsSingular);
        Assert.False(ToonPath.Parse("$..a").IsSingular);
    }

    [Fact]
    public void Parse_LimitsNesting()
    {
        var deep = "$[?" + new string('(', 100) + "@" + new string(')', 100) + "]";

        Assert.Equal($"The path nests more than {ToonPathParser.MaxNesting} levels", Assert.Throws<ToonPathException>(() => ToonPath.Parse(deep)).Reason);

        var filters = "$" + string.Concat(Enumerable.Repeat("[?@", 100)) + new string(']', 100);
        Assert.Throws<ToonPathException>(() => ToonPath.Parse(filters));

        var fine = "$[?" + new string('(', 30) + "@" + new string(')', 30) + "]";
        Assert.NotNull(ToonPath.Parse(fine));
    }

    [Theory]
    [InlineData(@"(a)\1")] // back-reference
    [InlineData(@"(?:a)")] // .NET group syntax
    [InlineData(@"\d+")] // not an I-Regexp escape
    [InlineData(@"[a")] // unclosed class
    [InlineData(@"a\")]
    public void Regex_OutsideIRegexp_IsFalse(string pattern)
    {
        var root = new ToonArray(["a1", "aa", "a"]);

        Assert.Empty(root.Select($"$[?search(@, '{pattern.Replace(@"\", @"\\")}')]"));
    }

    [Fact]
    public void Regex_RunsInLinearTime()
    {
        // (a+)+$ backtracks exponentially on "aaaa...b" in a backtracking engine
        var root = new ToonArray([new string('a', 50_000) + "b"]);
        var watch = Stopwatch.StartNew();

        Assert.Empty(root.Select("$[?match(@, '(a+)+')]"));
        Assert.Empty(root.Select("$[?search(@, '(a+)+$') && !search(@, 'b')]"));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5));

        var longPattern = new string('a', ToonPathRegex.MaxPatternLength + 1);
        Assert.Empty(new ToonArray([longPattern]).Select($"$[?match(@, '{longPattern}')]"));
    }

    [Fact]
    public void Regex_PatternFromTheDocument()
    {
        var root = ToonDocument.Parse("pattern: \"b.*\"\nnames[3]: abc,bcd,b").Root;

        Assert.Equal(["bcd", "b"], root.Select("$.names[?match(@, $.pattern)]").Select(v => v.GetValue<string>()));
    }
}
