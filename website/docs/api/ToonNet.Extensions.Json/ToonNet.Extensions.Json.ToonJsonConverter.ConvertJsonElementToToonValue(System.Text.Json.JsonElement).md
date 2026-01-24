### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonJsonConverter](ToonNet.Extensions.Json.ToonJsonConverter.md 'ToonNet\.Extensions\.Json\.ToonJsonConverter')

## ToonJsonConverter\.ConvertJsonElementToToonValue\(JsonElement\) Method

Converts a JSON element to a TOON value\.

```csharp
private static ToonNet.Core.Models.ToonValue ConvertJsonElementToToonValue(System.Text.Json.JsonElement element);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonElementToToonValue(System.Text.Json.JsonElement).element'></a>

`element` [System\.Text\.Json\.JsonElement](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement 'System\.Text\.Json\.JsonElement')

The [System\.Text\.Json\.JsonElement](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement 'System\.Text\.Json\.JsonElement') to convert\. The element's [System\.Text\.Json\.JsonValueKind](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonvaluekind 'System\.Text\.Json\.JsonValueKind') determines
the type of TOON value created\.

#### Returns
[ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue')  
A [ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue') representing the JSON element\. The returned value can be one of the following:
- [ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject') for JSON objects.
- [ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray') for JSON arrays.
- [ToonNet\.Core\.Models\.ToonString](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonstring 'ToonNet\.Core\.Models\.ToonString') for JSON strings.
- [ToonNet\.Core\.Models\.ToonNumber](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonnumber 'ToonNet\.Core\.Models\.ToonNumber') for JSON numbers.
- [ToonNet\.Core\.Models\.ToonBoolean](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonboolean 'ToonNet\.Core\.Models\.ToonBoolean') for JSON boolean values.
- [ToonNet\.Core\.Models\.ToonNull](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonnull 'ToonNet\.Core\.Models\.ToonNull') for JSON null or undefined values.

#### Exceptions

[System\.Text\.Json\.JsonException](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonexception 'System\.Text\.Json\.JsonException')  
Thrown if the [element](ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonElementToToonValue(System.Text.Json.JsonElement).md#ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonElementToToonValue(System.Text.Json.JsonElement).element 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ConvertJsonElementToToonValue\(System\.Text\.Json\.JsonElement\)\.element') has an unsupported [System\.Text\.Json\.JsonValueKind](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonvaluekind 'System\.Text\.Json\.JsonValueKind')\.

### Remarks
This method uses a switch expression to map the [System\.Text\.Json\.JsonValueKind](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonvaluekind 'System\.Text\.Json\.JsonValueKind') of the input
[element](ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonElementToToonValue(System.Text.Json.JsonElement).md#ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonElementToToonValue(System.Text.Json.JsonElement).element 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ConvertJsonElementToToonValue\(System\.Text\.Json\.JsonElement\)\.element') to the corresponding TOON value type\. Unsupported kinds will result
in a [System\.Text\.Json\.JsonException](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonexception 'System\.Text\.Json\.JsonException') being thrown\.