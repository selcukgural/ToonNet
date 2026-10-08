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
  `ToonYamlConverter.MaxAliasExpansionNodes` (100,000) nodes throws a `YamlException`.

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
| `ToonYamlConvert.ToYaml(string)` | TOON string → YAML string |
| `ToonYamlConverter.FromYaml(string)` | YAML string → ToonDocument |
| `ToonYamlConverter.FromYaml(string, ToonOptions?)` | Same, with a custom `MaxDepth` |
| `ToonYamlConverter.ToYaml(ToonDocument)` | ToonDocument → YAML string |
| `ToonYamlConverter.ToYaml(ToonValue)` | ToonValue → YAML string |

## Conversion Notes

- Only the first document of a multi-document YAML stream (`---`) is converted.
- Quoted and block scalars are always strings (`"42"` stays a string). Plain scalars are typed by their text:
  `true`/`false` (also `yes`/`no`, `on`/`off`, in lowercase, capitalized or uppercase), `null`/`~`/empty, and numbers
  in JSON number grammar (optional leading `+`, exact value when it fits in a `decimal`). Everything else, such as
  `007`, `1,000`, `0xFF` or `.inf`, stays a string.
- `ToonYamlConvert.ToYaml` quotes strings that YAML would otherwise read as another type, so conversions round-trip,
  and accepts `ToonOptions` for parsing the TOON input.
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
