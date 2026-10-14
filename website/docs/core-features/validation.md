# Rules and Validation

Deserializing checks that a document fits a type: a number where a number goes. Most data also has rules a type cannot
say: a total that is not negative, a status from a fixed list, order lines that add up to the order total, ids that do
not repeat, a password that must never be sent. XML writes such rules in Schematron; with ToonNet they are paths and
filter expressions, checked by `ToonRuleSet`.

```csharp
using ToonNet.Core.Validation;

var rules = new ToonRuleSet()
    .Rule("$.orders[*].total", "@ >= 0", "An order total cannot be negative ({value})")
    .Rule("$.orders[*]", "@.status == 'open' || @.status == 'closed'", "Unknown status")
    .Rule("$.orders[*]", "sum(@.lines[*].price) == @.total", "The lines do not add up to the total")
    .Rule("$.orders[*].lines[*]", "@.qty > 0 && type(@.sku) == 'string'", "Bad line")
    .Required("$.customer.email", "The customer needs an email")
    .Forbidden("$..password", "Passwords must not be sent")
    .Unique("$.orders[*].id", "Order id {value} repeats");

ToonRuleResult result = rules.Validate(toon);   // also UTF-8 bytes, or a ToonValue tree

if (!result.IsValid)
{
    foreach (var violation in result.Violations)
    {
        Console.WriteLine(violation);   // $['orders'][1]['total']: An order total cannot be negative (-5)
    }
}
```

## Kinds of Rules

| Rule | Holds when | A violation for |
|------|------------|-----------------|
| `Rule(path, assertion, message)` | The assertion holds for every value the path selects | each value it does not hold for |
| `Required(path, message)` | The path selects at least one value | the path, once |
| `Forbidden(path, message)` | The path selects nothing | each value it selects |
| `Unique(path, message)` | The values the path selects are distinct (compared by value, so `1` and `1.0` are the same) | each value that repeats an earlier one |

An assertion is a [filter expression](querying#path-syntax) without the `[?` and `]`: `@` is the value the rule's path
selected, and `$` is the root of the document, so rules can compare with other parts of it
(`"@.total <= $.limits.maxOrder"`). Assertions can use comparisons, `&&`, `||`, `!`, existence tests
(`"@.email"`), the standard functions, the [ToonNet functions](querying#toonnet-functions) (`starts_with`, `contains`,
`type`, `sum`, ...) and your own [registered functions](querying#your-own-functions):

```csharp
// IsValidIban: your own check
var functions = new ToonPathFunctions().AddPredicate("is_iban", v => v is ToonString s && IsValidIban(s.Value));

var rules = new ToonRuleSet(new ToonPathOptions { Functions = functions })
    .Rule("$.payments[*]", "is_iban(@.account)", "Not an IBAN: {value}");
```

A rule's path that selects nothing makes nothing to check: `Rule("$.orders[*]", ...)` holds for a document without
orders. Add `Required` when the values must be there.

## Results

Each `ToonRuleViolation` has:

- `Path`: the normalized path of the value (`$['orders'][3]['total']`), or the rule's path for `Required`
- `Message`: the rule's message, with `{path}` and `{value}` filled in (`{value}` is a primitive's text, or the size of
  an object or array)
- `Value`: the value that broke the rule (null for `Required`)
- `Rule` and `Severity`: the rule, and whether it is an error or a warning

Rules have a severity: `ToonRuleSeverity.Error` (the default) makes `IsValid` false, `ToonRuleSeverity.Warning` is
reported without it. `result.Errors` and `result.Warnings` separate them, and `result.ThrowIfInvalid()` throws a
`ToonRuleException` whose message lists the errors on one line:

```text
The document has 2 rule violations: $['orders'][1]['total']: An order total cannot be negative (-5); $.customer.email: The customer needs an email
```

A validation collects at most `MaxViolations` violations (1,000 by default) and then stops, with `result.IsTruncated`
true, so a large document full of bad values cannot make a huge result.

## Answers From Language Models

A model's answer can be well-formed and still wrong. `ToonPrompt.DeserializeValidated` reads the answer leniently,
checks it against the rules, and deserializes it only when it follows them; the exception's message is ready to send
back to the model:

```csharp
try
{
    return ToonPrompt.DeserializeValidated<Summary>(answer, rules);
}
catch (ToonRuleException ex)
{
    prompt += $"\nYour answer breaks these rules, fix them: {ex.Message}";
}
```

## Errors in Rules and Limits

A path or an assertion that is not valid throws `ToonPathException` when the rule is added, so a rule set is checked
once, when it is built. A rule set is thread-safe for validation once its rules are added: build it once (for example
in a static field) and validate many documents with it.

Validation uses the [limits](querying#paths-from-untrusted-input) of the rule set's `ToonPathOptions`: all the rules
share one budget of steps and one timeout per document (beyond them, `ToonPathLimitException`), so rules or documents
from untrusted sources cannot run away.

## See Also

- [Querying](querying): the paths, filter expressions and functions rules are written with
- [Language Models](language-models): prompts and answers
