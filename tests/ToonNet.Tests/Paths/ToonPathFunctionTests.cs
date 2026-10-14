using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Paths;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Paths;

public class ToonPathFunctionTests
{
    private const string Users = """
                                 users[6]{name,email,city,tier,score}:
                                   Ada,admin@firma.com,İstanbul,gold,10
                                   bob,bob@other.org,ISTANBUL,silver,2.5
                                   Cy,"cy\"x@firma.com",Ankara,gold,7
                                   Di,di@firma.com,izmir,null,true
                                   " Ed ",ed@firma.com,Oslo,bronze,-0
                                   42,x@y.z,Rome,gold,1e2
                                 products[3]:
                                   - name: Pro Laptop
                                     tags[2]: sale,new
                                     lines[2]{qty,price}:
                                       1,1200.50
                                       2,30
                                   - name: Mouse
                                     tags[0]:
                                     lines[1]{qty,price}:
                                       5,10
                                   - name: pro cable
                                     tags[1]: sale
                                     lines[2]{qty,price}:
                                       1,x
                                       2,4
                                 """;

    private static ToonValue Root => ToonDocument.Parse(Users).Root;

    private static string[] Names(string path) =>
        Root.Select(path).Select(v => v is ToonString s ? s.Value : v.ToString()!).ToArray();

