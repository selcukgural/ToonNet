# Input Formatters

Handle TOON format in HTTP requests using MVC input formatters.

## Installation

```bash
dotnet add package ToonNet.AspNetCore.Mvc
```

## Setup

```csharp
using ToonNet.AspNetCore.Mvc.DependencyInjection;

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

Note that `[Consumes("application/toon")]` limits the action to that media type, so a `text/toon` request to the action
above gets `415 Unsupported Media Type`.

## Configuration

```csharp
using ToonNet.Core;
using ToonNet.Core.Serialization;

builder.Services.AddControllers()
    .AddToonFormatters(options =>
    {
        options.PropertyNamingPolicy = PropertyNamingPolicy.CamelCase;
        options.ToonOptions = new ToonOptions { MaxDepth = 50 };
    });
```

The same options are used for reading and writing, so with `CamelCase` request bodies must use camelCase keys
(`name: Alice`); keys that match no property are ignored. These options are independent of the ones registered with
`AddToonNet`.

## Limits

Request bodies larger than **4 MB** (`ToonFormatterDefaults.MaxRequestBodySize`) are rejected with
`413 Payload Too Large`. Use the overload with `maxRequestBodySize` to change it:

```csharp
builder.Services.AddControllers()
    .AddToonFormatters(configureOptions: null, maxRequestBodySize: 512 * 1024);
```

The limit is enforced by throwing `BadHttpRequestException` with status code 413. Kestrel and the developer exception
page turn it into a `413` response; on .NET 8, `UseExceptionHandler` answers with `500` unless your handler uses the
exception's `StatusCode`.

Parsing enforces `ToonOptions.MaxDepth` (default 100). Input that is nested too deeply, is not valid TOON or does not
match the model type becomes a model-state error, so `[ApiController]` endpoints answer with `400 Bad Request`. The
response contains the TOON error message (line, column or property path, and the offending value); messages of other
exceptions are not returned to the client.

## Encoding

Request bodies are decoded with the charset from the `Content-Type` header; UTF-8 and UTF-16 are supported.

## See Also

- **[Output Formatters](output-formatters)**: Return TOON responses
- **[Dependency Injection](dependency-injection)**: Service configuration
