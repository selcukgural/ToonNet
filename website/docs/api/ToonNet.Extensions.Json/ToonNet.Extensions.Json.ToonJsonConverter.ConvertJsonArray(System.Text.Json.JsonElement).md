### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonJsonConverter](ToonNet.Extensions.Json.ToonJsonConverter.md 'ToonNet\.Extensions\.Json\.ToonJsonConverter')

## ToonJsonConverter\.ConvertJsonArray\(JsonElement\) Method

Converts a JSON array to a TOON array\.

```csharp
private static ToonNet.Core.Models.ToonArray ConvertJsonArray(System.Text.Json.JsonElement element);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonArray(System.Text.Json.JsonElement).element'></a>

`element` [System\.Text\.Json\.JsonElement](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement 'System\.Text\.Json\.JsonElement')

The [System\.Text\.Json\.JsonElement](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement 'System\.Text\.Json\.JsonElement') representing the JSON array to convert\.
Must be of type [System\.Text\.Json\.JsonValueKind\.Array](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonvaluekind.array 'System\.Text\.Json\.JsonValueKind\.Array')\.

#### Returns
[ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray')  
A [ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray') containing the converted TOON values from the JSON array\.

#### Exceptions

[System\.Text\.Json\.JsonException](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonexception 'System\.Text\.Json\.JsonException')  
Thrown if the [element](ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonArray(System.Text.Json.JsonElement).md#ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonArray(System.Text.Json.JsonElement).element 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ConvertJsonArray\(System\.Text\.Json\.JsonElement\)\.element') is not of type [System\.Text\.Json\.JsonValueKind\.Array](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonvaluekind.array 'System\.Text\.Json\.JsonValueKind\.Array')\.

### Remarks
This method iterates over the elements of the JSON array, converts each element
to a corresponding TOON value using [ConvertJsonElementToToonValue\(JsonElement\)](ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonElementToToonValue(System.Text.Json.JsonElement).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ConvertJsonElementToToonValue\(System\.Text\.Json\.JsonElement\)'), and
adds it to the resulting [ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray')\.