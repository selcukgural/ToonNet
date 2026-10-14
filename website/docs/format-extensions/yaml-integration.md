# YAML Integration

Convert between YAML and TOON formats using `ToonNet.Extensions.Yaml`.

## Installation

```bash
dotnet add package ToonNet.Extensions.Yaml
```

## ToonYamlConvert Class

Static utility class for YAML ↔ TOON conversion. Its methods work on strings.

### YAML to TOON Conversion

```csharp
using ToonNet.Extensions.Yaml;

string yaml = """
name: Alice
age: 30
address:
  city: New York
  zip: 10001
""";

string toonString = ToonYamlConvert.FromYaml(yaml);
// Output:
// name: Alice
// age: 30
// address:
//   city: New York
//   zip: 10001
```

`FromYaml` also takes `ToonOptions` (encoding options and the `MaxDepth` limit described below).

### TOON to YAML Conversion

```csharp
string toonString = """
name: Alice
age: 30
address:
  city: New York
  zip: 10001
""";

string yaml = ToonYamlConvert.ToYaml(toonString);
```

## ToonYamlConverter Class

Bidirectional converter between YAML and `ToonDocument` / `ToonValue`:

```csharp
using ToonNet.Core.Models;
using ToonNet.Extensions.Yaml;

// YAML → TOON
string yamlInput = File.ReadAllText("config.yaml");
ToonDocument toonDoc = ToonYamlConverter.FromYaml(yamlInput);

// TOON → YAML
string toonInput = """
database:
  host: localhost
  port: 5432
  name: mydb
""";
ToonDocument doc = ToonDocument.Parse(toonInput);
string yaml = ToonYamlConverter.ToYaml(doc);
```

## Limits

`FromYaml` checks the input before loading it, so untrusted YAML cannot exhaust the stack or memory:

- Nesting deeper than `ToonOptions.MaxDepth` (default 100) throws a `YamlException`.
  Pass options to change it: `ToonYamlConverter.FromYaml(yaml, new ToonOptions { MaxDepth = 50 })`.
- Aliases (`*anchor`) are expanded into copies. Input whose aliases expand to more than
  `ToonYamlConverter.MaxAliasExpansionNodes` (100,000) nodes, or copy more than
  `ToonYamlConverter.MaxAliasExpansionChars` characters (1,000,000, or ten times the length of the YAML when that is
  more), throws a `YamlException`, so a small document cannot expand into a huge string.
- The nesting limit counts the expanded aliases too, and an alias inside the node its anchor marks (a recursive
  structure such as `&a [*a]`) throws a `YamlException`.

## Complete Examples

### Configuration File Migration

```csharp
// Read existing YAML config
string yaml = File.ReadAllText("appsettings.yaml");

// Convert to TOON
string toonString = ToonYamlConvert.FromYaml(yaml);

// Save as TOON config
File.WriteAllText("appsettings.toon", toonString);
```

### Bidirectional Transformation

```csharp
// YAML → TOON → YAML
string originalYaml = """
app:
  name: MyApp
  version: 1.0.0
features:
  - authentication
  - caching
""";

// To TOON
string toon = ToonYamlConvert.FromYaml(originalYaml);
// app:
//   name: MyApp
//   version: 1.0.0
// features[2]: authentication,caching

// Back to YAML
string convertedYaml = ToonYamlConvert.ToYaml(toon);
```

### Docker Compose to TOON

```csharp
string dockerCompose = File.ReadAllText("docker-compose.yaml");
ToonDocument toonDoc = ToonYamlConverter.FromYaml(dockerCompose);

// Now work with TOON API
var services = (ToonObject)toonDoc.AsObject()["services"]!;
foreach (var service in services.Properties)
{
    Console.WriteLine($"Service: {service.Key}");
}
```

## Key Methods Summary

| Method | Description |
|--------|-------------|
| `ToonYamlConvert.FromYaml(string, ToonOptions?)` | YAML string → TOON string |
| `ToonYamlConvert.ToYaml(string, ToonOptions?)` | TOON string → YAML string |
| `ToonYamlConverter.FromYaml(string)` | YAML string → ToonDocument |
| `ToonYamlConverter.FromYaml(string, ToonOptions?)` | Same, with a custom `MaxDepth` |
| `ToonYamlConverter.ToYaml(ToonDocument)` | ToonDocument → YAML string |
| `ToonYamlConverter.ToYaml(ToonValue)` | ToonValue → YAML string |

## Conversion Notes

- Only the first document of a multi-document YAML stream (`---`) is converted.
- Mapping keys must be scalars: a sequence or mapping used as a key throws a `YamlException`. An empty key is kept as
  the TOON key `""`.
- Duplicate keys in a mapping throw a `YamlException`, also keys that differ only in their tag (`a` and `!!str a`).
- Merge keys (`<<: *base`) are not expanded; `<<` stays an ordinary key.
- Quoted and block scalars (and scalars tagged `!!str`) are strings (`"42"` stays a string), unless an explicit
  core-schema tag says otherwise (`!!int "42"` is 42; a value that does not fit its tag, such as `!!int 1.5` or
  `!!null foo`, is a `YamlException`). Plain scalars are
  typed by the YAML 1.2 core schema: `true`/`false` (lowercase, capitalized or uppercase), `null`/`~`/empty, integers
  (`42`, `+5`, `007` is 7, `0xFF` is 255, `0o17` is 15) and floats (`1.5`, `.5`, `1e3`), exact when they fit in a
  `decimal`. Everything else is a string, including the YAML 1.1 booleans `yes`/`no`/`on`/`off` and `.inf`/`.nan`,
  which a TOON number cannot hold.
- `ToonYamlConvert.ToYaml` quotes strings that YAML would otherwise read as another type (also `yes`/`no`/`on`/`off`,
  `2001-12-14`, `12:30:00`, `0b101`, `1_000`, `<<`, `=` and text that starts with a byte order mark, for YAML 1.1
  readers), so conversions round-trip, and accepts `ToonOptions` for parsing the TOON input. Strings with a carriage
  return or a YAML 1.1 line break (NEL, U+2028, U+2029) are double-quoted, so they keep those characters. Values nested
  deeper than 200 levels (the most `MaxDepth` allows without extended limits) throw a `YamlException`, and the YAML
  ends with a line feed.
- Anchors and aliases are expanded into copies; comments are not preserved.

## Use Cases

1. **Configuration Migration**: Convert YAML configs to TOON
2. **DevOps Tools**: Work with YAML-based tools (Docker, Kubernetes, etc.)
3. **Format Conversion**: Bi-directional YAML ↔ TOON transformation
4. **CI/CD Pipelines**: Transform pipeline configs
5. **Interoperability**: Bridge YAML and TOON ecosystems

## See Also

- **[JSON Integration](json-integration)**: Convert JSON ↔ TOON
- **[Custom Formats](custom-formats)**: Create custom converters
