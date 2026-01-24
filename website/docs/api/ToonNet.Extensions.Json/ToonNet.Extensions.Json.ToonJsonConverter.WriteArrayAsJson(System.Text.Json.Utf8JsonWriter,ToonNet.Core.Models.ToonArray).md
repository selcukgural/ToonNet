### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonJsonConverter](ToonNet.Extensions.Json.ToonJsonConverter.md 'ToonNet\.Extensions\.Json\.ToonJsonConverter')

## ToonJsonConverter\.WriteArrayAsJson\(Utf8JsonWriter, ToonArray\) Method

Writes a TOON array as a JSON array using the provided Utf8JsonWriter\.

```csharp
private static void WriteArrayAsJson(System.Text.Json.Utf8JsonWriter writer, ToonNet.Core.Models.ToonArray array);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonJsonConverter.WriteArrayAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonArray).writer'></a>

`writer` [System\.Text\.Json\.Utf8JsonWriter](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonwriter 'System\.Text\.Json\.Utf8JsonWriter')

The Utf8JsonWriter instance used to write the JSON output\.

<a name='ToonNet.Extensions.Json.ToonJsonConverter.WriteArrayAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonArray).array'></a>

`array` [ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray')

The TOON array to be serialized into JSON\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown if [writer](ToonNet.Extensions.Json.ToonJsonConverter.WriteArrayAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonArray).md#ToonNet.Extensions.Json.ToonJsonConverter.WriteArrayAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonArray).writer 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteArrayAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonArray\)\.writer') or [array](ToonNet.Extensions.Json.ToonJsonConverter.WriteArrayAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonArray).md#ToonNet.Extensions.Json.ToonJsonConverter.WriteArrayAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonArray).array 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteArrayAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonArray\)\.array') is null\.

### Remarks
This method iterates over the items in the TOON array and writes each item
as a JSON element\. Each item is recursively serialized using [WriteToonValueAsJson\(Utf8JsonWriter, ToonValue\)](ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteToonValueAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonValue\)')\.