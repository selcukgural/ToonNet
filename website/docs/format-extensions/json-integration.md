# JSON Integration

Convert between JSON and TOON formats using `ToonNet.Extensions.Json`.

## Installation

```bash
dotnet add package ToonNet.Extensions.Json
```

## ToonConvert Class

Static utility class for JSON ↔ TOON ↔ .NET object conversion. Its methods work on strings.

### Deserialize from JSON

Convert a JSON string directly to a .NET object. This is a thin wrapper over `System.Text.Json`'s
`JsonSerializer.Deserialize` (TOON is not involved), so the usual System.Text.Json rules apply: property
names are matched case-sensitively unless you pass options.

```csharp
using System.Text.Json;
using ToonNet.Extensions.Json;

string jsonString = """
{
  "name": "Alice",
  "age": 30
}
""";

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
Person? person = ToonConvert.DeserializeFromJson<Person>(jsonString, jsonOptions);
```

To go JSON → TOON → .NET object instead, use `ToonConvert.ParseJson<T>(json, toonSerializerOptions)`.
TOON keys must then match the property names (or the `PropertyNamingPolicy`).

### Serialize to JSON

Convert .NET objects directly to JSON (a wrapper over `JsonSerializer.Serialize`; indented when no options
are passed):

```csharp
var person = new Person { Name = "Alice", Age = 30 };
string json = ToonConvert.SerializeToJson(person);
```

### JSON to TOON Conversion

```csharp
string json = """{ "name": "Alice", "age": 30 }""";

string toonString = ToonConvert.FromJson(json);
// Output:
// name: Alice
// age: 30
```

Pass `ToonOptions` to control the encoding, e.g. `ToonConvert.FromJson(json, new ToonOptions { Delimiter = '|' })`.

### TOON to JSON Conversion

```csharp
string toonString = """
name: Alice
age: 30
""";

string json = ToonConvert.ToJson(toonString);
// Output: {"name":"Alice","age":30}
```

## ToonJsonConverter Class

Bidirectional converter with more control:

```csharp
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Extensions.Json;

// JSON → TOON
string json = """{"users": [{"name": "Alice"}, {"name": "Bob"}]}""";
ToonDocument toonDoc = ToonJsonConverter.FromJson(json);
string toon = new ToonEncoder().Encode(toonDoc);
// Output:
// users[2]{name}:
//   Alice
//   Bob

// TOON → JSON
string toonInput = """
users[2]{name}:
  Alice
  Bob
""";
ToonDocument doc = ToonDocument.Parse(toonInput);
string jsonBack = ToonJsonConverter.ToJson(doc);
// Output: {"users":[{"name":"Alice"},{"name":"Bob"}]}
```

`ToonJsonConverter.ToJson` also accepts a single `ToonValue`.

### With JsonElement

```csharp
using System.Text.Json;

JsonDocument jsonDoc = JsonDocument.Parse(jsonString);
JsonElement element = jsonDoc.RootElement;

ToonDocument toonDoc = ToonJsonConverter.FromJson(element);
```

### With JsonWriterOptions

```csharp
var options = new JsonWriterOptions
{
    Indented = true
};

string prettyJson = ToonJsonConverter.ToJson(toonDoc, options);
```

## Complete Workflow Examples

### Scenario 1: API Migration (JSON → TOON)

```csharp
// Existing JSON API response
string jsonResponse = await httpClient.GetStringAsync("/api/users");

// Convert to TOON for AI/LLM
string toonString = ToonConvert.FromJson(jsonResponse);

// Now use with an LLM (uniform arrays of objects become compact tables)
string prompt = $"Analyze this data:\n{toonString}";
```

### Scenario 2: Data Import

```csharp
// Read JSON file
string json = File.ReadAllText("data.json");

// Convert to .NET objects via TOON
var data = ToonConvert.ParseJson<MyData>(json);

// Process data
ProcessData(data);

// Save as TOON for future use
string toon = ToonSerializer.Serialize(data);
File.WriteAllText("data.toon", toon);
```

### Scenario 3: Format Transformation

```csharp
// JSON → TOON → JSON (with formatting)
string compactJson = """{"name":"Alice","age":30}""";

ToonDocument toonDoc = ToonJsonConverter.FromJson(compactJson);

var options = new JsonWriterOptions { Indented = true };
string prettyJson = ToonJsonConverter.ToJson(toonDoc, options);

Console.WriteLine(prettyJson);
// Output:
// {
//   "name": "Alice",
//   "age": 30
// }
```

## Key Methods Summary

| Method | Description |
|--------|-------------|
| `ToonConvert.DeserializeFromJson<T>(string, JsonSerializerOptions?)` | JSON → .NET object (System.Text.Json) |
| `ToonConvert.SerializeToJson<T>(T, JsonSerializerOptions?)` | .NET object → JSON (System.Text.Json) |
| `ToonConvert.ParseJson<T>(string, ToonSerializerOptions?)` | JSON → TOON → .NET object |
| `ToonConvert.FromJson(string, ToonOptions?)` | JSON string → TOON string |
| `ToonConvert.ToJson(string, JsonWriterOptions?)` | TOON string → JSON string |
| `ToonJsonConverter.FromJson(string)` | JSON string → ToonDocument |
| `ToonJsonConverter.FromJson(JsonElement)` | JsonElement → ToonDocument |
| `ToonJsonConverter.ToJson(ToonDocument, JsonWriterOptions?)` | ToonDocument → JSON string |
| `ToonJsonConverter.ToJson(ToonValue, JsonWriterOptions?)` | ToonValue → JSON string |

## Conversion Notes

- JSON numbers are read as `double`, so integers beyond 2^53 and long decimals lose precision
  (`12345678901234567890` becomes `12345678901234567000`). `35.00` is written as `35`.
- `ToonJsonConverter.FromJson(string)` uses `JsonDocument.Parse` with its default maximum depth of 64.
- `ToonConvert.ToJson` parses TOON with the default `ToonOptions` (strict mode).

## Use Cases

1. **API Response Transformation**: Convert JSON APIs to TOON for LLM consumption
2. **Data Migration**: Import JSON data into TOON-based systems
3. **Format Conversion**: Bi-directional JSON ↔ TOON transformation
4. **Token Optimization**: Send compact TOON instead of JSON to LLMs
5. **Interoperability**: Work with JSON systems while using TOON internally

## See Also

- **[YAML Integration](yaml-integration)**: Convert YAML ↔ TOON
- **[Custom Formats](custom-formats)**: Create custom converters
