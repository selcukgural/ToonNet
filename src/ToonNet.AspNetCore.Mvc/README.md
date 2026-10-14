# ToonNet.AspNetCore.Mvc

**MVC input/output formatters for TOON format**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/ToonNet.AspNetCore.Mvc.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.AspNetCore.Mvc/)
[![Downloads](https://img.shields.io/nuget/dt/ToonNet.AspNetCore.Mvc.svg?style=flat)](https://www.nuget.org/packages/ToonNet.AspNetCore.Mvc/)
[![Status](https://img.shields.io/badge/status-stable-success)](#)

---

## 📦 What is ToonNet.AspNetCore.Mvc?

ToonNet.AspNetCore.Mvc provides **MVC input/output formatters** for serving and accepting TOON format in ASP.NET Core APIs:

- ✅ **Input Formatter** - Accept TOON in request body
- ✅ **Output Formatter** - Serve TOON in response body
- ✅ **Content Negotiation** - `Accept: application/toon` and `Accept: text/toon` support
- ✅ **Model Binding** - Automatic TOON → C# object conversion
- ✅ **ToonResult** - Return TOON responses from actions and Minimal APIs
- ✅ **ReadFromToonAsync** - Read TOON request bodies in Minimal APIs

**Perfect for:**
- 🌐 **REST APIs** - Serve TOON alongside JSON
- 📊 **Content Negotiation** - Let clients choose format
- 🤖 **AI/LLM APIs** - Token-efficient responses
- 🔧 **Flexible APIs** - Support multiple formats

---

## 🚀 Quick Start

### Installation

```bash
# Core packages (installed automatically as dependencies)
dotnet add package ToonNet.Core
dotnet add package ToonNet.AspNetCore

# MVC formatters
dotnet add package ToonNet.AspNetCore.Mvc
```

### Basic Setup

```csharp
using ToonNet.AspNetCore.DependencyInjection;
using ToonNet.AspNetCore.Mvc.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Optional: configures the IOptions<ToonSerializerOptions> shared by the formatters and ToonResult
builder.Services.AddToonNet();

// Add MVC with TOON formatters
builder.Services.AddControllers()
    .AddToonFormatters();

var app = builder.Build();
app.MapControllers();
app.Run();
```

---

## 📖 Usage

### Example 1: Content Negotiation

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<Product>> GetProducts()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Laptop", Price = 1299.99m },
            new() { Id = 2, Name = "Mouse", Price = 29.99m },
            new() { Id = 3, Name = "Keyboard", Price = 89.99m }
        };

        // Automatic format negotiation based on Accept header
        return Ok(products);
    }
}

// Client requests:
// GET /api/products
// Accept: application/json  → Returns JSON
// Accept: application/toon  → Returns TOON
// Accept: */*               → Returns JSON (the first registered formatter)
```

**TOON Response:**
```toon
[3]{Id,Name,Price}:
  1,Laptop,1299.99
  2,Mouse,29.99
  3,Keyboard,89.99
```

### Example 2: Accept TOON Input

```csharp
[HttpPost]
public ActionResult<Product> CreateProduct([FromBody] Product product)
{
    // Accepts both JSON and TOON based on Content-Type header
    // Content-Type: application/json  → Parses as JSON
    // Content-Type: application/toon  → Parses as TOON
    
    // Validate and save...
    return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
}
```

**TOON Request Body:**
```toon
Name: Gaming Mouse
Price: 59.99
InStock: true
Category: Electronics
```

### Example 3: Explicit TOON Response

```csharp
using ToonNet.AspNetCore.Mvc.Http;

[HttpGet("{id}")]
public IResult GetProduct(int id)
{
    var product = _repository.GetById(id);
    if (product == null)
        return Results.NotFound();

    // Explicitly return TOON format, regardless of the Accept header
    return new ToonResult(product);
}
```

### Example 4: Nested Objects

```csharp
public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal Total { get; set; }
}

[HttpGet("orders/{id}")]
public ActionResult<Order> GetOrder(int id)
{
    var order = _orderService.GetById(id);
    return Ok(order);
}
```

**TOON Response:**
```toon
Id: 12345
Customer:
  Name: Alice Johnson
  Email: alice@example.com
Items[2]{ProductName,Quantity,Price}:
  Laptop,1,1299.99
  Mouse,2,29.99
Total: 1359.97
```

---

## ⚙️ Configuration

### Custom Formatter Options

```csharp
using ToonNet.Core;
using ToonNet.Core.Serialization;

builder.Services.AddControllers()
    .AddToonFormatters(options =>
    {
        options.PropertyNamingPolicy = PropertyNamingPolicy.CamelCase;
        options.ToonOptions = new ToonOptions { IndentSize = 4, MaxDepth = 50 };
    });
