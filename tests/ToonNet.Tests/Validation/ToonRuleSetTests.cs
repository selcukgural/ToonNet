using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Paths;
using ToonNet.Core.Validation;

namespace ToonNet.Tests.Validation;

public class ToonRuleSetTests
{
    private const string Orders = """
                                  customer:
                                    name: Ada
                                    password: secret
                                  orders[4]:
                                    - id: 1
                                      status: open
                                      total: 30
                                      lines[2]{sku,price}:
                                        a,10
                                        b,20
                                    - id: 2
                                      status: shipped
                                      total: -5
                                      lines[1]{sku,price}:
                                        c,-5
                                    - id: 1
                                      status: closed
                                      total: 12
                                      lines[1]{sku,price}:
                                        d,10
                                    - id: 1.0
                                      status: open
                                      total: 0
                                      lines[0]:
                                  """;

    private static ToonRuleSet Rules() => new ToonRuleSet()
        .Rule("$.orders[*]", "@.total >= 0", "Total of order {value} is negative")
        .Rule("$.orders[*].total", "@ >= 0", "Negative total {value} at {path}")
        .Rule("$.orders[*]", "@.status == 'open' || @.status == 'closed'", "Unknown status")
        .Rule("$.orders[*]", "sum(@.lines[*].price) == @.total", "Lines do not add up", ToonRuleSeverity.Warning)
        .Required("$.customer.email", "The customer needs an email ({path})")
        .Forbidden("$..password", "Passwords must not be sent")
        .Unique("$.orders[*].id", "Order id {value} repeats");

    [Fact]
    public void Validate_ReportsEveryViolation()
    {
        var result = Rules().Validate(Orders);

        Assert.False(result.IsValid);
        Assert.Equal(
        [
            "$['orders'][1]: Total of order an object of 4 members is negative",
            "$['orders'][1]['total']: Negative total -5 at $['orders'][1]['total']",
            "$['orders'][1]: Unknown status",
            "$['orders'][2]: Lines do not add up",
            "$.customer.email: The customer needs an email ($.customer.email)",
            "$['customer']['password']: Passwords must not be sent",
            "$['orders'][2]['id']: Order id 1 repeats",
            "$['orders'][3]['id']: Order id 1 repeats" // 1.0 equals 1
        ], result.Violations.Select(v => v.ToString()));

        Assert.Single(result.Warnings);
        Assert.Equal(7, result.Errors.Count());
        Assert.Equal(-5, result.Violations[1].Value!.GetValue<int>());
        Assert.Null(result.Violations[4].Value);
        Assert.Equal(ToonRuleKind.Required, result.Violations[4].Rule.Kind);
        Assert.Equal("$.orders[*]: @.total >= 0", result.Violations[0].Rule.ToString());
    }

    [Fact]
    public void ValidDocuments_AndWarningsOnly()
    {
        var rules = new ToonRuleSet()
            .Rule("$.orders[*]", "@.total >= 0", "negative")
            .Rule("$.orders[*]", "@.note", "no note", ToonRuleSeverity.Warning);

        var result = rules.Validate("orders[2]{total}:\n  1\n  2");

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Warnings.Count());
        result.ThrowIfInvalid();

        Assert.True(rules.Validate("a: 1").IsValid); // no orders: the assertions have nothing to check
        Assert.Empty(rules.Validate("a: 1").Violations);
    }

    [Fact]
    public void ThrowIfInvalid_ListsTheErrors()
    {
        var ex = Assert.Throws<ToonRuleException>(() => Rules().Validate(Orders).ThrowIfInvalid());

        Assert.Equal(7, ex.Violations.Count);
        Assert.StartsWith("The document breaks 7 rules: $['orders'][1]: Total of order an object of 4 members is negative; ", ex.Message);
    }

    [Fact]
    public void Assertions_UseTheRootAndFunctions()
    {
        var functions = new ToonPathFunctions().AddPredicate("is_sku", v => v is ToonString { Value.Length: 1 });
        var rules = new ToonRuleSet(new ToonPathOptions { Functions = functions })
            .Rule("$.orders[*]", "@.total <= $.limit", "Over the limit")
            .Rule("$.orders[*].lines[*]", "is_sku(@.sku) && starts_with(@.sku, 'a')", "Bad sku {value}");

        var result = rules.Validate(ToonDocument.Parse(Orders.Replace("customer:", "limit: 20\ncustomer:")).Root);

        Assert.Equal(["$['orders'][0]: Over the limit", "$['orders'][0]['lines'][1]: Bad sku an object of 2 members",
                      "$['orders'][1]['lines'][0]: Bad sku an object of 2 members", "$['orders'][2]['lines'][0]: Bad sku an object of 2 members"],
                     result.Violations.Select(v => v.ToString()));
    }

    [Fact]
    public void Unique_ComparesData()
    {
        var rules = new ToonRuleSet().Unique("$.items[*]", "{value} repeats");
        var result = rules.Validate("items[6]:\n  - a: 1\n    b: 2\n  - b: 2\n    a: 1\n  - [2]: 1,2\n  - [2]: 2,1\n  - x\n  - \"x\"");

        Assert.Equal(["$['items'][1]: an object of 2 members repeats", "$['items'][5]: x repeats"], result.Violations.Select(v => v.ToString()));
    }

    [Fact]
    public void InvalidRules_FailWhenAdded()
    {
        var rules = new ToonRuleSet();

        Assert.Throws<ToonPathException>(() => rules.Rule("$.a[", "@", "m"));
        Assert.Throws<ToonPathException>(() => rules.Rule("$.a", "@.b ==", "m"));
        Assert.Throws<ToonPathException>(() => rules.Rule("$.a", "@.b] , $.c[?@", "m"));
        Assert.Throws<ToonPathException>(() => rules.Rule("$.a", "1", "m"));            // a literal is not a test
        Assert.Throws<ToonPathException>(() => rules.Rule("$.a", "length(@)", "m"));    // a value must be compared
        Assert.Throws<ArgumentNullException>(() => rules.Rule("$.a", "@", null!));
        Assert.Throws<ArgumentNullException>(() => rules.Required(null!, "m"));
        Assert.Empty(rules.Rules);
    }

    [Fact]
    public void Validation_IsBounded()
    {
        var deep = string.Concat(Enumerable.Range(0, 40).Select(i => new string(' ', i * 2) + "a:\n")) + new string(' ', 80) + "b: 1";
        var rules = new ToonRuleSet().Rule("$..*..*..*..*..*..*", "@", "m");

        Assert.Throws<InvalidOperationException>(() => rules.Validate(deep));
    }
}
