### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonJsonConverter](ToonNet.Extensions.Json.ToonJsonConverter.md 'ToonNet\.Extensions\.Json\.ToonJsonConverter')

## ToonJsonConverter\.WriteToonValueAsJson\(Utf8JsonWriter, ToonValue\) Method

Serializes a TOON value into JSON using the provided Utf8JsonWriter\.

```csharp
private static void WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter writer, ToonNet.Core.Models.ToonValue value);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).writer'></a>

`writer` [System\.Text\.Json\.Utf8JsonWriter](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonwriter 'System\.Text\.Json\.Utf8JsonWriter')

The Utf8JsonWriter instance used to write the JSON output\.
Must not be null\.

<a name='ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).value'></a>

`value` [ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue')

The TOON value to serialize\. This can be of various types such as
ToonNull, ToonBoolean, ToonNumber, ToonString, ToonObject, or ToonArray\.
Must not be null\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown if [writer](ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).md#ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).writer 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteToonValueAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonValue\)\.writer') or [value](ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).md#ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).value 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteToonValueAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonValue\)\.value') is null\.

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
Thrown if the type of [value](ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).md#ToonNet.Extensions.Json.ToonJsonConverter.WriteToonValueAsJson(System.Text.Json.Utf8JsonWriter,ToonNet.Core.Models.ToonValue).value 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.WriteToonValueAsJson\(System\.Text\.Json\.Utf8JsonWriter, ToonNet\.Core\.Models\.ToonValue\)\.value') is unsupported for serialization\.

### Remarks
This method determines the type of the provided TOON value and writes
the corresponding JSON representation\. Complex types such as ToonObject
and ToonArray are serialized recursively\.