using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Paths;
using ToonNet.Core.Serialization;
using ToonNet.Core.Validation;

namespace ToonNet.Tests.Paths;

/// <summary>Behaviors of paths, functions and rules that the main tests do not reach.</summary>
public class ToonPathEdgeCaseTests
{
    [Fact]
    public void Length_CountsCharacters_NotUtf16Units()
    {
        const string toon = "s[2]: a😀b,ab";
        var path = ToonPath.Parse("$.s[?length(@) == 3]");

        Assert.Equal("a😀b", Assert.Single(path.Select(ToonDocument.Parse(toon).Root)).GetValue<string>());
        Assert.Single(path.SelectFrom(toon));
    }

    [Fact]
    public void SetValue_ReplacesItems()
    {
        var root = ToonDocument.Parse("tags[2]: a,b").Root;

        root.SetValue("$.tags[0]", "z");
        Assert.Equal("tags[2]: z,b", ToonSerializer.Serialize(root));

        root.SetValue("$.tags[-2]", "y");
        Assert.Equal("tags[2]: y,b", ToonSerializer.Serialize(root));
    }

    [Theory]
    [InlineData("[^a]", new[] { "b", "A" })]
    [InlineData("\\\\p{Lu}", new[] { "A" })]
    [InlineData("\\\\pL", new string[0])]     // \p needs braces: not an I-Regexp
    [InlineData("[[a]]", new string[0])]      // no nested classes
    [InlineData("a{2}", new[] { "aa" })]
    public void Regex_Classes(string pattern, string[] expected)
    {
        var root = ToonDocument.Parse("v[4]: a,b,A,aa").Root;

        Assert.Equal(expected, root.Select($"$.v[?match(@, '{pattern}')]").Select(v => v.GetValue<string>()));
    }

    [Fact]
    public void Type_OfContainers()
    {
        var root = ToonDocument.Parse("v[3]:\n  - a: 1\n  - [1]: 2\n  - x").Root;

        Assert.Single(root.Select("$.v[?type(@) == 'object']"));
        Assert.Single(root.Select("$.v[?type(@) == 'array']"));
        Assert.Single(root.Select("$.v[?type(@) == 'string']"));
    }

    [Fact]
    public void MinAndMax_OfDoubles()
    {
        var root = ToonDocument.Parse("v[3]: 1e300,2,3").Root;

        // @ is the array v: the root's only member
        Assert.Single(root.Select("$[?min(@[*]) == 2]"));
        Assert.Single(root.Select("$[?max(@[*]) == 1e300]"));
        Assert.Empty(root.Select("$[?min(@[*]) == 1e300]"));
    }

    [Fact]
    public void Options_RejectLimitsBelowOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToonPathOptions { MaxLength = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToonPathOptions { MaxPatternLength = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToonPathOptions { MaxNesting = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToonPathOptions { MaxStepsPerValue = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToonPathOptions { Timeout = TimeSpan.FromSeconds(-1) });
        Assert.Equal(0, new ToonPathOptions { MaxStepsPerValue = 0 }.MaxStepsPerValue);
    }

    [Fact]
    public void Functions_RejectNullDelegates_AndNames()
    {
        var functions = new ToonPathFunctions();

        Assert.Throws<ArgumentNullException>(() => functions.AddPredicate("a", (Func<ToonValue?, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => functions.AddPredicate("a", (Func<ToonValue?, ToonValue?, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => functions.AddFunction("a", (Func<ToonValue?, ToonValue?>)null!));
        Assert.Throws<ArgumentNullException>(() => functions.AddFunction("a", (Func<ToonValue?, ToonValue?, ToonValue?>)null!));
        Assert.Throws<ArgumentNullException>(() => functions.AddNodesPredicate("a", null!));
        Assert.Throws<ArgumentNullException>(() => functions.AddNodesFunction("a", null!));
        Assert.Throws<ArgumentNullException>(() => functions.AddFunction(null!, v => v));
        Assert.Throws<ArgumentException>(() => functions.AddNodesFunction("Bad", _ => null));
        Assert.Throws<ArgumentException>(() => functions.AddFunction("count", v => v));
        Assert.Empty(functions.Names);
    }

    [Fact]
    public void SharedPaths_EvaluateConcurrently()
    {
        var rows = "rows[2000]{a,b}:\n" + string.Join('\n', Enumerable.Range(0, 2000).Select(i => $"  {i},x{i % 7}"));
        var root = ToonDocument.Parse(rows).Root;
        var path = ToonPath.Parse("$.rows[?@.a > 1000 || starts_with(@.b, 'x3')]");
        var expected = path.Select(root).Count;

        Parallel.For(0, 64, _ =>
        {
            Assert.Equal(expected, path.SelectFrom(rows).Count);
            Assert.Equal(expected, path.Select(root).Count);
        });
    }

    [Fact]
    public void Functions_RegisteredWhilePathsCompile()
    {
        var functions = new ToonPathFunctions();
        var options = new ToonPathOptions { Functions = functions };

        Parallel.For(0, 200, i =>
        {
            functions.AddPredicate($"f{i}", _ => true);
            Assert.Single(ToonPath.Parse($"$[?f{i}(@)]", options).Select(new ToonArray([1])));
        });

        Assert.Equal(200, functions.Names.Count);
    }

    #region Rules

    [Fact]
    public void Rules_UseTheirOptions()
    {
        var limited = new ToonRuleSet(new ToonPathOptions { MaxLength = 5 });
        Assert.Equal("The expression is longer than 5 characters", Assert.Throws<ToonPathException>(() => limited.Rule("$.a", "@ > 100000", "m")).Reason);

        var standard = new ToonRuleSet(new ToonPathOptions { StandardFunctionsOnly = true });
        Assert.Throws<ToonPathException>(() => standard.Rule("$.a", "starts_with(@, 'x')", "m"));
    }

    [Fact]
    public void Rules_ValidateUtf8_AndUniqueBooleans()
    {
        var rules = new ToonRuleSet().Unique("$.v[*]", "{value} repeats");
        var result = rules.Validate(System.Text.Encoding.UTF8.GetBytes("v[4]: true,true,null,null"));

        Assert.Equal(["$['v'][1]: true repeats", "$['v'][3]: null repeats"], result.Violations.Select(v => v.ToString()));
        Assert.Equal("$['v'][1]: true repeats\n$['v'][3]: null repeats", result.ToString());
    }

    [Fact]
    public void Rules_AsText()
    {
        var rules = new ToonRuleSet().Required("$.a", "m").Forbidden("$.b", "m").Unique("$.c", "m").Rule("$.d", "@ > 1", "m");

        Assert.Equal(["$.a: required", "$.b: forbidden", "$.c: unique", "$.d: @ > 1"], rules.Rules.Select(r => r.ToString()));
    }

    [Fact]
    public void Rules_ShareOneBudget()
    {
        // Each rule alone fits in the budget; together they do not
        var rows = "v[300]: " + string.Join(',', Enumerable.Range(0, 300));
        var options = new ToonPathOptions { MaxSteps = 1000, MaxStepsPerValue = 0 };
        var one = new ToonRuleSet(options).Rule("$.v[*]", "@ >= 0", "m");
        var many = new ToonRuleSet(options).Rule("$.v[*]", "@ >= 0", "m").Rule("$.v[*]", "@ >= 0", "m").Rule("$.v[*]", "@ >= 0", "m");

        Assert.True(one.Validate(rows).IsValid);
        Assert.Throws<ToonPathLimitException>(() => many.Validate(rows));
    }

    #endregion
}
