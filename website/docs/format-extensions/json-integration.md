# JSON Integration

Convert between JSON and TOON formats using `ToonNet.Extensions.Json`.

## Installation

```bash
dotnet add package ToonNet.Extensions.Json
```

## ToonConvert Class

Static utility class for JSON ↔ TOON ↔ .NET object conversion.

### Deserialize from JSON

Convert JSON string directly to .NET objects:

```csharp
using ToonNet.Extensions.Json;

string jsonString = """
{
  "name": "Alice",
  "age": 30
}
""";

Person person = ToonConvert.DeserializeFromJson<Person>(jsonString);
```

### Serialize to JSON

Convert .NET objects directly to JSON:

```csharp
var person = new Person { Name = "Alice", Age = 30 };
string json = ToonConvert.SerializeToJson(person);
```

### JSON to TOON Conversion

```csharp
string json = """{ "name": "Alice", "age": 30 }""";

// Convert to ToonDocument
ToonDocument toonDoc = ToonConvert.FromJson(json);

// Convert to TOON string
string toonString = toonDoc.ToString();
// Output:
// name: Alice
// age: 30
```

### TOON to JSON Conversion

```csharp
string toonString = """
name: Alice
age: 30
""";

ToonDocument toonDoc = ToonDocument.Parse(toonString);
string json = ToonConvert.ToJson(toonDoc);
// Output: {"name":"Alice","age":30}
```

## ToonJsonConverter Class

Bidirectional converter with more control:

```csharp
using ToonNet.Extensions.Json;

// JSON → TOON
string json = """{"users": [{"name": "Alice"}, {"name": "Bob"}]}""";
ToonDocument toonDoc = ToonJsonConverter.FromJson(json);

// TOON → JSON
string toonInput = """
users:
  - name: Alice
  - name: Bob
""";
ToonDocument doc = ToonDocument.Parse(toonInput);
string json = ToonJsonConverter.ToJson(doc);
```

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
ToonDocument toonDoc = ToonConvert.FromJson(jsonResponse);
string toonString = toonDoc.ToString();

// Now use with LLM (40% fewer tokens!)
string prompt = $"Analyze this data:\n{toonString}";
```

### Scenario 2: Data Import

```csharp
// Read JSON file
string json = File.ReadAllText("data.json");

// Convert to .NET objects via TOON
var data = ToonConvert.DeserializeFromJson<MyData>(json);

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

ToonDocument toonDoc = ToonConvert.FromJson(compactJson);

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
| `ToonConvert.DeserializeFromJson<T>(string)` | JSON → .NET object |
| `ToonConvert.SerializeToJson<T>(T)` | .NET object → JSON |
| `ToonConvert.FromJson(string)` | JSON → ToonDocument |
| `ToonConvert.ToJson(ToonDocument)` | ToonDocument → JSON |
| `ToonJsonConverter.FromJson(string)` | JSON string → ToonDocument |
| `ToonJsonConverter.FromJson(JsonElement)` | JsonElement → ToonDocument |
| `ToonJsonConverter.ToJson(ToonDocument)` | ToonDocument → JSON string |
| `ToonJsonConverter.ToJson(ToonDocument, JsonWriterOptions)` | With formatting options |

## Use Cases

1. **API Response Transformation**: Convert JSON APIs to TOON for LLM consumption
2. **Data Migration**: Import JSON data into TOON-based systems
3. **Format Conversion**: Bi-directional JSON ↔ TOON transformation
4. **Token Optimization**: Reduce AI API costs by converting JSON to TOON
5. **Interoperability**: Work with JSON systems while using TOON internally

## See Also

- **[YAML Integration](yaml-integration)**: Convert YAML ↔ TOON
- **[Custom Formats](custom-formats)**: Create custom converters
