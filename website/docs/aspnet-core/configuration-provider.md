# Configuration Provider

Use TOON files as configuration sources in ASP.NET Core.

## Installation

```bash
dotnet add package ToonNet.AspNetCore
```

## Basic Usage

### Add TOON Configuration File

```csharp
using ToonNet.AspNetCore;

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
Features:
  - Authentication
  - Caching
  - Compression
```

## Reading Configuration

```csharp
// Read simple values
string appName = builder.Configuration["AppName"];
string version = builder.Configuration["Version"];

// Read nested values
string connectionString = builder.Configuration["Database:ConnectionString"];
int maxRetries = builder.Configuration.GetValue<int>("Database:MaxRetries");

// Bind to strongly-typed objects
var dbConfig = builder.Configuration.GetSection("Database").Get<DatabaseConfig>();
```

## Strongly-Typed Configuration

```csharp
public class DatabaseConfig
{
    public string ConnectionString { get; set; }
    public int MaxRetries { get; set; }
    public int Timeout { get; set; }
}

// Register as options
builder.Services.Configure<DatabaseConfig>(
    builder.Configuration.GetSection("Database")
);

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
