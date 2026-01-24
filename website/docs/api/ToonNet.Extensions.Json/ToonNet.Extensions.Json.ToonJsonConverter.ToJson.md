### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonJsonConverter](ToonNet.Extensions.Json.ToonJsonConverter.md 'ToonNet\.Extensions\.Json\.ToonJsonConverter')

## ToonJsonConverter\.ToJson Method

| Overloads | |
| :--- | :--- |
| [ToJson\(ToonDocument, Nullable&lt;JsonWriterOptions&gt;\)](ToonNet.Extensions.Json.ToonJsonConverter.ToJson.md#ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonDocument,System.Nullable_System.Text.Json.JsonWriterOptions_) 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ToJson\(ToonNet\.Core\.Models\.ToonDocument, System\.Nullable\<System\.Text\.Json\.JsonWriterOptions\>\)') | Converts a TOON document to a JSON string\. |
| [ToJson\(ToonValue, Nullable&lt;JsonWriterOptions&gt;\)](ToonNet.Extensions.Json.ToonJsonConverter.ToJson.md#ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonValue,System.Nullable_System.Text.Json.JsonWriterOptions_) 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ToJson\(ToonNet\.Core\.Models\.ToonValue, System\.Nullable\<System\.Text\.Json\.JsonWriterOptions\>\)') | Converts a TOON value to a JSON string\. |

<a name='ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonDocument,System.Nullable_System.Text.Json.JsonWriterOptions_)'></a>

## ToonJsonConverter\.ToJson\(ToonDocument, Nullable\<JsonWriterOptions\>\) Method

Converts a TOON document to a JSON string\.

```csharp
public static string ToJson(ToonNet.Core.Models.ToonDocument document, System.Nullable<System.Text.Json.JsonWriterOptions> writerOptions=null);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonDocument,System.Nullable_System.Text.Json.JsonWriterOptions_).document'></a>

`document` [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument')

The TOON document to convert\. Must not be null\.

<a name='ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonDocument,System.Nullable_System.Text.Json.JsonWriterOptions_).writerOptions'></a>

`writerOptions` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Text\.Json\.JsonWriterOptions](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonwriteroptions 'System\.Text\.Json\.JsonWriterOptions')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

Optional JSON writer options to control formatting\.
If null, default options with no indentation will be used\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
A JSON string representation of the TOON document\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the [document](ToonNet.Extensions.Json.ToonJsonConverter.md#ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonDocument,System.Nullable_System.Text.Json.JsonWriterOptions_).document 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ToJson\(ToonNet\.Core\.Models\.ToonDocument, System\.Nullable\<System\.Text\.Json\.JsonWriterOptions\>\)\.document') is null\.

### Remarks
This method serializes the root value of the provided TOON document into a JSON string\.
The caller can specify JSON writer options to control formatting, indentation, encoding, and other serialization behaviors\.

<a name='ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonValue,System.Nullable_System.Text.Json.JsonWriterOptions_)'></a>

## ToonJsonConverter\.ToJson\(ToonValue, Nullable\<JsonWriterOptions\>\) Method

Converts a TOON value to a JSON string\.

```csharp
public static string ToJson(ToonNet.Core.Models.ToonValue value, System.Nullable<System.Text.Json.JsonWriterOptions> writerOptions=null);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonValue,System.Nullable_System.Text.Json.JsonWriterOptions_).value'></a>

`value` [ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue')

The TOON value to convert\. Must not be null\.

<a name='ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonValue,System.Nullable_System.Text.Json.JsonWriterOptions_).writerOptions'></a>

`writerOptions` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Text\.Json\.JsonWriterOptions](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonwriteroptions 'System\.Text\.Json\.JsonWriterOptions')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

Optional JSON writer options to control formatting\.
If null, default options with no indentation will be used\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
A JSON string representation of the TOON value\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the [value](ToonNet.Extensions.Json.ToonJsonConverter.md#ToonNet.Extensions.Json.ToonJsonConverter.ToJson(ToonNet.Core.Models.ToonValue,System.Nullable_System.Text.Json.JsonWriterOptions_).value 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ToJson\(ToonNet\.Core\.Models\.ToonValue, System\.Nullable\<System\.Text\.Json\.JsonWriterOptions\>\)\.value') is null\.

### Remarks
This method serializes the provided TOON value into a JSON string\. The serialization
process uses a [System\.Text\.Json\.Utf8JsonWriter](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonwriter 'System\.Text\.Json\.Utf8JsonWriter') to write the JSON output\. Complex TOON
types such as objects and arrays are serialized recursively\. The caller can specify
JSON writer options to control formatting, indentation, encoding, and other serialization behaviors\.