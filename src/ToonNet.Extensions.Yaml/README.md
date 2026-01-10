# ToonNet.Extensions.Yaml

YAML interoperability extension for ToonNet. Provides seamless bidirectional conversion between YAML and TOON formats.

## 📦 Installation

```bash
dotnet add package ToonNet.Extensions.Yaml
```

## 🚀 Quick Start

### YAML → TOON

```csharp
using ToonNet.Extensions.Yaml;

var yaml = """
name: Alice
age: 30
tags:
  - dev
  - admin
settings:
  theme: dark
  notifications: true
""";

// Convert YAML to TOON
var toonDoc = ToonYamlConverter.FromYaml(yaml);

// Access data
var root = (ToonObject)toonDoc.Root;
var name = ((ToonString)root["name"]).Value; // "Alice"
var age = ((ToonNumber)root["age"]).Value;   // 30
```

### TOON → YAML

```csharp
using ToonNet.Core.Models;
using ToonNet.Extensions.Yaml;

// Create TOON document
var profile = new ToonObject
{
    ["bio"] = new ToonString("Software Engineer"),
    ["age"] = new ToonNumber(35)
};

var user = new ToonObject
{
    ["name"] = new ToonString("Bob"),
    ["profile"] = profile
};

var doc = new ToonDocument(user);

// Convert to YAML
var yaml = ToonYamlConverter.ToYaml(doc);
```

## 🔄 Round-trip Conversion

```csharp
// YAML → TOON → YAML (preserves structure)
var originalYaml = """
user:
  name: Charlie
  id: 123
  roles:
    - admin
    - editor
""";

var toonDoc = ToonYamlConverter.FromYaml(originalYaml);
var newYaml = ToonYamlConverter.ToYaml(toonDoc);
```

## ✨ Features

- **Full YAML Support**: Objects, arrays, scalars, null values
- **Boolean Variants**: Supports `true/false`, `yes/no`, `on/off`
- **Number Formats**: Integers, floats, scientific notation
- **Nested Structures**: Deep object and array nesting
- **Round-trip Safe**: YAML → TOON → YAML preserves structure

## 🎯 Use Cases

- **Configuration Files**: Convert YAML configs to TOON format
- **CI/CD Integration**: Work with YAML-based workflows
- **Kubernetes**: Convert K8s manifests to TOON
- **Docker Compose**: Transform compose files
- **OpenAPI/Swagger**: Convert API specs

## 📋 Requirements

- .NET 8.0 or later
- ToonNet.Core
- YamlDotNet 16.2.0+

## 🔗 Related Packages

- **ToonNet.Core** - Core TOON parsing and encoding
- **ToonNet.Extensions.Json** - JSON interoperability

## 📚 Examples

### Complex YAML Document

```csharp
var yaml = """
apiVersion: v1
kind: Service
metadata:
  name: my-service
  labels:
    app: myapp
    tier: backend
spec:
  type: LoadBalancer
  ports:
    - port: 80
      targetPort: 8080
  selector:
    app: myapp
""";

var toonDoc = ToonYamlConverter.FromYaml(yaml);
// Now you can work with it in TOON format
```

### Cross-Format Conversion

```csharp
using ToonNet.Extensions.Json;
using ToonNet.Extensions.Yaml;

// YAML → TOON → JSON
var yaml = "name: Alice\nage: 30";
var toonDoc = ToonYamlConverter.FromYaml(yaml);
var json = ToonJsonConverter.ToJson(toonDoc);

// JSON → TOON → YAML
var jsonStr = """{"city":"Istanbul","country":"Turkey"}""";
var doc = ToonJsonConverter.FromJson(jsonStr);
var yamlOutput = ToonYamlConverter.ToYaml(doc);
```

## 📄 License

MIT
