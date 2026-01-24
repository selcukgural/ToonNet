### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonJsonConverter](ToonNet.Extensions.Json.ToonJsonConverter.md 'ToonNet\.Extensions\.Json\.ToonJsonConverter')

## ToonJsonConverter\.ConvertJsonObject\(JsonElement\) Method

Converts a JSON object to a TOON object\.

```csharp
private static ToonNet.Core.Models.ToonObject ConvertJsonObject(System.Text.Json.JsonElement element);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonObject(System.Text.Json.JsonElement).element'></a>

`element` [System\.Text\.Json\.JsonElement](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement 'System\.Text\.Json\.JsonElement')

The [System\.Text\.Json\.JsonElement](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement 'System\.Text\.Json\.JsonElement') representing the JSON object to convert\.
Must be of type [System\.Text\.Json\.JsonValueKind\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonvaluekind.object 'System\.Text\.Json\.JsonValueKind\.Object')\.

#### Returns
[ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject')  
A [ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject') containing the converted TOON key\-value pairs from the JSON object\.

#### Exceptions

[System\.Text\.Json\.JsonException](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonexception 'System\.Text\.Json\.JsonException')  
Thrown if the [element](ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonObject(System.Text.Json.JsonElement).md#ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonObject(System.Text.Json.JsonElement).element 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ConvertJsonObject\(System\.Text\.Json\.JsonElement\)\.element') is not of type [System\.Text\.Json\.JsonValueKind\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonvaluekind.object 'System\.Text\.Json\.JsonValueKind\.Object')\.

### Remarks
This method iterates over the properties of the JSON object, converts each property's value
to a corresponding TOON value using [ConvertJsonElementToToonValue\(JsonElement\)](ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonElementToToonValue(System.Text.Json.JsonElement).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ConvertJsonElementToToonValue\(System\.Text\.Json\.JsonElement\)'), and adds it
to the resulting [ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject')\.