```

The formatters use these options for both reading and writing (with `CamelCase`, request bodies must use camelCase
keys; unknown keys are ignored). The delegate configures the application's `IOptions<ToonSerializerOptions>`, so the
formatters, `ToonResult` and `AddToonNet` all share one set of options; settings made with `AddToonNet` apply to the
formatters too.

### Request Size and Depth Limits

The input formatter rejects request bodies larger than **4 MB** (`ToonFormatterDefaults.MaxRequestBodySize`) with
`413 Payload Too Large`, independently of the server's own limit. Pass a different limit in bytes if you need one:

```csharp
builder.Services.AddControllers()
    .AddToonFormatters(configureOptions: null, maxRequestBodySize: 512 * 1024);
```

The limit is enforced by throwing `BadHttpRequestException` (status code 413). Kestrel and the developer exception page
answer with `413`; `UseExceptionHandler` answers with `500` unless your handler uses the exception's `StatusCode` (on
.NET 9 and later, `ExceptionHandlerOptions.StatusCodeSelector` can map it). The options are validated when the
application starts, and calling `AddToonFormatters` again replaces the formatters (the last limit applies).

Parsing enforces `ToonOptions.MaxDepth` (default 100). Bodies that are nested too deeply, are not valid TOON or do not
match the model type produce a model-state error (`400 Bad Request` with `[ApiController]`) instead of an exception.
The TOON error message (position, or property path and target type) is returned to the client; it does not contain the
offending value. Other exceptions are not swallowed and their messages are not returned.

### Formatter Priority

`AddToonFormatters()` appends the TOON formatters after the JSON formatters, so JSON stays the default and TOON is
returned when the client asks for `application/toon` (or the action has `[Produces("application/toon")]`). To answer
`406 Not Acceptable` instead of falling back to JSON for unsupported `Accept` values:

```csharp
builder.Services.AddControllers(options =>
{
    options.ReturnHttpNotAcceptable = true;
})
.AddToonFormatters();
```

### Media Type Mappings

TOON formatters register these media types:
- `application/toon` (input and output)
- `text/toon` (input and output)

Both formatters support UTF-8 and UTF-16 (`charset=utf-16`); the request charset is used for reading and the negotiated
charset for writing, also when the `Accept` value names one (`Accept: application/toon; charset=utf-16`).

---

## 📊 API Reference

### ToonInputFormatter

Deserializes TOON request bodies to C# objects:

```csharp
// Automatically registered with AddToonFormatters()
// Handles Content-Type: application/toon and text/toon
// Rejects bodies over 4 MB by default (413)
new ToonInputFormatter(serializerOptions);
new ToonInputFormatter(serializerOptions, maxRequestBodySize: 1024 * 1024);
```

### ToonOutputFormatter

Serializes C# objects to TOON response bodies:

```csharp
// Automatically registered with AddToonFormatters()
// Handles Accept: application/toon and text/toon
```

### ToonResult

Explicit TOON response for Minimal APIs (`IResult`):

```csharp
public sealed class ToonResult : IResult, IStatusCodeHttpResult, IValueHttpResult, IContentTypeHttpResult
{
    public ToonResult(object? value, ToonSerializerOptions? options = null)
    public ToonResult(object? value, ToonSerializerOptions? options, int? statusCode)

    public Task ExecuteAsync(HttpContext httpContext)
}

// Usage (Content-Type: application/toon; status 200):
app.MapGet("/data", () => new ToonResult(data));
app.MapGet("/data-custom", () => new ToonResult(data, customOptions));
app.MapGet("/data-ext", () => Results.Extensions.Toon(data));  // same, via IResultExtensions
app.MapPost("/items", async (HttpRequest request) =>
    Results.Extensions.Toon(await request.ReadFromToonAsync<Item>(), StatusCodes.Status201Created));

