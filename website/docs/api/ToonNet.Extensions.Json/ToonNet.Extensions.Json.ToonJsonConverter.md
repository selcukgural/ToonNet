### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json')

## ToonJsonConverter Class

Provides bidirectional conversion between JSON and TOON formats\.
Supports System\.Text\.Json integration for seamless interoperability\.

```csharp
public static class ToonJsonConverter
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonJsonConverter

### Remarks
This static class acts as a utility for converting data between JSON and TOON formats\.
It includes methods for serializing and deserializing TOON documents and values,
ensuring compatibility with System\.Text\.Json\.

| Methods | |
| :--- | :--- |
| [ConvertJsonArray\(JsonElement\)](ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonArray(System.Text.Json.JsonElement).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ConvertJsonArray\(System\.Text\.Json\.JsonElement\)') | Converts a JSON array to a TOON array\. |
| [ConvertJsonElementToToonValue\(JsonElement\)](ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonElementToToonValue(System.Text.Json.JsonElement).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ConvertJsonElementToToonValue\(System\.Text\.Json\.JsonElement\)') | Converts a JSON element to a TOON value\. |
| [ConvertJsonObject\(JsonElement\)](ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonObject(System.Text.Json.JsonElement).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ConvertJsonObject\(System\.Text\.Json\.JsonElement\)') | Converts a JSON object to a TOON object\. |
| [FromJson\(string\)](ToonNet.Extensions.Json.ToonJsonConverter.FromJson.md#ToonNet.Extensions.Json.ToonJsonConverter.FromJson(string) 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.FromJson\(string\)') | Converts a JSON string to a TOON document\. |
| [FromJson\(JsonElement\)](ToonNet.Extensions.Json.ToonJsonConverter.FromJson.md#ToonNet.Extensions.Json.ToonJsonConverter.FromJson(System.Text.Json.JsonElement) 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.FromJson\(System\.Text\.Json\.JsonElement\)') | Converts a JsonElement to a TOON document\. |
| [ToJson\(ToonDocument, Nullable&lt;JsonWriterOptions&gt;\)](ToonNet.Extensions.Json.ToonJsonConverter.ToJson.md#ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonDocument,System.Nullable_System.Text.Json.JsonWriterOptions_) 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ToJson\(ToonNet\.Core\.Models\.ToonDocument, System\.Nullable\<System\.Text\.Json\.JsonWriterOptions\>\)') | Converts a TOON document to a JSON string\. |
| [ToJson\(ToonValue, Nullable&lt;JsonWriterOptions&gt;\)](ToonNet.Extensions.Json.ToonJsonConverter.ToJson.md#ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonValue,System.Nullable_System.Text.Json.JsonWriterOptions_) 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ToJson\(ToonNet\.Core\.Models\.ToonValue, System\.Nullable\<System\.Text\.Json\.JsonWriterOptions\>\)') | Converts a TOON value to a JSON string\. |
| [WriteArrayAsJson\(Utf8JsonWriter, ToonArray\)](ToonNet.Extensions.Json.ToonJsonConverter.WriteArrayAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonArray).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteArrayAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonArray\)') | Writes a TOON array as a JSON array using the provided Utf8JsonWriter\. |
| [WriteObjectAsJson\(Utf8JsonWriter, ToonObject\)](ToonNet.Extensions.Json.ToonJsonConverter.WriteObjectAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonObject).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteObjectAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonObject\)') | Writes a TOON object as a JSON object using the provided Utf8JsonWriter\. |
| [WriteToonValueAsJson\(Utf8JsonWriter, ToonValue\)](ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteToonValueAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonValue\)') | Serializes a TOON value into JSON using the provided Utf8JsonWriter\. |
