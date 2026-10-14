# Language Models

TOON was designed for prompts: tables write each field name once, so the same data takes fewer tokens than JSON.
`ToonPrompt` (namespace `ToonNet.Core.Llm`) covers the round trip: data into the prompt, the shape of the answer, and the
answer back into objects.

```csharp
using ToonNet.Core.Llm;

var prompt = $"""
    These are the open orders (TOON):
    {ToonPrompt.Serialize(orders, new ToonPromptOptions { MaxArrayItems = 50 })}

    Summarize them. Answer in a ```toon block with this shape:
    {ToonPrompt.DescribeShape<Summary>()}
    """;

string answer = await CallModelAsync(prompt);
Summary? summary = ToonPrompt.Deserialize<Summary>(answer);
```

## Data for a Prompt

`ToonPrompt.Serialize` writes a value like `ToonSerializer.Serialize`. With `MaxArrayItems`, long arrays keep their
first items, declare the number they kept, and are followed by a comment line that says how many were left out:

```toon
orders[2]{id,customer,total}:
  1,Ada,250.5
  2,Bob,80
  # 380 more items
```

The text is still valid TOON (parsers drop comments), and the model knows the data is incomplete. `ToonPrompt.Encode`
does the same for a `ToonValue` tree, without changing it.

## The Shape of an Answer

`DescribeShape<T>()` gives a template of the TOON the serializer writes for `T`, with a placeholder for each value and
`N` for each length. It follows the serializer's property names (naming policy, `[ToonProperty]`), order and forms
(tables for lists of flat objects):

```csharp
public class Summary
{
    public int Count { get; set; }
    public Status Status { get; set; }           // enum Status { Open, Closed }
    public DateTime? Since { get; set; }
    public List<string> Customers { get; set; } = [];
    public List<Line> Lines { get; set; } = [];  // Line { Sku, Quantity }
}
```

```toon
Count: <integer>
Status: <Open|Closed>
Since: <date-time|null>
Customers[N]: <string>,...
Lines[N]{Sku,Quantity}:
  <string>,<integer>
```

Placeholders are `<string>`, `<integer>`, `<number>`, `<boolean>`, `<date-time>`, `<date>`, `<time>`, `<duration>`,
`<uuid>`, `<uri>`, the names of an enum, and `<any>`; `|null` marks nullable values, and `<key>` the keys of a
dictionary. `DescribeShape` uses reflection (it is not available for trimming or NativeAOT).

## Reading an Answer

Models wrap their answer in prose and code fences. `ToonPrompt.Extract` takes the content of the first code block marked
`toon` (```` ```toon ```` or `~~~toon`), else of the first unmarked block, else the whole answer; a block cut off at the
end of the answer runs to the end.

`ToonPrompt.Deserialize<T>` and `ToonPrompt.Parse` read that text in non-strict mode, so the slips models make do not
fail the answer: a length that does not match the rows (`items[3]` with two rows), a repeated key (the last one wins),
or an indentation level jumped. Answers that are not TOON at all still throw `ToonParseException`, and values that do
not fit the type `ToonSerializationException`, so you can retry with the error in the prompt.

```csharp
try
{
    return ToonPrompt.Deserialize<Summary>(answer);
}
catch (ToonException ex)
{
    // Ask again: "Your answer could not be read: {ex.Message}"
}
```

With a `[ToonSerializable]` type, `ToonPrompt.Serialize(value, typeInfo, options)` and
`ToonPrompt.Deserialize(answer, typeInfo)` work without reflection.

## See Also

- [Querying](querying): taking parts of a large document before putting it in a prompt
- [Serialization](serialization): options such as the naming policy and the delimiter
