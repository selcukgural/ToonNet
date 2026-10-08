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
        options.PropertyNamingPolicy = PropertyNamingPolicy.CamelCase;
        options.ToonOptions = new ToonOptions { MaxDepth = 50 };
    });
```

## Limits

Request bodies larger than **4 MB** (`ToonFormatterDefaults.MaxRequestBodySize`) are rejected with
`413 Payload Too Large`. Use the overload with `maxRequestBodySize` to change it:

```csharp
builder.Services.AddControllers()
    .AddToonFormatters(configureOptions: null, maxRequestBodySize: 512 * 1024);
```

Parsing enforces `ToonOptions.MaxDepth` (default 100). Input that is nested too deeply or is not valid TOON
becomes a model-state error, so `[ApiController]` endpoints answer with `400 Bad Request`.

## Encoding

Request bodies are decoded with the charset from the `Content-Type` header; UTF-8 and UTF-16 are supported.

## See Also

- **[Output Formatters](output-formatters)**: Return TOON responses
- **[Dependency Injection](dependency-injection)**: Service configuration
