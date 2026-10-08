# Output Formatters

Return TOON format in HTTP responses using MVC output formatters.

## Installation

```bash
dotnet add package ToonNet.AspNetCore.Mvc
```

## Setup

```csharp
using ToonNet.AspNetCore.Mvc.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddToonFormatters();

var app = builder.Build();
app.MapControllers();
app.Run();
```

## Using in Controllers

### Return TOON Data

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    [Produces("application/toon")]
    public IActionResult GetUser(int id)
    {
        var user = new User
        {
            Id = id,
            Name = "Alice",
            Email = "alice@example.com"
        };
        
        return Ok(user);  // Automatically serialized to TOON
    }
}
```

### Response Example

```http
HTTP/1.1 200 OK
Content-Type: application/toon; charset=utf-8

Id: 1
Name: Alice
Email: alice@example.com
```

## Using ToonResult

Alternative approach using `ToonResult` (an `IResult`), which always writes TOON regardless of the `Accept` header.
It works in Minimal APIs and in controller actions that return `IResult`:

```csharp
using ToonNet.AspNetCore.Mvc.Http;

[HttpGet("{id}")]
public IResult GetUser(int id)
{
    var user = GetUserFromDatabase(id);
    return new ToonResult(user);
}

// Minimal API: extension method on Results.Extensions
app.MapGet("/users/{id}", (int id) => Results.Extensions.Toon(GetUserFromDatabase(id)));
```

`ToonResult` uses the options passed to it, otherwise the application's `IOptions<ToonSerializerOptions>`. The MVC
formatters use the same `IOptions<ToonSerializerOptions>`: `AddToonNet(...)`, `services.Configure<ToonSerializerOptions>(...)`
and the delegate passed to `AddToonFormatters` all configure that one instance.

## Content Negotiation

Client specifies desired format via `Accept` header:

```http
GET /api/users/1 HTTP/1.1
Accept: application/toon
```

The output formatter only produces `application/toon` (`text/toon` is accepted by the input formatter only). It is
added after the JSON formatter, so JSON stays the default for `Accept: */*` or a missing `Accept` header; use
`[Produces("application/toon")]` to always return TOON from an action.

## See Also

- **[Input Formatters](input-formatters)**: Handle TOON requests
- **[Dependency Injection](dependency-injection)**: Service configuration
