### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonConvert](ToonNet.Extensions.Json.ToonConvert.md 'ToonNet\.Extensions\.Json\.ToonConvert')

## ToonConvert\.ToJson\(string, Nullable\<JsonWriterOptions\>\) Method

Converts a TOON format string to JSON format string\.

```csharp
public static string ToJson(string toonString, System.Nullable<System.Text.Json.JsonWriterOptions> writerOptions=null);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonConvert.ToJson(string,System.Nullable_System.Text.Json.JsonWriterOptions_).toonString'></a>

`toonString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The TOON string to convert\.

<a name='ToonNet.Extensions.Json.ToonConvert.ToJson(string,System.Nullable_System.Text.Json.JsonWriterOptions_).writerOptions'></a>

`writerOptions` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Text\.Json\.JsonWriterOptions](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonwriteroptions 'System\.Text\.Json\.JsonWriterOptions')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

Optional JSON writer options to control formatting\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
JSON format string\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when toonString is null\.

[ToonNet\.Core\.ToonParseException](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.toonparseexception 'ToonNet\.Core\.ToonParseException')  
Thrown when TOON parsing fails\.

### Remarks
This method provides a simple, developer\-friendly API for TOON to JSON conversion:

```csharp
// Default (no indentation)
string jsonString = ToonConvert.ToJson(toonString);

// With indentation
string jsonString = ToonConvert.ToJson(toonString, new JsonWriterOptions { Indented = true });
```