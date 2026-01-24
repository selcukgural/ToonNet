# YAML Integration

Convert between YAML and TOON formats using `ToonNet.Extensions.Yaml`.

## Installation

```bash
dotnet add package ToonNet.Extensions.Yaml
```

## ToonYamlConvert Class

Static utility class for YAML ↔ TOON conversion.

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

// Convert to ToonDocument
ToonDocument toonDoc = ToonYamlConvert.FromYaml(yaml);

// Convert to TOON string
string toonString = toonDoc.ToString();
```

### TOON to YAML Conversion

```csharp
string toonString = """
name: Alice
age: 30
address:
  city: New York
  zip: 10001
""";

ToonDocument toonDoc = ToonDocument.Parse(toonString);
string yaml = ToonYamlConvert.ToYaml(toonDoc);
```

## ToonYamlConverter Class

Bidirectional converter:

```csharp
using ToonNet.Extensions.Yaml;

// YAML → TOON
string yaml = File.ReadAllText("config.yaml");
ToonDocument toonDoc = ToonYamlConverter.FromYaml(yaml);

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

## Complete Examples

### Configuration File Migration

```csharp
// Read existing YAML config
string yaml = File.ReadAllText("appsettings.yaml");

// Convert to TOON
ToonDocument toonDoc = ToonYamlConvert.FromYaml(yaml);
string toonString = toonDoc.ToString();

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
ToonDocument toonDoc = ToonYamlConvert.FromYaml(originalYaml);

// Back to YAML
string convertedYaml = ToonYamlConvert.ToYaml(toonDoc);
```

### Docker Compose to TOON

```csharp
string dockerCompose = File.ReadAllText("docker-compose.yaml");
ToonDocument toonDoc = ToonYamlConvert.FromYaml(dockerCompose);

// Now work with TOON API
var services = (ToonObject)toonDoc.Root["services"];
foreach (var service in services)
{
    Console.WriteLine($"Service: {service.Key}");
}
```

## Key Methods Summary

| Method | Description |
|--------|-------------|
| `ToonYamlConvert.FromYaml(string)` | YAML → ToonDocument |
| `ToonYamlConvert.ToYaml(ToonDocument)` | ToonDocument → YAML |
| `ToonYamlConverter.FromYaml(string)` | YAML string → ToonDocument |
| `ToonYamlConverter.ToYaml(ToonDocument)` | ToonDocument → YAML string |

## Use Cases

1. **Configuration Migration**: Convert YAML configs to TOON
2. **DevOps Tools**: Work with YAML-based tools (Docker, Kubernetes, etc.)
3. **Format Conversion**: Bi-directional YAML ↔ TOON transformation
4. **CI/CD Pipelines**: Transform pipeline configs
5. **Interoperability**: Bridge YAML and TOON ecosystems

## See Also

- **[JSON Integration](json-integration)**: Convert JSON ↔ TOON
- **[Custom Formats](custom-formats)**: Create custom converters
