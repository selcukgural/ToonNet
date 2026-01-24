# Input Formatters

Handle TOON format in HTTP requests using MVC input formatters.

## Installation

```bash
dotnet add package ToonNet.AspNetCore.Mvc
```

## Setup

```csharp
using ToonNet.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddToonFormatters();  // Add TOON input/output formatters

var app = builder.Build();
app.MapControllers();
app.Run();
```

## Using in Controllers

### POST Endpoint

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpPost]
    [Consumes("application/toon")]
    public IActionResult CreateUser([FromBody] User user)
    {
        // user is automatically deserialized from TOON
        return Ok($"Created: {user.Name}");
    }
}
```

### Request Example

```http
POST /api/users HTTP/1.1
Content-Type: application/toon

Name: Alice Smith
Email: alice@example.com
Age: 30
```

## Media Type

The input formatter handles:
- **Content-Type**: `application/toon`
- **Content-Type**: `text/toon`

## Configuration

```csharp
builder.Services.AddControllers()
    .AddToonFormatters(options =>
    {
        options.SerializerOptions = new ToonSerializerOptions
        {
            PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
            CaseSensitive = false
        };
    });
```

## See Also

- **[Output Formatters](output-formatters)**: Return TOON responses
- **[Dependency Injection](dependency-injection)**: Service configuration
