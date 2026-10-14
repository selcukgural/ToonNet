# Querying

TOON has the data model of JSON, so ToonNet selects values with **JSONPath** (RFC 9535), the standard query language
for that model. `ToonPath` implements all of it and passes all 715 cases of the
[JSONPath Compliance Test Suite](https://github.com/jsonpath-standard/jsonpath-compliance-test-suite).

The same path works on three levels:

| You have | Method | What it does |
|----------|--------|--------------|
| A `ToonValue` tree | `root.Select(path)` | Selects values in the tree |
| TOON text | `ToonPath.Parse(path).SelectFrom(toon)` | Selects values straight from the text, building only them |
| TOON text, and a type | `ToonSerializer.DeserializeAt<T>` / `DeserializeAll<T>` | Reads the selected values straight into objects |

## Path Syntax

```text
$                         the root
$.orders                  a member (also $['orders'], $["orders"])
$.orders[0]               an item; $.orders[-1] is the last
$.orders[*]               every item (or member)
$.orders[1:5:2]           a slice: start, end, step (all optional)
$.orders[0,2]             several selectors in one bracket
$..id                     descendants: every "id" at any depth
$.orders[?@.total > 100]  a filter: the items for which the expression holds
```

Filter expressions compare with `==`, `!=`, `<`, `<=`, `>`, `>=`, combine with `&&`, `||`, `!` and parentheses, and test
members for existence (`[?@.discount]`). `@` is the item being tested; `$` is the root. Literals are numbers, strings in
single or double quotes, `true`, `false` and `null`.

The five functions of RFC 9535:

| Function | Result |
|----------|--------|
| `length(value)` | The length of a string (in characters), an array or an object |
| `count(query)` | The number of values a query selects: `[?count(@.tags[*]) > 2]` |
| `match(text, pattern)` | Whether the whole string matches a regular expression (I-Regexp, RFC 9485) |
| `search(text, pattern)` | Whether some part of the string matches |
| `value(query)` | The value of a query that selects exactly one |

## ToonNet Functions

Most JSONPath libraries stop at the five standard functions, which leaves everyday filters awkward: I-Regexp has no
case-insensitive flag, and there is no "starts with" or "sum of the lines". ToonNet adds these, under names that
follow the RFC's rules for function extensions:

| Function | Result | Example |
|----------|--------|---------|
| `starts_with(text, prefix)` | Whether a string starts with another | `$.users[?starts_with(@.email, 'admin')]` |
| `ends_with(text, suffix)` | Whether a string ends with another | `$.users[?ends_with(@.email, '@firma.com')]` |
| `contains(text, part)` | Whether a string contains another | `$.products[?contains(@.name, 'Pro')]` |
| `contains(array, item)` | Whether an array has an item (compared by value) | `$.products[?contains(@.tags, 'sale')]` |
| `equals_ignore_case(a, b)` | Whether two strings are equal ignoring case | `$.users[?equals_ignore_case(@.city, 'oslo')]` |
| `lower(text)`, `upper(text)` | The string in lower or upper case | `$.users[?lower(@.name) == 'ada']` |
| `trim(text)` | The string without leading and trailing white space | `$.users[?trim(@.code) == 'X1']` |
| `type(value)` | `'null'`, `'boolean'`, `'number'`, `'string'`, `'object'` or `'array'` | `$.items[?type(@.price) != 'number']` |
| `sum(query)` | The sum of the numbers a query selects (0 for none) | `$.orders[?sum(@.lines[*].price) > 1000]` |
| `min(query)`, `max(query)` | The smallest or largest of the numbers | `$.orders[?max(@.lines[*].qty) > 10]` |
| `avg(query)` | Their average | `$.products[?avg(@.ratings[*]) >= 4.5]` |

- **Strings are compared ordinally**, without culture: the result does not depend on the machine's language.
  `equals_ignore_case`, `lower` and `upper` use the invariant culture's case rules, so Turkish `İ` is not `I`; write
  both forms when it matters (`|| @.city == 'İstanbul'`).
- **Wrong types are false or nothing**, as in the RFC: `starts_with(@.score, '1')` is false when `score` is a number,
  and `lower(@.age)` gives no value, so a comparison with it is false.
- **Aggregates are exact**: `sum`, `min`, `max` and `avg` work with decimals while every number is one
  (`0.1 + 0.1 + 0.1` is `0.3`). A value that is not a number makes the result nothing; the sum of no values is 0.
- **Over text**, `starts_with`, `ends_with`, `contains` and `equals_ignore_case` on a member and a string literal are
  tested on the member's text, like comparisons, without building anything.
- **Portability:** a path that uses these functions is ToonNet's: other JSONPath implementations reject it. Set
  `ToonPathOptions.StandardFunctionsOnly` to allow only the five standard ones, for paths that must stay portable.

### Your Own Functions

Register functions with `ToonPathFunctions` and pass them in the options of the paths that call them:

```csharp
var functions = new ToonPathFunctions()
    .AddPredicate("is_vip", customer => customer is ToonObject c && c["tier"] is ToonString { Value: "gold" }) // [?is_vip(@.customer)]
    .AddPredicate("same_day", (a, b) => SameDay(a, b))                                         // two values
    .AddFunction("net", total => total is ToonNumber n ? new ToonNumber(n.Value / 1.2) : null) // [?net(@.total) > 100]
    .AddNodesPredicate("all_shipped", lines => lines.All(l => l["shipped"] is ToonBoolean { Value: true }))
    .AddNodesFunction("weight", lines => new ToonNumber(lines.Sum(l => l["kg"]!.GetValue<double>())));

var options = new ToonPathOptions { Functions = functions };

var vip = ToonPath.Parse("$.orders[?is_vip(@.customer) && net(@.total) > 100]", options).SelectFrom(toon);
var heavy = ToonSerializer.DeserializeAll<Order>(toon, "$.orders[?weight(@.lines[*]) > 20]",
                                                 new ToonSerializerOptions { PathOptions = options });
```

| Method | Called as | Arguments | Result |
|--------|-----------|-----------|--------|
| `AddPredicate(name, v => bool)` | `[?name(@.a)]` | a value (null when the query selects nothing) | a test |
| `AddPredicate(name, (a, b) => bool)` | `[?name(@.a, 'x')]` | two values | a test |
| `AddFunction(name, v => value)` | `[?name(@.a) > 1]` | a value | a value to compare (null for none) |
| `AddFunction(name, (a, b) => value)` | `[?name(@.a, @.b) == 'x']` | two values | a value to compare |
| `AddNodesPredicate(name, values => bool)` | `[?name(@.lines[*])]` | the values a query selects | a test |
| `AddNodesFunction(name, values => value)` | `[?name(@.lines[*]) > 1]` | the values a query selects | a value to compare |

The type rules of the RFC apply: a predicate is tested, not compared (`[?is_vip(@) == true]` is an error); a value
function is compared; a value argument is a literal, a singular query (`@.name`) or a value function; a nodes argument
is any query. Names are lowercase letters, digits and underscores, starting with a letter, and cannot be those of the
built-in functions. Register functions before compiling the paths that use them; they are called on the threads that
evaluate paths, so they should not change shared state. Exceptions they throw are not caught: an argument can be any
kind of value (or null), so check its kind first, as `is_vip` does. Functions and their limits also work in
[rules](validation).

## On a Tree

```csharp
using ToonNet.Core.Models;

var root = ToonDocument.Parse(toon).Root;

IReadOnlyList<ToonValue> big = root.Select("$.orders[?@.total > 100 && @.paid == true]");
ToonValue? region = root.SelectSingle("$.meta.region");          // null if none; throws if several
IReadOnlyList<ToonPathMatch> where = root.SelectMatches("$..id"); // with normalized paths: $['orders'][2]['id']
```

The methods that take path text compile each path once and cache it. To keep a compiled path yourself, use
`ToonPath.Parse` (or `ToonPath.TryParse`, which returns false instead of throwing `ToonPathException`):

```csharp
using ToonNet.Core.Paths;

var paid = ToonPath.Parse("$.orders[?@.paid == true].id");
var ids = paid.Select(root);
```

Paths also change trees; see [Editing Trees](editing).

## Straight From Text

`SelectFrom` evaluates a path over TOON text with the parser, without building the document: what the path does not
select is skipped, and only the selected values become trees.

```csharp
var path = ToonPath.Parse("$.orders[?@.total > 12000].id");
IReadOnlyList<ToonValue> ids = path.SelectFrom(toon);            // also UTF-8 bytes
```

- A filter that compares members with literals (`@.total > 12000`, `@.region == 'eu'`, `@.paid`) is tested on the
  text of those members, without building anything; other filters read just the members they use.
- Segments with one selector are evaluated over the text. Several selectors in a bracket, a negative slice step, and
  descendant segments (`..`) build the value they apply to and continue on that tree.
- The whole document is read and checked. In non-strict mode, and for filters with absolute queries (`$`), the document
  is parsed first.

In a quick measurement on 10,000 rows, `$.orders[?@.total > 12000].id` ran 2.3x faster than `Parse` + `Select` and
allocated 34 KB instead of 3.5 MB.

## Into Objects

`DeserializeAt<T>` reads the value at a path of member names and indexes, such as the list inside a response envelope:

```csharp
var orders = ToonSerializer.DeserializeAt<List<Order>>(toon, "$.data.orders");
var first  = ToonSerializer.DeserializeAt<Order>(utf8, "$.data.orders[0]");
var last   = await ToonSerializer.DeserializeAtAsync<Order>(stream, "$.data.orders[-1]");
```

- In strict mode (the default), the parser skips what comes before the value without building it, reads the value
  straight into objects and stops at its end: the text after it is not read, and errors there are not reported. A key
  repeated on the way to the value gives its first value (parsing the whole document would report it). A small
  object at the start of a 10,000-row document is read about 20x faster than the whole document.
- A missing value gives `default`. Conversion errors carry the path in the document (`$.data.orders[1].Id`).
- Non-strict mode, where a repeated key replaces an earlier one, parses the whole document.

`DeserializeAll<T>` reads every value a path selects, such as the rows that pass a filter:

```csharp
List<Order?> big = ToonSerializer.DeserializeAll<Order>(toon, "$.orders[?@.total > 100]");
List<decimal> totals = ToonSerializer.DeserializeAll<decimal>(toon, "$.orders[*].total");
```

It is evaluated over the text like `SelectFrom`, and the matches are read straight into objects. On 10,000 rows it ran
2.3x faster than deserializing everything and filtering with LINQ, with 13x less memory.

To read the items of an array inside a document from a stream as they arrive, see
[`DeserializeAsyncEnumerableAt`](streaming#an-array-inside-a-document). All of these have overloads that take a
`ToonTypeInfo` for trimming and NativeAOT.

## Paths From Untrusted Input

`ToonPathOptions` bound the work of a query. Each descendant segment (`..`) visits the whole subtree, so nested ones
multiply the work (`$..*..*..*..*` on a small, deep document would visit billions of values); the step budget and the
timeout stop such queries. A step is a value visited, a filter tested, a segment of a normalized path
`SelectMatches` writes, or 16 characters of a string an operation reads (comparisons, `lower`, `contains`, ...);
comparing or passing an object or array costs a step per value in it, so `$.items[?lower($.bigText) == 'x']` cannot
read a large string once per item for free.

Regular expressions run on .NET's non-backtracking engine: linear in the length of the text, but also in the size of
the pattern's automaton (`[a-z]{1000}` has a thousand states). A match spends a step per 64 characters and states before
it runs, so a large pattern on a long text stops at the budget instead of running for seconds. Patterns that come from a
document (`match(@.code, @.pattern)`) are charged the same way, and the states a pattern builds while matching are not
kept beyond the evaluation (except for a few small patterns).

| Limit | Default | Beyond it |
|-------|---------|-----------|
| `MaxLength` | 4,096 characters | `ToonPathException` when compiling |
| `MaxNesting` | 64 levels of filters, parentheses, function calls | `ToonPathException` when compiling |
| `MaxPatternLength` | 1,024 characters | the pattern is not valid: `match()`/`search()` are false |
| `MaxSteps` + `MaxStepsPerValue` | 1,000,000 + 16 per value of the document (per line when evaluating text) | `ToonPathLimitException` when evaluating |
| `Timeout` | 10 seconds (`Timeout.InfiniteTimeSpan` turns it off) | `ToonPathLimitException` (`IsTimeout`) when evaluating |
| `Functions`, `StandardFunctionsOnly` | none; extensions allowed | an unknown function is a `ToonPathException` |

The defaults suit paths written in code. For paths that clients send (for example to filter an API response), set
stricter limits, on a compiled path or on the serializer options:

```csharp
var limits = new ToonPathOptions
{
    MaxLength = 256,
    MaxNesting = 8,
    MaxSteps = 100_000,
    MaxStepsPerValue = 4,
    Timeout = TimeSpan.FromMilliseconds(200)
};

var path = ToonPath.Parse(filterFromClient, limits);      // the limits stay with the compiled path
var rows = path.SelectFrom(toon);

var options = new ToonSerializerOptions { PathOptions = limits };
var orders = ToonSerializer.DeserializeAll<Order>(toon, filterFromClient, options);
```

`ToonSerializerOptions.PathOptions` applies to `DeserializeAt`, `DeserializeAll` and `DeserializeAsyncEnumerableAt`; the
methods of `ToonValue` that take path text (`Select`, `SetValue`, ...) use the defaults. Paths with the default limits
are compiled once and cached (up to 512 characters); paths with other limits are compiled on each `Parse`.

## See Also

- [Rules and Validation](validation): rules written with paths, such as "every order total is positive"
- [Editing Trees](editing): navigating, changing and patching `ToonValue` trees
- [Reader and Writer](reader-writer): reading documents token by token
- [Streaming](streaming): large arrays item by item
