### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonJsonConverter](ToonNet.Extensions.Json.ToonJsonConverter.md 'ToonNet\.Extensions\.Json\.ToonJsonConverter')

## ToonJsonConverter\.WriteObjectAsJson\(Utf8JsonWriter, ToonObject\) Method

Writes a TOON object as a JSON object using the provided Utf8JsonWriter\.

```csharp
private static void WriteObjectAsJson(System.Text.Json.Utf8JsonWriter writer, ToonNet.Core.Models.ToonObject obj);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonJsonConverter.WriteObjectAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonObject).writer'></a>

`writer` [System\.Text\.Json\.Utf8JsonWriter](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonwriter 'System\.Text\.Json\.Utf8JsonWriter')

The Utf8JsonWriter instance used to write the JSON output\.

<a name='ToonNet.Extensions.Json.ToonJsonConverter.WriteObjectAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonObject).obj'></a>

`obj` [ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject')

The TOON object to be serialized into JSON\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown if [writer](ToonNet.Extensions.Json.ToonJsonConverter.WriteObjectAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonObject).md#ToonNet.Extensions.Json.ToonJsonConverter.WriteObjectAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonObject).writer 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteObjectAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonObject\)\.writer') or [obj](ToonNet.Extensions.Json.ToonJsonConverter.WriteObjectAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonObject).md#ToonNet.Extensions.Json.ToonJsonConverter.WriteObjectAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonObject).obj 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteObjectAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonObject\)\.obj') is null\.

### Remarks
This method iterates over the properties of the TOON object and writes each key\-value pair
as a JSON property\. The value is recursively serialized using [WriteToonValueAsJson\(Utf8JsonWriter, ToonValue\)](ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteToonValueAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonValue\)')\.