    /// <summary>Over text the functions must select what they select on the tree.</summary>
    private static void AssertSameAsTree(string path, ToonPathOptions? options = null)
    {
        var compiled = ToonPath.Parse(path, options);
        var expected = compiled.Select(ToonDocument.Parse(Users).Root);
        var actual = compiled.SelectFrom(Users);

        Assert.True(expected.Count == actual.Count, $"{path}: {expected.Count} != {actual.Count}");

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.True(ToonValue.DeepEquals(expected[i], actual[i]), $"{path}: value {i}");
        }
    }

    [Fact]
    public void StringFunctions()
    {
        Assert.Equal(["Ada"], Names("$.users[?starts_with(@.email, 'admin')].name"));
        Assert.Equal(["Ada", "Cy", "Di", " Ed "], Names("$.users[?ends_with(@.email, '@firma.com')].name"));
        Assert.Equal(["Cy"], Names("$.users[?contains(@.email, '\"')].name"));
        Assert.Equal(["bob"], Names("$.users[?equals_ignore_case(@.city, 'istanbul')].name")); // ordinal: İ is not I
        Assert.Equal(["bob"], Names("$.users[?lower(@.name) == 'bob'].name"));
        Assert.Equal(["Ada"], Names("$.users[?upper(@.name) == 'ADA'].name"));
        Assert.Equal([" Ed "], Names("$.users[?trim(@.name) == 'Ed'].name"));
        Assert.Equal(["Ada", "bob", "Cy", "Di", " Ed ", "42"], Names("$.users[?!starts_with(@.name, '4')].name")); // 42 is a number: not a string

        // Not a string: false, or no value
        Assert.Empty(Names("$.users[?starts_with(@.score, '1')]"));
        Assert.Empty(Names("$.users[?starts_with(@.missing, 'a')]"));
        Assert.Empty(Names("$.users[?lower(@.score) == '10']"));
    }

    [Fact]
    public void Contains_FindsItemsInArrays()
    {
        Assert.Equal(["Pro Laptop", "pro cable"], Names("$.products[?contains(@.tags, 'sale')].name"));
        Assert.Equal(["Pro Laptop"], Names("$.products[?contains(@.tags, 'new')].name"));
        Assert.Equal(["Pro Laptop", "pro cable"], Names("$.products[?contains(lower(@.name), 'pro')].name"));
    }

    [Fact]
    public void TypeAndAggregates()
    {
        Assert.Equal(["Di"], Names("$.users[?type(@.score) == 'boolean'].name"));
        Assert.Equal(["Di"], Names("$.users[?type(@.tier) == 'null'].name"));
        Assert.Equal(["Ada", "bob", "Cy", " Ed ", "42"], Names("$.users[?type(@.score) == 'number'].name"));
        Assert.Empty(Names("$.users[?type(@.missing) == 'null']"));

        Assert.Equal(["Pro Laptop"], Names("$.products[?sum(@.lines[*].price) > 1000].name"));
        Assert.Equal(["Mouse"], Names("$.products[?min(@.lines[*].qty) == 5].name"));
        Assert.Equal(["Pro Laptop", "pro cable"], Names("$.products[?max(@.lines[*].qty) == 2].name"));
        Assert.Equal(["Mouse"], Names("$.products[?avg(@.lines[*].price) == 10].name"));
        Assert.Empty(Names("$.products[?sum(@.lines[*].price) == 4]"));    // 'x' is not a number: no value
        Assert.Equal(2, Names("$[?sum(@.users[*].missing) == 0]").Length); // the sum of nothing is 0 (for both members)
        Assert.Empty(Names("$[?avg(@.users[*].missing) == 0]"));

        var exact = ToonPath.Parse("$[?sum(@[*]) == 0.3]").Select(ToonDocument.Parse("values[3]: 0.1,0.1,0.1").Root);
        Assert.Single(exact); // decimals, not 0.30000000000000004
    }

    [Theory]
    [InlineData("$.users[?starts_with(@.email, 'admin')]")]
    [InlineData("$.users[?ends_with(@.email, '.com') && !starts_with(@.name, 'C')]")]
    [InlineData("$.users[?contains(@.email, '\"')]")]
    [InlineData("$.users[?contains(@.email, 'firma') || equals_ignore_case(@.city, 'oslo')]")]
    [InlineData("$.users[?equals_ignore_case(@.city, 'İSTANBUL')]")]
    [InlineData("$.users[?starts_with(@.score, '1')]")]
    [InlineData("$.users[?starts_with(@.name, '4')]")]
    [InlineData("$.users[?lower(@.name) == 'bob']")]
    [InlineData("$.users[?type(@.score) == 'number']")]
    [InlineData("$.products[?contains(@.tags, 'sale')]")]
    [InlineData("$.products[?contains(@.name, 'pro') || contains(@.tags, 'new')]")]
    [InlineData("$.products[?sum(@.lines[*].price) > 1000].lines[0]")]
    [InlineData("$..[?starts_with(@, 'sa')]")]
    [InlineData("$.products[*].tags[?starts_with(@, 'n')]")]
    public void OverText_MatchesTheTree(string path)
    {
        AssertSameAsTree(path);
    }

    [Fact]
    public void RegisteredFunctions()
    {
        var functions = new ToonPathFunctions()
            .AddPredicate("is_gold", tier => tier is ToonString { Value: "gold" })
            .AddPredicate("same", (a, b) => ToonValue.DeepEquals(a, b))
            .AddFunction("double_it", v => v is ToonNumber n ? new ToonNumber(n.Value * 2) : null)
            .AddFunction("concat", (a, b) => a is ToonString x && b is ToonString y ? new ToonString(x.Value + y.Value) : null)
            .AddNodesPredicate("all_positive", values => values.All(v => v is ToonNumber { Value: > 0 }))
            .AddNodesFunction("total", values => new ToonNumber(values.OfType<ToonNumber>().Sum(n => n.Value)));
        var options = new ToonPathOptions { Functions = functions };
        var root = Root;

        Assert.Equal(3, ToonPath.Parse("$.users[?is_gold(@.tier)]", options).Select(root).Count);
        Assert.Single(ToonPath.Parse("$.users[?same(@.name, 'bob')]", options).Select(root));
        Assert.Single(ToonPath.Parse("$.users[?double_it(@.score) == 20]", options).Select(root));
        Assert.Single(ToonPath.Parse("$.users[?concat(@.name, '!') == 'Ada!']", options).Select(root));
        Assert.Equal(2, ToonPath.Parse("$.products[?all_positive(@.lines[*].qty)]", options).Select(root).Count - 1);
        Assert.Single(ToonPath.Parse("$.products[?total(@.lines[*].qty) == 5]", options).Select(root));

        // Over text, and through the serializer's options
        AssertSameAsTree("$.users[?is_gold(@.tier) && double_it(@.score) > 10]", options);
        Assert.Equal(3, ToonSerializer.DeserializeAll<string>(Users, "$.users[?is_gold(@.tier)].name",
                                                              new ToonSerializerOptions { PathOptions = options }).Count);

        // Without the functions, the names are unknown
        Assert.Equal("Unknown function 'is_gold'", Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?is_gold(@)]")).Reason);
        Assert.Equal(["all_positive", "concat", "double_it", "is_gold", "same", "total"], functions.Names.Order());
    }

    [Fact]
    public void RegisteredFunctions_AreTypeChecked()
    {
        var options = new ToonPathOptions
        {
            Functions = new ToonPathFunctions().AddPredicate("ok", _ => true).AddFunction("v", x => x).AddNodesFunction("n", _ => null)
        };

        Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?ok(@) == true]", options));   // a predicate is not compared
        Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?v(@)]", options));             // a value function is compared
        Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?ok(@.*)]", options));          // a value argument is singular
        Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?n('x') == 1]", options));      // a nodes argument is a query
        Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?ok(@, @)]", options));         // arity
        Assert.NotNull(ToonPath.Parse("$[?n(@.*) == 1 && ok(v(@))]", options));
    }

    [Theory]
    [InlineData("Starts")]
    [InlineData("1st")]
    [InlineData("a-b")]
    [InlineData("")]
    [InlineData("length")]
    [InlineData("starts_with")]
    public void RegisteredFunctions_NeedValidNewNames(string name)
    {
        Assert.Throws<ArgumentException>(() => new ToonPathFunctions().AddPredicate(name, _ => true));
    }

    [Fact]
    public void RegisteredFunctions_CannotBeRegisteredTwice()
    {
        var functions = new ToonPathFunctions().AddPredicate("f", _ => true);

        Assert.Throws<ArgumentException>(() => functions.AddFunction("f", x => x));
    }

    [Fact]
    public void StandardFunctionsOnly()
    {
        var options = new ToonPathOptions { StandardFunctionsOnly = true, Functions = new ToonPathFunctions().AddPredicate("mine", _ => true) };

        Assert.Equal("'starts_with' is not a function of RFC 9535 (StandardFunctionsOnly is set)",
                     Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?starts_with(@, 'a')]", options)).Reason);
        Assert.Equal("'mine' is not a function of RFC 9535 (StandardFunctionsOnly is set)",
                     Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?mine(@)]", options)).Reason);
        Assert.NotNull(ToonPath.Parse("$[?length(@) > 1 && match(@, 'a.*')]", options));
    }
}
