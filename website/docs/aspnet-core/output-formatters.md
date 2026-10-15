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

// The same with the constructor: value, options (null for the application's), status code
app.MapGet("/v2/users/{id}", (int id) => new ToonResult(GetUserFromDatabase(id), null, StatusCodes.Status200OK));

// With a status code (reading the TOON body: see Input Formatters, Minimal APIs)
app.MapPost("/users", async (HttpRequest request) =>
    Results.Extensions.Toon(Save((await request.ReadFromToonAsync<User>())!), StatusCodes.Status201Created));
```

`ToonResult` exposes `Value`, `StatusCode` and `ContentType` (`application/toon; charset=utf-8`), as the results of
`Results` do. It uses the options passed to it, otherwise the application's `IOptions<ToonSerializerOptions>`. The MVC
formatters use the same `IOptions<ToonSerializerOptions>`: `AddToonNet(...)`, `services.Configure<ToonSerializerOptions>(...)`
and the delegate passed to `AddToonFormatters` all configure that one instance.

A null value is written as the TOON document `null` (status 200), like `Results.Json(null)`. An empty body is not used
because an empty TOON document means an empty object. In controllers, MVC still answers a null `ObjectResult` with
`204 No Content` unless `HttpNoContentOutputFormatter.TreatNullValueAsNoContent` is turned off.

The response value itself is written as its runtime type, as System.Text.Json does for MVC responses (an action that
returns `object` or a base type writes the members of the object it returns); members inside it are written as their
declared types unless `SerializeRuntimeTypes` is on. An `IAsyncEnumerable<T>` value is collected into a list before it
is written, because a TOON array header carries the number of items.

## Content Negotiation

Client specifies desired format via `Accept` header:

```http
GET /api/users/1 HTTP/1.1
Accept: application/toon
```

The output formatter produces `application/toon` and `text/toon` (the media types the input formatter accepts) and
answers with the one the client asked for, also when the `Accept` value names a charset
(`application/toon; charset=utf-16`); UTF-8 and UTF-16 are written. Another charset in `Accept` is not copied into the
response's `Content-Type`: the response is written in UTF-8, so a client cannot make a cached response carry a
content type of its choosing. It is added after the JSON formatter, so JSON stays the default for `Accept: */*` or a missing `Accept` header; use
`[Produces("application/toon")]` to always return TOON from an action.

## See Also

- **[Input Formatters](input-formatters)**: Handle TOON requests
- **[Dependency Injection](dependency-injection)**: Service configuration