// Without options, ToonResult uses IOptions<ToonSerializerOptions> from DI (AddToonNet) or the defaults.
// A null value is written as `null` (an empty TOON body would mean an empty object).
```

### ReadFromToonAsync

Reads a TOON request body in Minimal APIs and middleware, like `ReadFromJsonAsync` (a minimal API parameter such as
`(Item item)` is read as JSON):

```csharp
public static class ToonHttpRequestExtensions
{
    public static bool HasToonContentType(this HttpRequest request)
    public static ValueTask<T?> ReadFromToonAsync<T>(this HttpRequest request, CancellationToken cancellationToken = default)
    public static ValueTask<T?> ReadFromToonAsync<T>(this HttpRequest request, ToonSerializerOptions? options, CancellationToken cancellationToken = default)
    public static ValueTask<T?> ReadFromToonAsync<T>(this HttpRequest request, ToonTypeInfo<T> typeInfo, CancellationToken cancellationToken = default)
    public static ValueTask<object?> ReadFromToonAsync(this HttpRequest request, Type type, ToonSerializerOptions? options = null, CancellationToken cancellationToken = default)
}
```

It uses `IOptions<ToonSerializerOptions>` from DI unless options are passed. A request without a TOON content type
throws `InvalidOperationException`, invalid TOON a `ToonException`; a blank body reads as the default value. The body
size is limited by the server (`MaxRequestBodySize`, `[RequestSizeLimit]`), not by the formatter's 4 MB limit.

---

## 🔒 Thread-Safety

- `ToonSerializer` methods are safe to call concurrently across threads.
- Shared metadata/name caches use `ConcurrentDictionary` for concurrent access.
- Cache entries are created on demand and retained for the process lifetime (no eviction).
- Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.

---

## 🎯 Real-World Examples

### Example 1: AI/LLM API Endpoint

```csharp
[HttpGet("context")]
[Produces("application/toon", "application/json")]
public ActionResult<UserContext> GetUserContext(int userId)
{
    var context = new UserContext
    {
        Name = "Alice",
        Age = 28,
        Interests = new[] { "AI", "Coding", "Gaming" },
        RecentPurchases = _purchaseService.GetRecent(userId, 10)
    };

    // LLM clients request with Accept: application/toon for fewer tokens
    return Ok(context);
}
```

### Example 2: Batch Operations

```csharp
[HttpPost("batch")]
[Consumes("application/toon")]
public ActionResult<BatchResult> ProcessBatch([FromBody] List<Product> products)
{
    // Client sends TOON (smaller payload)
    var results = _productService.BulkInsert(products);
    return Ok(results);
}

// Request (TOON - more compact, a root tabular array):
// [3]{Name,Price}:
//   Item1,10.00
//   Item2,20.00
//   Item3,30.00
```

### Example 3: Configuration API

```csharp
[HttpPut("config")]
public async Task<IResult> UpdateConfig([FromBody] AppConfig config)
{
    // Accepts both JSON and TOON
    await _configService.UpdateAsync(config);
    
    // Return TOON for readability
    return new ToonResult(config);
}
```

---

## 🔍 Testing

### Testing with WebApplicationFactory

```csharp
using Microsoft.AspNetCore.Mvc.Testing;

public class ToonFormatterTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ToonFormatterTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_WithToonAccept_ReturnsToon()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Accept", "application/toon");

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/toon", response.Content.Headers.ContentType.MediaType);
        
        var toon = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("[3]{Id,Name,Price}:", toon);  // list of products → tabular array
    }

    [Fact]
    public async Task PostProduct_WithToonContent_Success()
    {
        // Arrange
        var toonContent = """
            Name: Test Product
            Price: 99.99
            InStock: true
            """;
        
        var content = new StringContent(toonContent, Encoding.UTF8, "application/toon");

        // Act
        var response = await _client.PostAsync("/api/products", content);

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
```

---

## 🔗 Related Packages

**Core:**
- [`ToonNet.Core`](https://github.com/selcukgural/ToonNet/tree/master/src/ToonNet.Core) - Core serialization (required)
- [`ToonNet.AspNetCore`](https://github.com/selcukgural/ToonNet/tree/master/src/ToonNet.AspNetCore) - ASP.NET Core DI (required)

**Extensions:**
- [`ToonNet.Extensions.Json`](https://github.com/selcukgural/ToonNet/tree/master/src/ToonNet.Extensions.Json) - JSON ↔ TOON conversion
- [`ToonNet.Extensions.Yaml`](https://github.com/selcukgural/ToonNet/tree/master/src/ToonNet.Extensions.Yaml) - YAML ↔ TOON conversion

**Development:**
- [`ToonNet.Demo`](https://github.com/selcukgural/ToonNet/tree/master/demo/ToonNet.Demo) - Sample applications
- [`ToonNet.Tests`](https://github.com/selcukgural/ToonNet/tree/master/tests/ToonNet.Tests) - Test suite

---

## 📚 Documentation

- [Main Documentation](https://github.com/selcukgural/ToonNet/blob/master/README.md) - Complete ToonNet guide
- [API Guide](https://github.com/selcukgural/ToonNet/blob/master/docs/API-GUIDE.md) - Detailed API reference
- [ASP.NET Core Guide](https://github.com/selcukgural/ToonNet/blob/master/src/ToonNet.AspNetCore/README.md) - DI and configuration

---

## 📋 Requirements

- .NET 8.0 or later
- ASP.NET Core MVC 8.0+
- ToonNet.Core
- ToonNet.AspNetCore

---

## 📄 License

MIT License - See [LICENSE](https://github.com/selcukgural/ToonNet/blob/master/LICENSE) file for details.

---

## 🤝 Contributing

Contributions welcome! Please read [CONTRIBUTING.md](https://github.com/selcukgural/ToonNet/blob/master/CONTRIBUTING.md) first.

---

**Part of the [ToonNet](https://github.com/selcukgural/ToonNet/blob/master/README.md) serialization library family.**
