# Output Formatters

Return TOON format in HTTP responses using MVC output formatters.

## Installation

```bash
dotnet add package ToonNet.AspNetCore.Mvc
```

## Setup

```csharp
using ToonNet.AspNetCore.Mvc;

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
Content-Type: application/toon

Id: 1
Name: Alice
Email: alice@example.com
```

## Using ToonResult

Alternative approach using `ToonResult`:

```csharp
[HttpGet("{id}")]
public IResult GetUser(int id)
{
    var user = GetUserFromDatabase(id);
    return Results.Toon(user);  // Extension method
}
```

## Content Negotiation

Client specifies desired format via `Accept` header:

```http
GET /api/users/1 HTTP/1.1
Accept: application/toon
```

## See Also

- **[Input Formatters](input-formatters)**: Handle TOON requests
- **[Dependency Injection](dependency-injection)**: Service configuration
