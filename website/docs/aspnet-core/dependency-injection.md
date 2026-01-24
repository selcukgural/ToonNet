# Dependency Injection

Configure ToonNet services in ASP.NET Core applications.

## Installation

```bash
dotnet add package ToonNet.AspNetCore
```

## Basic Setup

### Minimal API

```csharp
using ToonNet.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add ToonNet services
builder.Services.AddToonNet();

var app = builder.Build();
```

### MVC/Controllers

```csharp
using ToonNet.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddToonNet();

var app = builder.Build();
app.MapControllers();
app.Run();
```

## Configuration Options

```csharp
builder.Services.AddToonNet(options =>
{
    options.SerializerOptions = new ToonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
        IgnoreNullValues = true
    };
});
```

## Using Injected Services

```csharp
public class DataService
{
    private readonly IToonSerializer _serializer;
    
    public DataService(IToonSerializer serializer)
    {
        _serializer = serializer;
    }
    
    public string SerializeData(MyData data)
    {
        return _serializer.Serialize(data);
    }
}
```

## See Also

- **[Input Formatters](input-formatters)**: Handle TOON requests
- **[Output Formatters](output-formatters)**: Return TOON responses
- **[Configuration Provider](configuration-provider)**: TOON config files
