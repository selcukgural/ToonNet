# Configuration Provider

Use TOON files as configuration sources in ASP.NET Core.

## Installation

```bash
dotnet add package ToonNet.AspNetCore
```

## Basic Usage

### Add TOON Configuration File

```csharp
using ToonNet.AspNetCore.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add TOON configuration file
builder.Configuration.AddToonFile("appsettings.toon", optional: false, reloadOnChange: true);

var app = builder.Build();
```

## Configuration File Example

**appsettings.toon:**
```toon
AppName: MyApplication
Version: 1.0.0
Database:
  ConnectionString: Server=localhost;Database=mydb
  MaxRetries: 3
  Timeout: 30
Logging:
  Level: Information
  EnableConsole: true
Features[3]: Authentication,Caching,Compression
```

Nested objects become `Section:Key` paths and array items become index keys (`Features:0`, `Features:1`, ...). An
empty object or array (`Features: []`) sets its key to null, which clears values from earlier sources (JSON
configuration files do so for an empty object, and set an empty array to `""`). Strict mode applies by default (pass a
`ToonOptions` as the last argument of `AddToonFile` to change it). A file that is not valid TOON, whose root is not an
object, or with two keys that differ only in case (configuration keys are case-insensitive) makes `Build()` throw an
`InvalidDataException` ("Failed to load configuration from file ...") with a `FormatException` that says why as its
`InnerException`, as the JSON provider does.

Values are stored as strings: booleans become `True`/`False`, and numbers are written in canonical TOON form (spec
§2): `12345678901234567890` stays as written, while `1.0` becomes `1` and `1.50` becomes `1.5`. A number with more
digits than a `decimal` holds (about 28) is rounded to the nearest `double`. Quote a value (`Version: "1.0"`) to keep
its text exactly. `AddToonFile` accepts both relative and absolute paths.

`AddToonFile(path)`, `AddToonFile(path, optional)` and `AddToonFile(path, optional, reloadOnChange)` read from the
application's file provider; `AddToonFile(provider, path, optional, reloadOnChange, toonOptions)` takes an
`IFileProvider` (a `PhysicalFileProvider` for another folder, an embedded or in-memory provider in tests). The
`ToonConfigurationSource` it adds has the `ToonOptions` the file is parsed with (`Options`), and builds a
`ToonConfigurationProvider`.

## Reading Configuration

```csharp
// Read simple values
string? appName = builder.Configuration["AppName"];
string? version = builder.Configuration["Version"];

// Read nested values
string? connectionString = builder.Configuration["Database:ConnectionString"];
int maxRetries = builder.Configuration.GetValue<int>("Database:MaxRetries");

// Bind to strongly-typed objects
var dbConfig = builder.Configuration.GetSection("Database").Get<DatabaseConfig>();
```

## Strongly-Typed Configuration

```csharp
using Microsoft.Extensions.Options;

// Register as options
builder.Services.Configure<DatabaseConfig>(
    builder.Configuration.GetSection("Database")
);

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = "";
    public int MaxRetries { get; set; }
    public int Timeout { get; set; }
}

// Use in services
public class DataService
{
    private readonly DatabaseConfig _config;
    
    public DataService(IOptions<DatabaseConfig> options)
    {
        _config = options.Value;
    }
}
```

## Environment-Specific Configuration

```csharp
builder.Configuration
    .AddToonFile("appsettings.toon", optional: false, reloadOnChange: true)
    .AddToonFile($"appsettings.{builder.Environment.EnvironmentName}.toon", 
        optional: true, reloadOnChange: true);
```

**appsettings.Development.toon:**
```toon
Database:
  ConnectionString: Server=localhost;Database=mydb_dev
Logging:
  Level: Debug
```

**appsettings.Production.toon:**
```toon
Database:
  ConnectionString: Server=prod-server;Database=mydb_prod
Logging:
  Level: Warning
```

## See Also

- **[Dependency Injection](dependency-injection)**: Service configuration
- **[Configuration](../core-features/configuration)**: ToonSerializerOptions
