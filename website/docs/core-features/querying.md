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

| Function | Result |
|----------|--------|
| `length(value)` | The length of a string (in characters), an array or an object |
| `count(query)` | The number of values a query selects: `[?count(@.tags[*]) > 2]` |
| `match(text, pattern)` | Whether the whole string matches a regular expression (I-Regexp, RFC 9485) |
| `search(text, pattern)` | Whether some part of the string matches |
| `value(query)` | The value of a query that selects exactly one |

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
  straight into objects and stops at its end: the text after it is not read, and errors there are not reported. A small
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

Regular expressions run on .NET's non-backtracking engine, patterns are limited to 1,024 characters, and nesting
(parentheses, filters, function calls) to 64 levels, so a path cannot hang a query on a pattern or exhaust the stack.
Each descendant segment (`..`) visits the whole subtree, so nested ones multiply the work (`$..*..*..*..*` on a small,
deep document would visit billions of values): an evaluation takes at most a million steps plus 16 per value of the
document, and throws `InvalidOperationException` beyond that. If clients send paths (for example to filter an API
response), prefer a fixed set, or reject paths with more than one `..`.

## See Also

- [Editing Trees](editing): navigating, changing and patching `ToonValue` trees
- [Reader and Writer](reader-writer): reading documents token by token
- [Streaming](streaming): large arrays item by item
