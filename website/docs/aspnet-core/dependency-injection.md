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
  is the registered `ToonOptions` unless your serializer options delegate, `AddToonFormatters` or
  `Configure<ToonSerializerOptions>` sets it to another instance, in any registration order)
- `ToonEncoder` as a singleton, created with the registered `ToonOptions` (the encoder keeps no per-call state, so
  sharing it across requests is safe)

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

To configure only the serializer options:

```csharp
builder.Services.AddToonNet(options => options.PropertyNamingPolicy = PropertyNamingPolicy.CamelCase);
```

Or bind them from configuration (section `ToonNet` by default, with `ToonOptions` and `ToonSerializerOptions`
subsections):

```csharp
builder.Services.AddToonNet(builder.Configuration);
```

The `ToonOptions` subsection reads `IndentSize`, `MaxDepth`, `Delimiter`, `StrictMode` and `AllowExtendedLimits`; the
`ToonSerializerOptions` subsection reads `IgnoreNullValues`, `IncludeReadOnlyProperties`, `PropertyNamingPolicy`,
`PropertyNameCaseInsensitive`, `UnmappedMemberHandling`, `RespectRequiredMembers` (default `true`),
`SerializeRuntimeTypes` (default `false`), `MaxDepth` and `AllowExtendedLimits`:

```json
{
  "ToonNet": {
    "ToonSerializerOptions": {
      "PropertyNamingPolicy": "CamelCase",
      "SerializeRuntimeTypes": false
    }
  }
}
```

`ToonNet:ToonSerializerOptions:SerializeRuntimeTypes = true` writes objects with their runtime type, so members of
derived types reach responses through properties of narrower types; leave it off unless you need the behavior of
ToonNet before 4.0 (see [Security](../advanced/security)).

Invalid values (for example a `MaxDepth` above 200 without `AllowExtendedLimits`, or an `IndentSize` outside 1–100) make
the application fail at startup. Calling `AddToonNet` more than once is safe: the delegates add up and the services
are registered once. Enum values are names in any case (`camelCase`); numbers and comma-separated lists are rejected.

The options are read once, when they are first used: the formatters, `ToonEncoder` and `IOptions<T>` keep them for the
lifetime of the application, and `IOptionsMonitor<T>` does not reload them when the configuration changes. Restart the
application to apply new values.

`IndentSize` is the indentation written and also the indentation expected when reading: in strict mode a request
body indented by another number of spaces is rejected.

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
