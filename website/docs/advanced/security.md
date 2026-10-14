# Security

ToonNet reads documents that come from outside the application: request bodies, uploaded files, answers of language
models. This page lists what it does with such input by default, what the limits are, and the settings that loosen
them. Report security problems privately, as [SECURITY.md](https://github.com/selcukgural/ToonNet/blob/master/SECURITY.md)
describes.

## Safe Defaults

| Default | What it prevents |
|---------|------------------|
| Strict mode (`ToonOptions.StrictMode = true`) | Duplicate keys (one parser takes the first, another the last), lengths that do not match the rows, tabs in indentation |
| `MaxDepth` 100 for parsing and serialization; at most 200 without `AllowExtendedLimits` | Stack exhaustion and deep recursion |
| Declared lengths (`items[2147483647]`) reserve nothing | Memory reserved before the data is there |
| Strict UTF-8 (ill-formed bytes are an error, not U+FFFD) | Text that differs from what was checked |
| Only public setters are set from documents | A document setting what the code keeps for itself (`IsAdmin { get; private set; }`) |
| Values are written as their declared type | Members of a derived type (a password hash) reaching a response through a narrower type |
| `RespectRequiredMembers = true` | A `required` member left at its default (`null`, `false`) by a document that omits it |
| No type names in documents | A document choosing which .NET type is created |
| Error messages hold no values; keys are escaped and cut to 64 characters | Personal data in logs, forged log lines, huge messages |
| ASP.NET Core: 4 MB request bodies (formatter and `ReadFromToonAsync`) | Large bodies that take many times their size in memory |

The serializer never creates a type the document names: a property of type `object` becomes dictionaries, lists and
primitives, and interfaces and abstract types cannot be read. Converters come only from the options and from
`[ToonConverter]` attributes.

## Reading Objects From Clients

Only `set` and `init` setters are used. A property with a `private`, `protected` or `internal` setter is read-only for
documents:

```csharp
public class Account
{
    public string Name { get; set; } = "";
    public bool IsAdmin { get; private set; }    // never set from a document

    [ToonInclude]
    public int Version { get; private set; }     // set from a document: opted in
}
```

A missing `required` member is an error (`RespectRequiredMembers`). Unknown keys are skipped by default; set
`UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow` to reject them. Use the types of the request, not your
entities: a separate input type has exactly the members a client may set.

## Writing Responses

An object is written with the members of the type it is declared as: the property's type, the item type of a
collection, the value type of a dictionary, or the `T` of `Serialize<T>`. An entity in a property of a summary type
writes only the summary's members:

```csharp
public class UserSummary { public string Name { get; set; } = ""; }
public class UserEntity : UserSummary { public string PasswordHash { get; set; } = ""; }

ToonSerializer.Serialize(new Envelope { User = entity });   // User: Name only
```

A value declared as `object` is written as what it is. `SerializeRuntimeTypes = true` writes every object with the
members of its runtime type, as ToonNet did before 4.0; use it only for types whose derived types hold nothing
private. In ASP.NET Core the root of a response is written as its runtime type, as System.Text.Json does there: return
the type you mean to send.

`[ToonIgnore]` keeps a property out of both reading and writing.

## ASP.NET Core

- The MVC input formatter and `ReadFromToonAsync` reject bodies over 4 MB with `413 Payload Too Large`, also when the
  body is compressed (the limit counts decompressed bytes). For larger bodies, raise the limit of one endpoint
  (`[RequestSizeLimit]` on the action or the handler, `.WithMetadata(new RequestSizeLimitAttribute(...))`), not of the
  whole application.
- A TOON body takes several times its size in memory as text and objects; keep limits close to what the endpoint needs.
- Errors in a body are model-state errors (400) in MVC. In minimal APIs, `ReadFromToonAsync` throws: catch
  `ToonException` and return `Results.BadRequest()`, so a bad body does not become a 500 with an error log.
- The output formatter writes a `charset` only for UTF-8 and UTF-16; it never copies the requested text into the
  response. If responses are cached while the content type is negotiated, send `Vary: Accept`; and send
  `X-Content-Type-Options: nosniff` (from a middleware or the reverse proxy), as for any API.

## Paths, Rules and Patterns From Untrusted Sources

`ToonPathOptions` bound every evaluation: the length and nesting of a path, the length of regular expression patterns,
a budget of steps (a million plus 16 per value of the document, or per line when evaluating text) and a timeout of 10
seconds. Regular expressions run on .NET's non-backtracking engine and spend steps for the size of their automaton
before they run, so a large pattern on a long text stops at the budget. Use stricter options for paths and rules that
clients send:

```csharp
var strict = new ToonPathOptions
{
    MaxLength = 256, MaxNesting = 8, MaxPatternLength = 64,
    MaxSteps = 100_000, MaxStepsPerValue = 4, Timeout = TimeSpan.FromMilliseconds(200),
    StandardFunctionsOnly = true
};
var path = ToonPath.Parse(clientPath, strict);
```

Rule messages put string values in quotes, escaped and cut to 64 characters, so a value cannot add lines or forged
violations to a message that is logged or sent back to a model. See [Querying](../core-features/querying#paths-from-untrusted-input)
and [Rules and Validation](../core-features/validation).

## Answers of Language Models

`ToonPrompt.Deserialize` reads leniently (non-strict mode) but keeps the depth limits. Treat an answer like any
untrusted document: validate it with a rule set (`ToonPrompt.DeserializeValidated`) before acting on it, and do not
put values from it into commands, queries or HTML without the usual encoding. Text a model returns can try to steer
what your code asks next (prompt injection): do not send secrets or other users' data to a model with untrusted text.

## JSON and YAML

- JSON conversion keeps the depth limit and rejects duplicate keys in strict mode.
- YAML reading limits nesting, the nodes aliases expand to (`MaxAliasExpansionNodes`, 100,000) and the characters they
  copy (`MaxAliasExpansionChars`, 1,000,000 or ten times the document), rejects recursive aliases, and creates no
  types from tags. Keys that differ only in their tag are duplicates.
- `ToYaml` quotes strings that YAML 1.1 readers would read as another type, a merge key or a value key.

## Settings That Loosen the Limits

| Setting | Use it only when |
|---------|------------------|
| `StrictMode = false` | The input is your own (migrations, hand-written files): duplicate keys then keep the last value |
| `AllowExtendedLimits = true` (depth up to 1,000) | Documents are trusted and really that deep |
| `SerializeRuntimeTypes = true` | Derived types hold nothing private |
| `RespectRequiredMembers = false` | Missing members may keep their defaults |
| `[ToonInclude]` on a non-public setter | Any client may set that property |
| `ToonPathOptions` with larger budgets or `Timeout.InfiniteTimeSpan` | Paths, rules and documents are trusted |
| Larger request size limits | The endpoint needs them; set them per endpoint |

## Logging Errors

Exception messages of ToonNet hold positions, paths and types, not values. `ToonSerializationException.Value` and
`ToonEncodingException.ProblematicValue` do hold the value: loggers that write all properties of an exception
(destructuring) log them, so leave them out of logs that must not hold personal data.

## Known Limits

- Dictionaries with integer or `Guid` keys use .NET's hashing of those keys, which is not randomized: many keys chosen
  to collide make reading slow (seconds for a few megabytes), as with System.Text.Json. Limit the size of such
  documents, or read them into dictionaries with string keys.
- A document that fails direct reading is read again through the tree to report the error, so invalid documents cost
  about twice as much to reject.

## See Also

- [Configuration](../core-features/configuration): all options
- [Migrating to 4.0](../getting-started/migration-4): the defaults that changed
- [Input Formatters](../aspnet-core/input-formatters): request size limits
