# Installation

## NuGet Packages

ToonNet is distributed as a set of NuGet packages. Install the packages you need based on your requirements.

### Core Package (Required)

The core package provides TOON serialization and deserialization functionality.

```bash
dotnet add package ToonNet.Core
```

```xml
<PackageReference Include="ToonNet.Core" Version="1.0.0" />
```

### Format Extensions (Optional)

#### JSON Integration

Convert between JSON and TOON formats.

```bash
dotnet add package ToonNet.Extensions.Json
```

```xml
<PackageReference Include="ToonNet.Extensions.Json" Version="1.0.0" />
```

#### YAML Integration

Convert between YAML and TOON formats.

```bash
dotnet add package ToonNet.Extensions.Yaml
```

```xml
<PackageReference Include="ToonNet.Extensions.Yaml" Version="1.0.0" />
```

### ASP.NET Core Integration (Optional)

#### Configuration Provider

Use TOON files as configuration sources in ASP.NET Core.

```bash
dotnet add package ToonNet.AspNetCore
```

```xml
<PackageReference Include="ToonNet.AspNetCore" Version="1.0.0" />
```

#### MVC Formatters

Enable TOON format for HTTP requests and responses.

```bash
dotnet add package ToonNet.AspNetCore.Mvc
```

```xml
<PackageReference Include="ToonNet.AspNetCore.Mvc" Version="1.0.0" />
```

## Requirements

- **.NET 8.0 or later**
- **C# 12.0 or later** (for source generators)

## Quick Package Selection Guide

| Scenario | Required Packages |
|----------|------------------|
| Basic TOON serialization | `ToonNet.Core` |
| JSON ↔ TOON conversion | `ToonNet.Core` + `ToonNet.Extensions.Json` |
| YAML ↔ TOON conversion | `ToonNet.Core` + `ToonNet.Extensions.Yaml` |
| ASP.NET Core API with TOON | `ToonNet.Core` + `ToonNet.AspNetCore.Mvc` |
| TOON config files | `ToonNet.Core` + `ToonNet.AspNetCore` |
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
