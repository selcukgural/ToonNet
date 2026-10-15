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
- **Content-Type**: `application/toon` (`ToonFormatterDefaults.MediaType`)
- **Content-Type**: `text/toon` (`ToonFormatterDefaults.TextMediaType`)

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
(`name: Alice`); keys that match no property are ignored, a missing key for a `required` property is a model-state error
(`RespectRequiredMembers` is `true` by default), and properties without a public setter are not set unless they are
marked `[ToonInclude]`. They are the application's
`IOptions<ToonSerializerOptions>`, so settings made with `AddToonNet` or `services.Configure<ToonSerializerOptions>`
apply too. The options are validated when the application starts, and calling `AddToonFormatters` again replaces the
formatters instead of adding a second pair (the last request body limit applies).

To register the formatters yourself, create them with the options:
`mvcOptions.InputFormatters.Add(new ToonInputFormatter(serializerOptions, maxRequestBodySize))` (the limit is optional)
and `mvcOptions.OutputFormatters.Add(new ToonOutputFormatter(serializerOptions))`.

## Limits

Request bodies larger than **4 MB** (`ToonFormatterDefaults.MaxRequestBodySize`) are rejected with
`413 Payload Too Large`. Use the overload with `maxRequestBodySize` to change it:

```csharp
builder.Services.AddControllers()
    .AddToonFormatters(configureOptions: null, maxRequestBodySize: 512 * 1024);
```

The limit is enforced by throwing `BadHttpRequestException` with status code 413. Kestrel and the developer exception
page turn it into a `413` response; `UseExceptionHandler` answers with `500` unless your handler uses the exception's
`StatusCode` (on .NET 9 and later, `ExceptionHandlerOptions.StatusCodeSelector` can map it).

Parsing enforces `ToonOptions.MaxDepth` (default 100). Input that is nested too deeply, is not valid TOON or does not
match the model type becomes a model-state error, so `[ApiController]` endpoints answer with `400 Bad Request`. The
response contains the TOON error message (line and column, or property path and target type). The offending value is
not part of the message, so request data is not echoed back or written to logs: keys from the document are escaped and
cut to 64 characters, and an exception thrown by the model's constructor is named by its type only
(`The constructor of Money threw ArgumentException`). Messages of other exceptions are not returned to the client.

## Encoding

Request bodies are decoded with the charset from the `Content-Type` header; UTF-8 and UTF-16 are supported. UTF-8
bodies (the default) are decoded strictly into a pooled buffer and parsed there, without creating a string; ill-formed
UTF-8 is reported as a model error.

## Minimal APIs

A minimal API parameter such as `(User user)` is read as JSON. To read a TOON body, take the `HttpRequest` and call
`ReadFromToonAsync`, the TOON counterpart of `ReadFromJsonAsync` (it needs no `AddToonFormatters`):

```csharp
using ToonNet.AspNetCore.Mvc.Http;

app.MapPost("/users", async (HttpRequest request) =>
{
    var user = await request.ReadFromToonAsync<User>();
    return Results.Extensions.Toon(Save(user!), StatusCodes.Status201Created);
});
```

It uses the application's `IOptions<ToonSerializerOptions>` (or the options you pass), and there is an overload with a
`ToonTypeInfo<T>` for trimmed and NativeAOT applications, and one with a `Type`
(`request.ReadFromToonAsync(typeof(User))`) for code that knows the type at run time. The request must have a TOON content type, otherwise it
throws `InvalidOperationException` (check with `request.HasToonContentType()`); invalid TOON throws a `ToonException`.
A body with only whitespace reads as the default value.

Bodies larger than **4 MB** (`ToonFormatterDefaults.MaxRequestBodySize`, the limit of the input formatter) are rejected
with `413 Payload Too Large` (a `BadHttpRequestException` with status code 413). An action or endpoint that sets its
own limit (`[RequestSizeLimit]` or `[DisableRequestSizeLimit]` metadata) keeps the server's limit instead, which is the
one it sets, for the MVC formatter and `ReadFromToonAsync` alike. Give an endpoint that needs larger bodies its own limit:

```csharp
using Microsoft.AspNetCore.Mvc;

app.MapPost("/imports", async (HttpRequest request) =>
{
    var import = await request.ReadFromToonAsync<Import>();
    return Results.Extensions.Toon(Save(import!), StatusCodes.Status201Created);
})
.WithMetadata(new RequestSizeLimitAttribute(16 * 1024 * 1024)); // 16 MB for this endpoint
```

Avoid `[DisableRequestSizeLimit]` on endpoints that take bodies from untrusted clients. See
[Security](../advanced/security).

## See Also

- **[Output Formatters](output-formatters)**: Return TOON responses
- **[Dependency Injection](dependency-injection)**: Service configuration
