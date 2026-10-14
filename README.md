![ToonNet](https://raw.githubusercontent.com/selcukgural/ToonNet/master/icon.png)

**TOON serialization for .NET: fewer tokens for language models, the API of System.Text.Json.**

[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/ToonNet.Core.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ToonNet.Core/)
[![Downloads](https://img.shields.io/nuget/dt/ToonNet.Core.svg?style=flat)](https://www.nuget.org/packages/ToonNet.Core/)
[![CI](https://github.com/selcukgural/ToonNet/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/selcukgural/ToonNet/actions/workflows/ci.yml)
[![Spec](https://img.shields.io/badge/TOON%20spec%20v4.4-564%2F564%20fixtures-brightgreen?style=flat)](https://github.com/selcukgural/ToonNet/blob/master/docs/TOON_SPEC_COMPLIANCE.md#toonnet-implementation-status)
[![Documentation](https://img.shields.io/badge/docs-online-brightgreen?style=flat&logo=docusaurus)](https://selcukgural.github.io/ToonNet/)

[Documentation](https://selcukgural.github.io/ToonNet/) • [Migrating to 4.0](https://selcukgural.github.io/ToonNet/docs/getting-started/migration-4) •
[Samples](https://github.com/selcukgural/ToonNet/tree/master/demo/ToonNet.Demo/Samples) • [Changelog](CHANGELOG.md)

[TOON](https://github.com/toon-format/spec) writes uniform data as tables, with each field name once:

```toon
[3]{Id,Customer,Total,Status}:
  1001,Ada Lovelace,250.5,Shipped
  1002,Alan Turing,80,Pending
  1003,Grace Hopper,1200,Pending
```

The same orders in JSON repeat every key on every row. ToonNet implements TOON spec v4.4 completely (all 564
official fixtures), writes exactly what the official library writes, many times faster, and adds a source generator,
NativeAOT, ASP.NET Core, JSON/YAML conversion, JSONPath queries, business rules and helpers for prompts.

## Install

```bash
dotnet add package ToonNet.Core                # serializer, queries, rules, prompts
dotnet add package ToonNet.SourceGenerators    # optional: generated code, NativeAOT
dotnet add package ToonNet.AspNetCore.Mvc      # optional: TOON request and response bodies
dotnet add package ToonNet.Extensions.Json     # optional: JSON <-> TOON
```

## Serialize and Deserialize

```csharp
using ToonNet.Core.Serialization;

var orders = new List<Order>
{
    new(1001, "Ada Lovelace", 250.50m, OrderStatus.Shipped),
    new(1002, "Alan Turing", 80.00m, OrderStatus.Pending),
    new(1003, "Grace Hopper", 1200.00m, OrderStatus.Pending)
};

string toon = ToonSerializer.Serialize(orders);                  // the table above
List<Order> back = ToonSerializer.Deserialize<List<Order>>(toon)!;

public record Order(int Id, string Customer, decimal Total, OrderStatus Status);
public enum OrderStatus { Pending, Shipped }
```

Mark a type `[ToonSerializable]` (with `ToonNet.SourceGenerators`) and the serializer uses generated code for it,
without reflection; `ToonSerializer.Serialize(order, Order.ToonTypeInfo)` also works under NativeAOT.

## Send Data to a Language Model, Read the Answer Back

```csharp
using ToonNet.Core.Llm;
using ToonNet.Core.Validation;

var prompt = $"""
    Here are today's orders:
    {ToonPrompt.Serialize(orders)}

    Which orders need attention? Answer in a ```toon block with this shape:
    {ToonPrompt.DescribeShape<Review>()}
    """;

var rules = new ToonRuleSet()
    .Rule("$.Flagged[*]", "@.Id > 0", "Unknown order id {value}")
    .Required("$.Summary", "Give a summary");

string answer = await CallModelAsync(prompt);
Review review = ToonPrompt.DeserializeValidated<Review>(answer, rules)!;   // throws ToonRuleException with what to fix

public class Review
{
    public string Summary { get; set; } = "";
    public List<Flag> Flagged { get; set; } = [];
}

public record Flag(int Id, string Reason);
```

`DescribeShape` tells the model exactly what to write:

```toon
Summary: <string>
Flagged[N]{Id,Reason}:
  <integer>,<string>
```

`ToonPrompt.Deserialize` finds the ```` ```toon ```` block in the answer and forgives the slips models make (a wrong
array length, a repeated key). `ToonPromptOptions.MaxArrayItems` shortens long arrays and tells the model so.

## Serve TOON From an ASP.NET Core API

```csharp
using ToonNet.AspNetCore.Mvc.DependencyInjection;
using ToonNet.AspNetCore.Mvc.Http;

builder.Services.AddControllers().AddToonFormatters();   // Accept: application/toon, Content-Type: application/toon

// Controllers are unchanged: [HttpGet] public IEnumerable<Order> Get() => ...

// Minimal APIs read and write TOON explicitly
app.MapPost("/orders", async (HttpRequest request) =>
{
    var order = await request.ReadFromToonAsync<Order>();
    return Results.Extensions.Toon(order, StatusCodes.Status201Created);
});
```

```bash
curl -H "Accept: application/toon" https://localhost:5001/api/orders
```

```toon
[2]{Id,Customer,Total}:
  1,Ada,250.5
  2,Alan,80
```

Controllers keep returning objects; clients that ask for `application/toon` get TOON, others JSON. Request bodies are
limited to 4 MB by default.

## Convert JSON to TOON

```csharp
using ToonNet.Extensions.Json;

string toon = ToonConvert.FromJson("""{"users":[{"id":1,"name":"Ada","admin":true},{"id":2,"name":"Alan","admin":false}]}""");
string json = ToonConvert.ToJson(toon);   // the same JSON back
```

```toon
users[2]{id,name,admin}:
  1,Ada,true
  2,Alan,false
```

Use it to shrink an API response or a tool result before it goes into a prompt.

## Query a Large Document Without Loading All of It

```csharp
using ToonNet.Core.Paths;

string toon = await File.ReadAllTextAsync("orders.toon");   // Orders[3]{Id,Customer,Total,Status}: ...

// JSONPath (RFC 9535) straight over the text: only the selected values are built
var bigOrders = ToonPath.Parse("$.Orders[?@.Total > 100].Id").SelectFrom(toon);          // 1001, 1003

// Objects at a path
List<Order?> pending = ToonSerializer.DeserializeAll<Order>(toon, "$.Orders[?@.Status == 'Pending']");

// One item at a time from a file, at constant memory
await using var file = File.OpenRead("orders.toon");
await foreach (var order in ToonSerializer.DeserializeAsyncEnumerableAt<Order>(file, "$.Orders"))
{
    await ProcessAsync(order!);
}
```

## Check Business Rules

```csharp
var rules = new ToonRuleSet()
    .Rule("$.Orders[*]", "@.Total >= 0", "Negative total")
    .Unique("$.Orders[*].Id", "Order id {value} repeats")
    .Forbidden("$..Password", "Passwords must not be sent");

var result = rules.Validate("""
    Orders[2]{Id,Total}:
      1,10
      1,-5
    Password: hunter2
    """);

foreach (var violation in result.Violations)
{
    Console.WriteLine(violation);
}
```

```text
$['Orders'][1]: Negative total
$['Orders'][1]['Id']: Order id 1 repeats
$['Password']: Passwords must not be sent
```

## Read Configuration From TOON

```csharp
using ToonNet.AspNetCore.Configuration;

builder.Configuration.AddToonFile("appsettings.toon", optional: true, reloadOnChange: true);
```

```toon
Shop:
  Name: Corner Books
  MaxItems: 50
```

## Packages

| Package | What it adds |
|---------|--------------|
| [ToonNet.Core](https://www.nuget.org/packages/ToonNet.Core/) | `ToonSerializer`, `ToonDocument`, reader and writer, `ToonPath`, `ToonRuleSet`, `ToonPrompt`, merge patches |
| [ToonNet.SourceGenerators](https://www.nuget.org/packages/ToonNet.SourceGenerators/) | Generated code for `[ToonSerializable]` types: faster, no reflection, NativeAOT |
| [ToonNet.AspNetCore](https://www.nuget.org/packages/ToonNet.AspNetCore/) | `AddToonNet` options from configuration, the TOON configuration provider |
| [ToonNet.AspNetCore.Mvc](https://www.nuget.org/packages/ToonNet.AspNetCore.Mvc/) | Input and output formatters, `ReadFromToonAsync`, `Results.Extensions.Toon` |
| [ToonNet.Extensions.Json](https://www.nuget.org/packages/ToonNet.Extensions.Json/) | JSON ↔ TOON |
| [ToonNet.Extensions.Yaml](https://www.nuget.org/packages/ToonNet.Extensions.Yaml/) | YAML ↔ TOON |

## Why ToonNet

- **Exact:** TOON spec v4.4 with all 564 official fixtures; byte-identical output to the official
  [Toon.Format](https://www.nuget.org/packages/Toon.Format) library; every digit of `decimal`, `long` and `ulong` kept.
- **Fast:** 8–27x faster than Toon.Format when writing and 2.4–7x when reading, with a fraction of the memory, close
  to System.Text.Json ([measurements](https://selcukgural.github.io/ToonNet/docs/comparison)).
- **Safe by default:** strict parsing, depth and size limits, only public setters set from documents, objects
  written as their declared type, no values in error messages ([security](https://selcukgural.github.io/ToonNet/docs/advanced/security)).
- **Everywhere:** .NET 8 and .NET 10, NativeAOT and trimming with generated type infos, no dependencies in the core.

## Learn More

The [documentation](https://selcukgural.github.io/ToonNet/) covers every feature in detail: serialization options,
the type system, streaming, the reader and writer, querying, rules, language models, ASP.NET Core, JSON and YAML,
source generators and performance. Report security problems as [SECURITY.md](SECURITY.md) describes; contributions
are welcome ([CONTRIBUTING.md](CONTRIBUTING.md)).

MIT License.
