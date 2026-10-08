# Dependency Injection

Configure ToonNet services in ASP.NET Core applications.

## Installation

```bash
dotnet add package ToonNet.AspNetCore
```

## Basic Setup

### Minimal API

```csharp
using ToonNet.AspNetCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add ToonNet services
builder.Services.AddToonNet();

var app = builder.Build();
```

### MVC/Controllers

```csharp
using ToonNet.AspNetCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddToonNet();

var app = builder.Build();
app.MapControllers();
app.Run();
```

`AddToonNet()` registers options and services only; it does not add TOON formatters to MVC. To accept and return
TOON in controllers, add the formatters from `ToonNet.AspNetCore.Mvc` (see [Input Formatters](input-formatters)).

`AddToonNet` registers:

- `IOptions<ToonOptions>` and `IOptions<ToonSerializerOptions>` (validated at startup; `ToonSerializerOptions.ToonOptions`
  is set to the registered `ToonOptions`)
- `ToonEncoder` as a singleton, created with the registered `ToonOptions`

## Configuration Options

```csharp
using ToonNet.AspNetCore.DependencyInjection;
using ToonNet.Core.Serialization;

builder.Services.AddToonNet(
    toonOptions =>
    {
        toonOptions.IndentSize = 2;
        toonOptions.MaxDepth = 50;
    },
    serializerOptions =>
    {
        serializerOptions.PropertyNamingPolicy = PropertyNamingPolicy.CamelCase;
        serializerOptions.IgnoreNullValues = true;
    });
```

Or bind them from configuration (section `ToonNet` by default, with `ToonOptions` and `ToonSerializerOptions`
subsections):

```csharp
builder.Services.AddToonNet(builder.Configuration);
```

Invalid values (for example an odd `IndentSize` or a `MaxDepth` above 200 without `AllowExtendedLimits`) make the
application fail at startup.

## Using Injected Services

There is no injectable serializer interface; `ToonSerializer` is a static class. Inject the configured options and pass
them to it:

```csharp
using Microsoft.Extensions.Options;
using ToonNet.Core.Serialization;

public class DataService
{
    private readonly ToonSerializerOptions _options;

    public DataService(IOptions<ToonSerializerOptions> options)
    {
        _options = options.Value;
    }

    public string SerializeData(MyData data)
    {
        return ToonSerializer.Serialize(data, _options);
    }
}
```

## See Also

- **[Input Formatters](input-formatters)**: Handle TOON requests
- **[Output Formatters](output-formatters)**: Return TOON responses
- **[Configuration Provider](configuration-provider)**: TOON config files
