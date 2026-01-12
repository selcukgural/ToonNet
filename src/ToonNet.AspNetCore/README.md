# ToonNet.AspNetCore


**ASP.NET Core integration for ToonNet serialization**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/ToonNet.AspNetCore.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.AspNetCore/)
[![Downloads](https://img.shields.io/nuget/dt/ToonNet.AspNetCore.svg?style=flat)](https://www.nuget.org/packages/ToonNet.AspNetCore/)
[![Status](https://img.shields.io/badge/status-stable-success)](#)

---

## 📦 What is ToonNet.AspNetCore?

ToonNet.AspNetCore provides **seamless integration** of ToonNet serialization with ASP.NET Core:

- ✅ **Dependency Injection** - Register ToonParser, ToonEncoder, and options
- ✅ **Configuration Binding** - Load settings from appsettings.json
- ✅ **Options Validation** - Fail-fast on invalid configuration
- ✅ **TOON Configuration Provider** - Read TOON files as configuration source
- ✅ **Middleware Ready** - Foundation for MVC formatters and middleware

**Perfect for:**
- 🌐 **Web APIs** - Serve TOON-formatted responses
- ⚙️ **Configuration** - Load TOON config files
- 🔧 **DI Integration** - Inject ToonParser/Encoder into services
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
using ToonNet.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register ToonNet services with default options
builder.Services.AddToon();

var app = builder.Build();
app.Run();
```

This registers:
- `ToonParser` (singleton)
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
      "MaxDepth": 64,
      "PreferInlineArrays": true,
      "PreferInlineObjects": false,
      "MaxInlineArrayLength": 80,
      "Delimiter": ",",
      "StrictMode": false,
      "AllowExtendedLimits": false
    },
    "ToonSerializerOptions": {
      "IncludeReadOnlyProperties": false,
      "MaxDepth": 64,
      "AllowExtendedLimits": false
    }
  }
}
```

**Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Bind configuration from appsettings.json
builder.Services.AddToon(builder.Configuration);

var app = builder.Build();
app.Run();
```

### Using Delegate Configuration

```csharp
builder.Services.AddToon(toonOptions =>
{
    toonOptions.IndentSize = 4;
    toonOptions.PreferInlineArrays = true;
    toonOptions.MaxDepth = 100;
}, serializerOptions =>
{
    serializerOptions.IncludeReadOnlyProperties = false;
    serializerOptions.MaxDepth = 100;
});
```

### Hybrid Approach (Configuration + Delegate)

```csharp
// Load from config + override specific values
builder.Services.AddToon(
    builder.Configuration,
    toonOptions =>
    {
        // Override specific settings
        toonOptions.IndentSize = 4;
    },
    serializerOptions =>
    {
        serializerOptions.IncludeReadOnlyProperties = true;
    }
);
```

---

## 📖 Configuration Options

### ToonOptions

Controls TOON format encoding behavior:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `IndentSize` | `int` | `2` | Number of spaces per indentation level |
| `MaxDepth` | `int` | `64` | Maximum nesting depth |
| `PreferInlineArrays` | `bool` | `true` | Use inline format for simple arrays |
| `PreferInlineObjects` | `bool` | `false` | Use inline format for simple objects |
| `MaxInlineArrayLength` | `int` | `80` | Max character length for inline arrays |
| `Delimiter` | `char` | `,` | Array item delimiter |
| `StrictMode` | `bool` | `false` | Enable strict parsing rules |
| `AllowExtendedLimits` | `bool` | `false` | Allow depths beyond 64 levels |

### ToonSerializerOptions

Controls C# object serialization behavior:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `IncludeReadOnlyProperties` | `bool` | `false` | Include read-only properties |
| `MaxDepth` | `int` | `64` | Maximum object graph depth |
| `AllowExtendedLimits` | `bool` | `false` | Allow depths beyond 64 levels |

---

## 🎯 Usage Patterns

### Pattern 1: Inject ToonParser/Encoder

```csharp
public class ToonService
{
    private readonly ToonParser _parser;
    private readonly ToonEncoder _encoder;
    private readonly ILogger<ToonService> _logger;

    public ToonService(
        ToonParser parser, 
        ToonEncoder encoder,
        ILogger<ToonService> logger)
    {
        _parser = parser;
        _encoder = encoder;
        _logger = logger;
    }

    public ToonDocument ParseToon(string toonString)
    {
        try
        {
            return _parser.Parse(toonString);
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

// Add TOON file as configuration source
builder.Configuration.AddToonFile("appsettings.toon", optional: false, reloadOnChange: true);

// Register ToonNet services
builder.Services.AddToon(builder.Configuration);

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
    .AddToonFile($"appsettings.{env}.toon", optional: true);

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
```

---

## ✅ Validation

ToonNet.AspNetCore uses **Options Validation** to ensure configuration is valid:

### Automatic Validation

```csharp
// Validation happens at startup
builder.Services.AddToon(builder.Configuration);

// If configuration is invalid, app will fail to start with clear error message
```

### Custom Validation

```csharp
builder.Services.AddToon(builder.Configuration)
    .Validate(options => 
    {
        if (options.IndentSize < 1 || options.IndentSize > 8)
            return false;
        return true;
    }, "IndentSize must be between 1 and 8");
```

### Validation Rules (Built-in)

- `IndentSize`: Must be 1-8
- `MaxDepth`: Must be 1-1024 (or 1-64 if AllowExtendedLimits=false)
- `MaxInlineArrayLength`: Must be 1-1024
- `Delimiter`: Must be a valid character

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
cd tests/ToonNet.Tests
dotnet test --filter "Category=AspNetCore"
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
