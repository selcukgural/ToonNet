# ToonNet.AspNetCore.Mvc

**MVC input/output formatters for TOON format**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Package](https://img.shields.io/badge/package-ToonNet.AspNetCore.Mvc-blue)](#)
[![Status](https://img.shields.io/badge/status-stable-success)](#)

---

## 📦 What is ToonNet.AspNetCore.Mvc?

ToonNet.AspNetCore.Mvc provides **MVC input/output formatters** for serving and accepting TOON format in ASP.NET Core APIs:

- ✅ **Input Formatter** - Accept TOON in request body
- ✅ **Output Formatter** - Serve TOON in response body
- ✅ **Content Negotiation** - `Accept: application/toon` support
- ✅ **Model Binding** - Automatic TOON → C# object conversion
- ✅ **ToonResult** - Return TOON responses from actions

**Perfect for:**
- 🌐 **REST APIs** - Serve TOON alongside JSON
- 📊 **Content Negotiation** - Let clients choose format
- 🤖 **AI/LLM APIs** - Token-efficient responses
- 🔧 **Flexible APIs** - Support multiple formats

---

## 🚀 Quick Start

### Installation

```bash
# Core packages (required)
dotnet add package ToonNet.Core
dotnet add package ToonNet.AspNetCore

# MVC formatters
dotnet add package ToonNet.AspNetCore.Mvc
```

### Basic Setup

```csharp
using ToonNet.AspNetCore;
using ToonNet.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add ToonNet services
builder.Services.AddToon();

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
// Accept: */*               → Returns default format
```

**TOON Response:**
```toon
products[3]:
  - Id: 1
    Name: Laptop
    Price: 1299.99
  - Id: 2
    Name: Mouse
    Price: 29.99
  - Id: 3
    Name: Keyboard
    Price: 89.99
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
public IActionResult GetProduct(int id)
{
    var product = _repository.GetById(id);
    if (product == null)
        return NotFound();

    // Explicitly return TOON format
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
Items[2]:
  - ProductName: Laptop
    Quantity: 1
    Price: 1299.99
  - ProductName: Mouse
    Quantity: 2
    Price: 29.99
Total: 1359.97
```

---

## ⚙️ Configuration

### Custom Formatter Options

```csharp
builder.Services.AddControllers()
    .AddToonFormatters(options =>
    {
        options.IndentSize = 4;
        options.PreferInlineArrays = true;
        options.MaxDepth = 100;
    });
```

### Formatter Priority

```csharp
builder.Services.AddControllers(options =>
{
    // Add TOON as preferred format
    options.RespectBrowserAcceptHeader = true;
    options.ReturnHttpNotAcceptable = true;
})
.AddToonFormatters();
```

### Media Type Mappings

TOON formatters register these media types:
- `application/toon`
- `text/toon`
- `application/x-toon`

---

## 📊 API Reference

### ToonInputFormatter

Deserializes TOON request bodies to C# objects:

```csharp
// Automatically registered with AddToonFormatters()
// Handles Content-Type: application/toon
```

### ToonOutputFormatter

Serializes C# objects to TOON response bodies:

```csharp
// Automatically registered with AddToonFormatters()
// Handles Accept: application/toon
```

### ToonResult

Explicit TOON response result:

```csharp
public class ToonResult : IActionResult
{
    public ToonResult(object value)
    public ToonResult(object value, ToonSerializerOptions options)
    
    public Task ExecuteResultAsync(ActionContext context)
}

// Usage:
return new ToonResult(data);
return new ToonResult(data, customOptions);
```

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

// Request (TOON - more compact):
// products[3]:
//   - Name: Item1, Price: 10.00
//   - Name: Item2, Price: 20.00
//   - Name: Item3, Price: 30.00
```

### Example 3: Configuration API

```csharp
[HttpPut("config")]
public async Task<IActionResult> UpdateConfig([FromBody] AppConfig config)
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
        Assert.Contains("Name:", toon);
        Assert.Contains("Price:", toon);
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
- [`ToonNet.Core`](../ToonNet.Core) - Core serialization (required)
- [`ToonNet.AspNetCore`](../ToonNet.AspNetCore) - ASP.NET Core DI (required)

**Extensions:**
- [`ToonNet.Extensions.Json`](../ToonNet.Extensions.Json) - JSON ↔ TOON conversion
- [`ToonNet.Extensions.Yaml`](../ToonNet.Extensions.Yaml) - YAML ↔ TOON conversion

**Development:**
- [`ToonNet.Demo`](../../demo/ToonNet.Demo) - Sample applications
- [`ToonNet.Tests`](../../tests/ToonNet.Tests) - Test suite

---

## 📚 Documentation

- [Main Documentation](../../README.md) - Complete ToonNet guide
- [API Guide](../../docs/API-GUIDE.md) - Detailed API reference
- [ASP.NET Core Guide](../ToonNet.AspNetCore/README.md) - DI and configuration

---

## 📋 Requirements

- .NET 8.0 or later
- ASP.NET Core MVC 8.0+
- ToonNet.Core
- ToonNet.AspNetCore

---

## 📄 License

MIT License - See [LICENSE](../../LICENSE) file for details.

---

## 🤝 Contributing

Contributions welcome! Please read [CONTRIBUTING.md](../../CONTRIBUTING.md) first.

---

**Part of the [ToonNet](../../README.md) serialization library family.**
