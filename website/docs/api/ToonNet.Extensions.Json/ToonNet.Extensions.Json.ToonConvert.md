### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json')

## ToonConvert Class

Provides utility methods for converting between JSON, TOON, and \.NET objects\.

```csharp
public static class ToonConvert
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonConvert

### Remarks
This class follows the industry\-standard naming convention \(like JsonConvert, XmlConvert\)
and provides a clean, familiar API for format conversions\.
Similar to Newtonsoft\.Json's JsonConvert class\.

| Methods | |
| :--- | :--- |
| [DeserializeFromJson&lt;T&gt;\(string, JsonSerializerOptions\)](ToonNet.Extensions.Json.ToonConvert.DeserializeFromJson_T_(string,System.Text.Json.JsonSerializerOptions).md 'ToonNet\.Extensions\.Json\.ToonConvert\.DeserializeFromJson\<T\>\(string, System\.Text\.Json\.JsonSerializerOptions\)') | Deserializes a JSON string to a \.NET object using TOON as the intermediate format\. |
| [FromJson\(string, ToonOptions\)](ToonNet.Extensions.Json.ToonConvert.FromJson(string,ToonNet.Core.ToonOptions).md 'ToonNet\.Extensions\.Json\.ToonConvert\.FromJson\(string, ToonNet\.Core\.ToonOptions\)') | Converts a JSON string directly to TOON format string\. |
| [ParseJson&lt;T&gt;\(string, ToonSerializerOptions\)](ToonNet.Extensions.Json.ToonConvert.ParseJson_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Extensions\.Json\.ToonConvert\.ParseJson\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Converts a JSON string to TOON and deserializes to a \.NET object\. |
| [SerializeToJson&lt;T&gt;\(T, JsonSerializerOptions\)](ToonNet.Extensions.Json.ToonConvert.SerializeToJson_T_(T,System.Text.Json.JsonSerializerOptions).md 'ToonNet\.Extensions\.Json\.ToonConvert\.SerializeToJson\<T\>\(T, System\.Text\.Json\.JsonSerializerOptions\)') | Serializes a \.NET object to JSON string\. |
| [ToJson\(string, Nullable&lt;JsonWriterOptions&gt;\)](ToonNet.Extensions.Json.ToonConvert.ToJson(string,System.Nullable_System.Text.Json.JsonWriterOptions_).md 'ToonNet\.Extensions\.Json\.ToonConvert\.ToJson\(string, System\.Nullable\<System\.Text\.Json\.JsonWriterOptions\>\)') | Converts a TOON format string to JSON format string\. |
