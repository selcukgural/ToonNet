# Installation

## NuGet Packages

ToonNet is distributed as a set of NuGet packages. Install the packages you need based on your requirements.
`dotnet add package` adds the latest stable version; keep all ToonNet packages on the same version.

### Core Package (Required)

The core package provides TOON serialization and deserialization functionality.

```bash
dotnet add package ToonNet.Core
```

### Format Extensions (Optional)

#### JSON Integration

Convert between JSON and TOON formats.

```bash
dotnet add package ToonNet.Extensions.Json
```

#### YAML Integration

Convert between YAML and TOON formats.

```bash
dotnet add package ToonNet.Extensions.Yaml
```

### Source Generator (Optional)

Generates `Serialize`/`Deserialize` methods at compile time for `partial` types marked with
`[ToonSerializable]`. The generated code calls into `ToonNet.Core`, so install both packages.
See [Source Generators](../advanced/source-generators).

```bash
dotnet add package ToonNet.SourceGenerators
```

### ASP.NET Core Integration (Optional)

#### Configuration Provider

Use TOON files as configuration sources in ASP.NET Core.

```bash
dotnet add package ToonNet.AspNetCore
```

#### MVC Formatters

Enable TOON format for HTTP requests and responses.

```bash
dotnet add package ToonNet.AspNetCore.Mvc
```

## Requirements

- **.NET 8.0 or later** (the runtime packages build for `net8.0` and `net10.0`)
- Trimming and NativeAOT: see [Source Generators – NativeAOT and trimming](../advanced/source-generators#nativeaot-and-trimming)
- `ToonNet.Extensions.Yaml` depends on [YamlDotNet](https://github.com/aaubry/YamlDotNet) (installed automatically)

## Quick Package Selection Guide

| Scenario | Required Packages |
|----------|------------------|
| Basic TOON serialization | `ToonNet.Core` |
| JSON ↔ TOON conversion | `ToonNet.Core` + `ToonNet.Extensions.Json` |
| YAML ↔ TOON conversion | `ToonNet.Core` + `ToonNet.Extensions.Yaml` |
| ASP.NET Core API with TOON | `ToonNet.Core` + `ToonNet.AspNetCore.Mvc` |
| TOON config files | `ToonNet.Core` + `ToonNet.AspNetCore` |
| Compile-time generated serializers | `ToonNet.Core` + `ToonNet.SourceGenerators` |
| Full-featured setup | All packages |

## Verify Installation

After installing, verify the installation by checking the package references:

```bash
dotnet list package
```

You should see the ToonNet packages listed.

## Next Steps

- **[Quick Start](quick-start)**: Get started with your first TOON serialization
- **[Basic Serialization](basic-serialization)**: Learn the fundamentals
