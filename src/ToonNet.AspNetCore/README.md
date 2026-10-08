# ToonNet.AspNetCore


**ASP.NET Core integration for ToonNet serialization**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/ToonNet.AspNetCore.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.AspNetCore/)
[![Downloads](https://img.shields.io/nuget/dt/ToonNet.AspNetCore.svg?style=flat)](https://www.nuget.org/packages/ToonNet.AspNetCore/)
[![Status](https://img.shields.io/badge/status-stable-success)](#)

---

## 📦 What is ToonNet.AspNetCore?

ToonNet.AspNetCore provides **seamless integration** of ToonNet serialization with ASP.NET Core:

- ✅ **Dependency Injection** - Register `ToonEncoder` and options
- ✅ **Configuration Binding** - Load settings from appsettings.json
- ✅ **Options Validation** - Fail-fast on invalid configuration
- ✅ **TOON Configuration Provider** - Read TOON files as configuration source
- ✅ **MVC Ready** - Foundation for the `ToonNet.AspNetCore.Mvc` formatters

**Perfect for:**
- 🌐 **Web APIs** - Shared options for TOON responses (formatters in `ToonNet.AspNetCore.Mvc`)
- ⚙️ **Configuration** - Load TOON config files
- 🔧 **DI Integration** - Inject `ToonEncoder` and options into services
- 📊 **Options Pattern** - Configure ToonNet via appsettings.json

---

## 🚀 Quick Start

### Installation

```bash
# Core package (required)
dotnet add package ToonNet.Core

# ASP.NET Core integration
dotnet add package ToonNet.AspNetCore

# For MVC formatters (optional)
dotnet add package ToonNet.AspNetCore.Mvc
```

### Basic Setup - Default Options

```csharp
using ToonNet.AspNetCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register ToonNet services with default options
builder.Services.AddToonNet();

var app = builder.Build();
app.Run();
```

This registers:
- `ToonEncoder` (singleton)
- `ToonOptions` (IOptions<ToonOptions>)
- `ToonSerializerOptions` (IOptions<ToonSerializerOptions>)

---

## ⚙️ Configuration

### Using appsettings.json (Recommended)

**appsettings.json:**
```json
{
  "ToonNet": {
    "ToonOptions": {
      "IndentSize": 2,
      "MaxDepth": 100,
      "Delimiter": ",",
      "StrictMode": true,
      "AllowExtendedLimits": false
    },
    "ToonSerializerOptions": {
      "IncludeReadOnlyProperties": false,
      "MaxDepth": 100,
      "AllowExtendedLimits": false
    }
  }
}
```

Only the keys shown in the tables below are read. `ToonSerializerOptions` also accepts `IgnoreNullValues` and
`PropertyNamingPolicy` (`Default`, `CamelCase`, `SnakeCase`, `LowerCase`). Use a different root section name with
`AddToonNet(builder.Configuration, "MySection")`.

**Program.cs:**
```csharp
using ToonNet.AspNetCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration from appsettings.json
builder.Services.AddToonNet(builder.Configuration);

var app = builder.Build();
app.Run();
```

### Using Delegate Configuration

```csharp
builder.Services.AddToonNet(toonOptions =>
{
    toonOptions.IndentSize = 4;
    toonOptions.MaxDepth = 50;
}, serializerOptions =>
{
    serializerOptions.IncludeReadOnlyProperties = false;
    serializerOptions.MaxDepth = 100;
});
```

### Hybrid Approach (Configuration + Delegate)

```csharp
// Load from config, then override specific values (Configure runs after the configuration binding)
builder.Services.AddToonNet(builder.Configuration);

builder.Services.Configure<ToonOptions>(toonOptions =>
{
    toonOptions.IndentSize = 4;
});

builder.Services.Configure<ToonSerializerOptions>(serializerOptions =>
{
    serializerOptions.IncludeReadOnlyProperties = true;
});
```

---

## 📖 Configuration Options

### ToonOptions

Controls TOON format encoding behavior:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `IndentSize` | `int` | `2` | Number of spaces per indentation level (even, 2-100) |
| `MaxDepth` | `int` | `100` | Maximum nesting depth when encoding **and parsing**; deeper input is rejected with `ToonParseException` |
| `Delimiter` | `char` | `,` | Document delimiter: `,`, tab or `\|` (configuration also accepts `comma`, `tab`, `pipe`) |
| `StrictMode` | `bool` | `true` | Enable strict parsing rules (e.g. array length checks) |
| `AllowExtendedLimits` | `bool` | `false` | Allow `MaxDepth` above 200 (up to 1000) |

### ToonSerializerOptions

Controls C# object serialization behavior:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `IgnoreNullValues` | `bool` | `false` | Skip properties whose value is `null` |
| `IncludeReadOnlyProperties` | `bool` | `true` | Include read-only properties when serializing |
| `PropertyNamingPolicy` | `PropertyNamingPolicy` | `Default` | Key naming: `Default`, `CamelCase`, `SnakeCase`, `LowerCase` |
| `MaxDepth` | `int` | `100` | Maximum object graph depth |
| `AllowExtendedLimits` | `bool` | `false` | Allow `MaxDepth` above 200 (up to 1000) |

> **Untrusted input:** parsing enforces `ToonOptions.MaxDepth` and also checks the remaining stack space, so hostile, deeply nested
> input fails with a `ToonParseException` instead of crashing the process. Keep `AllowExtendedLimits` off for input you don't control.

---

## 🎯 Usage Patterns

### Pattern 1: Inject ToonEncoder

`ToonEncoder` is registered as a singleton with the configured `ToonOptions`. There is no injectable parser; parse
with `ToonDocument.Parse` and the injected options:

```csharp
using Microsoft.Extensions.Options;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

public class ToonService
{
    private readonly ToonEncoder _encoder;
    private readonly ToonOptions _options;
    private readonly ILogger<ToonService> _logger;

    public ToonService(
        ToonEncoder encoder,
        IOptions<ToonOptions> options,
        ILogger<ToonService> logger)
    {
        _encoder = encoder;
        _options = options.Value;
        _logger = logger;
    }

    public ToonDocument ParseToon(string toonString)
    {
        try
        {
            return ToonDocument.Parse(toonString, _options);
        }
        catch (ToonParseException ex)
        {
            _logger.LogError(ex, "Failed to parse TOON");
            throw;
        }
    }

    public string EncodeToon(ToonDocument document)
    {
        return _encoder.Encode(document);
    }
}

// Register service
builder.Services.AddScoped<ToonService>();
```

### Pattern 2: Inject Options

```csharp
using Microsoft.Extensions.Options;
using ToonNet.Core;
using ToonNet.Core.Serialization;

public class ConfigAnalyzer
{
    private readonly ToonOptions _toonOptions;
    private readonly ToonSerializerOptions _serializerOptions;

    public ConfigAnalyzer(
        IOptions<ToonOptions> toonOptions,
        IOptions<ToonSerializerOptions> serializerOptions)
    {
        _toonOptions = toonOptions.Value;
        _serializerOptions = serializerOptions.Value;
    }

    public void LogConfiguration()
    {
        Console.WriteLine($"Indent Size: {_toonOptions.IndentSize}");
        Console.WriteLine($"Max Depth: {_toonOptions.MaxDepth}");
        Console.WriteLine($"Include Read-Only: {_serializerOptions.IncludeReadOnlyProperties}");
    }
}
```

### Pattern 3: Use with ToonSerializer

```csharp
using ToonNet.Core.Serialization;
using Microsoft.Extensions.Options;

public class DataService
{
    private readonly ToonSerializerOptions _options;

    public DataService(IOptions<ToonSerializerOptions> options)
    {
        _options = options.Value;
    }

    public string SerializeData<T>(T data)
    {
        // Use configured options
        return ToonSerializer.Serialize(data, _options);
    }

    public T DeserializeData<T>(string toonString)
    {
        return ToonSerializer.Deserialize<T>(toonString, _options);
    }
}
```

---

## 📂 TOON Configuration Provider

Load TOON files as ASP.NET Core configuration sources:

### Create TOON Configuration File

**appsettings.toon:**
```toon
Database:
  ConnectionString: Server=localhost;Database=mydb
  Timeout: 30
  EnableRetry: true

Logging:
  Level: Information
  Console:
    Enabled: true
  File:
    Path: logs/app.log
    MaxSize: 10485760

Features:
  EnableCache: true
  CacheExpiry: 3600
```

### Register TOON Configuration Provider

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add TOON file as configuration source (using ToonNet.AspNetCore.Configuration;)
builder.Configuration.AddToonFile("appsettings.toon", optional: false, reloadOnChange: true);

// Register ToonNet services
builder.Services.AddToonNet(builder.Configuration);

var app = builder.Build();

// Access configuration
var connectionString = builder.Configuration["Database:ConnectionString"];
var logLevel = builder.Configuration["Logging:Level"];
```

### Configuration Provider Features

```csharp
// Multiple TOON files
builder.Configuration
    .AddToonFile("appsettings.toon")
    .AddToonFile($"appsettings.{builder.Environment.EnvironmentName}.toon", optional: true);

// With environment variables
builder.Configuration
    .AddToonFile("config.toon")
    .AddEnvironmentVariables();

// Reload on change
builder.Configuration.AddToonFile(
    "settings.toon",
    optional: false,
    reloadOnChange: true  // Auto-reload when file changes
);

// Custom parsing options (e.g. non-strict mode)
builder.Configuration.AddToonFile("legacy.toon", optional: true, reloadOnChange: false,
    options: new ToonOptions { StrictMode = false });
```

Nested objects become `Section:Key` paths and array items become index keys (`Items:0`, `Items:1`). Values are stored as
strings: booleans become `True`/`False`, and numbers are converted through `double`, so `1.0` becomes `1` and integers
beyond 2^53 lose precision; quote such values to keep them as written. A file that is not valid TOON throws a
`FormatException`.

---

## ✅ Validation

ToonNet.AspNetCore uses **Options Validation** to ensure configuration is valid:

### Automatic Validation

```csharp
// Validation happens at startup
builder.Services.AddToonNet(builder.Configuration);

// If configuration is invalid, app will fail to start with clear error message
```

### Custom Validation

`AddToonNet` returns the `IServiceCollection`; add extra rules through the options builder:

```csharp
builder.Services.AddToonNet(builder.Configuration);

builder.Services.AddOptions<ToonOptions>()
    .Validate(options => options.IndentSize <= 8, "IndentSize must not exceed 8")
    .ValidateOnStart();
```

### Validation Rules (Built-in)

- `IndentSize`: Must be an even number between 2 and 100
- `MaxDepth`: Must be 1-200 (or 1-1000 if `AllowExtendedLimits = true`)
- `Delimiter`: Must be comma, tab or pipe

---

## 🔒 Thread-Safety

- `ToonSerializer` methods are safe to call concurrently across threads.
- Shared metadata/name caches use `ConcurrentDictionary` for concurrent access.
- Cache entries are created on demand and retained for the process lifetime (no eviction).
- Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.

---

## 🔗 Related Packages

**Core:**
- [`ToonNet.Core`](../ToonNet.Core) - Core serialization (required)

**Extensions:**
- [`ToonNet.Extensions.Json`](../ToonNet.Extensions.Json) - JSON ↔ TOON conversion
- [`ToonNet.Extensions.Yaml`](../ToonNet.Extensions.Yaml) - YAML ↔ TOON conversion

**Web Integration:**
- [`ToonNet.AspNetCore.Mvc`](../ToonNet.AspNetCore.Mvc) - MVC input/output formatters

**Development:**
- [`ToonNet.Demo`](../../demo/ToonNet.Demo) - Sample applications
- [`ToonNet.Tests`](../../tests/ToonNet.Tests) - Test suite

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete ToonNet guide
- [API Guide](../../docs/API-GUIDE.md) - Detailed API reference
- [Samples](../../demo/ToonNet.Demo/Samples) - Real-world examples

---

## 🧪 Testing

```bash
# Run ASP.NET Core integration tests
dotnet test tests/ToonNet.Tests --filter "FullyQualifiedName~ToonNet.Tests.AspNetCore"
```

---

## 📋 Requirements

- .NET 8.0 or later
- ASP.NET Core 8.0+
- ToonNet.Core

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

## 🤝 Contributing

Contributions welcome! Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

---

**Part of the [ToonNet](../../README.md) serialization library family.**
