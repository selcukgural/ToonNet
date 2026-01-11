# ToonNet.Extensions.Yaml

**YAML ↔ TOON format conversion extension**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Package](https://img.shields.io/badge/package-ToonNet.Extensions.Yaml-blue)](#)
[![Status](https://img.shields.io/badge/status-stable-success)](#)

---

## 📦 What is ToonNet.Extensions.Yaml?

ToonNet.Extensions.Yaml provides **seamless bidirectional conversion** between YAML and TOON formats:

- ✅ **YAML → TOON** - Convert YAML strings/documents to TOON format
- ✅ **TOON → YAML** - Convert TOON strings/documents to YAML format
- ✅ **YamlDotNet integration** - Industry-standard YAML parser
- ✅ **Preserves structure** - Round-trip conversions maintain data integrity
- ✅ **Full YAML support** - Objects, arrays, scalars, anchors, aliases

**Perfect for:**
- ⚙️ **Configuration Files** - Convert YAML configs to TOON format
- 🐳 **Docker/Kubernetes** - Work with container configurations
- 🔄 **CI/CD** - Transform GitHub Actions, GitLab CI, etc.
- 📋 **OpenAPI/Swagger** - Convert API specifications
- 🔗 **Cross-format** - YAML → TOON → JSON workflows

---

## 🚀 Quick Start

### Installation

```bash
# Core package (required)
dotnet add package ToonNet.Core

# YAML extension
dotnet add package ToonNet.Extensions.Yaml
```

### Basic Usage - Document Conversion

```csharp
using ToonNet.Extensions.Yaml;

// YAML → TOON document
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

var toonDoc = ToonYamlConverter.FromYaml(yaml);

// Access data
var root = (ToonObject)toonDoc.Root;
var name = ((ToonString)root["name"]).Value; // "Alice"
var age = ((ToonNumber)root["age"]).Value;   // 30

// TOON → YAML document
var yamlOutput = ToonYamlConverter.ToYaml(toonDoc);
```

### String Format Conversion

```csharp
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Yaml;

// Load YAML config
string yamlConfig = File.ReadAllText("appsettings.yaml");

// Convert to TOON
var toonDoc = ToonYamlConverter.FromYaml(yamlConfig);
string toonString = new ToonEncoder().Encode(toonDoc);

// Work with TOON format...

// Convert back to YAML
var parser = new ToonParser();
var doc = parser.Parse(toonString);
string yamlOutput = ToonYamlConverter.ToYaml(doc);
```

---

## 📖 API Reference

### Document Conversion

```csharp
using ToonNet.Extensions.Yaml;

// YAML string → ToonDocument
ToonDocument doc = ToonYamlConverter.FromYaml(yamlString);

// ToonDocument → YAML string
string yaml = ToonYamlConverter.ToYaml(document);

// ToonValue → YAML string
string yaml = ToonYamlConverter.ToYaml(toonValue);
```

### Type Support

| YAML Type | TOON Type | Examples |
|-----------|-----------|----------|
| Mapping (object) | `ToonObject` | `key: value` |
| Sequence (array) | `ToonArray` | `- item1` |
| Scalar (string) | `ToonString` | `name: Alice` |
| Scalar (number) | `ToonNumber` | `age: 30` |
| Scalar (boolean) | `ToonBoolean` | `enabled: true` |
| Null | `ToonNull` | `value: null` |

---

## 🎯 Real-World Examples

### Example 1: Kubernetes Manifest Conversion

```csharp
using ToonNet.Extensions.Yaml;

// Load Kubernetes YAML
var k8sYaml = """
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

// Convert to TOON (more readable for analysis)
var toonDoc = ToonYamlConverter.FromYaml(k8sYaml);

// Work with TOON format
var root = (ToonObject)toonDoc.Root;
var serviceName = ((ToonString)root["metadata"]["name"]).Value;

// Convert back to YAML if needed
var modifiedYaml = ToonYamlConverter.ToYaml(toonDoc);
```

### Example 2: Docker Compose Transformation

```csharp
// Load Docker Compose YAML
var composeYaml = File.ReadAllText("docker-compose.yml");

// Convert to TOON
var toonDoc = ToonYamlConverter.FromYaml(composeYaml);

// Analyze or modify structure
var services = (ToonObject)((ToonObject)toonDoc.Root)["services"];

// Save as TOON for easier editing
File.WriteAllText("docker-compose.toon", 
    new ToonEncoder().Encode(toonDoc));
```

### Example 3: Cross-Format Conversion (YAML → JSON)

```csharp
using ToonNet.Extensions.Json;
using ToonNet.Extensions.Yaml;

// YAML → TOON → JSON
var yaml = """
database:
  host: localhost
  port: 5432
  credentials:
    username: admin
    password: secret
""";

var toonDoc = ToonYamlConverter.FromYaml(yaml);
var json = ToonJsonConverter.ToJson(toonDoc, writeIndented: true);

// JSON → TOON → YAML (reverse)
var jsonStr = """{"name":"Alice","tags":["dev","admin"]}""";
var doc = ToonJsonConverter.FromJson(jsonStr);
var yamlOutput = ToonYamlConverter.ToYaml(doc);
```

### Example 4: GitHub Actions Workflow

```csharp
// Load GitHub Actions workflow
var workflowYaml = """
name: CI
on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Build
        run: dotnet build
      - name: Test
        run: dotnet test
""";

// Convert to TOON for analysis
var toonDoc = ToonYamlConverter.FromYaml(workflowYaml);
var root = (ToonObject)toonDoc.Root;

// Access workflow structure
var jobs = (ToonObject)root["jobs"];
var buildJob = (ToonObject)jobs["build"];
var steps = (ToonArray)buildJob["steps"];

Console.WriteLine($"Workflow has {steps.Items.Count} steps");
```

---

## ✨ YAML Features Supported

### Boolean Variants
```yaml
# All supported
enabled: true
disabled: false
legacy_yes: yes
legacy_no: no
switch_on: on
switch_off: off
```

### Number Formats
```yaml
integer: 42
float: 3.14
scientific: 1.5e-10
hex: 0xFF
octal: 0o77
```

### Complex Structures
```yaml
# Nested objects
user:
  profile:
    settings:
      theme: dark

# Mixed arrays
items:
  - name: Item 1
    price: 9.99
  - name: Item 2
    price: 19.99

# Inline notation
tags: [dev, admin, user]
coords: {x: 10, y: 20}
```

---

## 🔄 Round-Trip Behavior

YAML → TOON → YAML conversions preserve structure:

```csharp
var originalYaml = """
user:
  name: Bob
  roles:
    - admin
    - editor
""";

var toonDoc = ToonYamlConverter.FromYaml(originalYaml);
var roundtripYaml = ToonYamlConverter.ToYaml(toonDoc);

// Structure preserved (formatting may differ)
```

**Note:** Comments and anchors/aliases are not preserved (YAML parser limitation).

---

## 🔗 Related Packages

**Core:**
- [`ToonNet.Core`](../ToonNet.Core) - Core serialization (required)

**Other Extensions:**
- [`ToonNet.Extensions.Json`](../ToonNet.Extensions.Json) - JSON ↔ TOON conversion

**Web Integration:**
- [`ToonNet.AspNetCore`](../ToonNet.AspNetCore) - ASP.NET Core middleware
- [`ToonNet.AspNetCore.Mvc`](../ToonNet.AspNetCore.Mvc) - MVC formatters

**Development:**
- [`ToonNet.Demo`](../../demo/ToonNet.Demo) - Sample applications
- [`ToonNet.Tests`](../../tests/ToonNet.Tests) - YAML conversion test suite

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete ToonNet guide
- [API Guide](../../docs/API-GUIDE.md) - Detailed API reference
- [Samples](../../demo/ToonNet.Demo/Samples) - Real-world examples

---

## 🧪 Testing

```bash
# Run YAML conversion tests
cd tests/ToonNet.Tests
dotnet test --filter "FullyQualifiedName~ToonYamlConverter"

# Run specific test categories
dotnet test --filter "Category=YamlConversion"
```

---

## 📋 Requirements

- .NET 8.0 or later
- ToonNet.Core
- YamlDotNet 16.3.0+

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

## 🤝 Contributing

Contributions welcome! Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

---

**Part of the [ToonNet](../../README.md) serialization library family.**

