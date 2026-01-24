### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonJsonConverter](ToonNet.Extensions.Json.ToonJsonConverter.md 'ToonNet\.Extensions\.Json\.ToonJsonConverter')

## ToonJsonConverter\.FromJson Method

| Overloads | |
| :--- | :--- |
| [FromJson\(string\)](ToonNet.Extensions.Json.ToonJsonConverter.FromJson.md#ToonNet.Extensions.Json.ToonJsonConverter.FromJson(string) 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.FromJson\(string\)') | Converts a JSON string to a TOON document\. |
| [FromJson\(JsonElement\)](ToonNet.Extensions.Json.ToonJsonConverter.FromJson.md#ToonNet.Extensions.Json.ToonJsonConverter.FromJson(System.Text.Json.JsonElement) 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.FromJson\(System\.Text\.Json\.JsonElement\)') | Converts a JsonElement to a TOON document\. |

<a name='ToonNet.Extensions.Json.ToonJsonConverter.FromJson(string)'></a>

## ToonJsonConverter\.FromJson\(string\) Method

Converts a JSON string to a TOON document\.

```csharp
public static ToonNet.Core.Models.ToonDocument FromJson(string json);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonJsonConverter.FromJson(string).json'></a>

`json` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The JSON string to convert\. This parameter must not be null\.

#### Returns
[ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument')  
A [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument') representing the JSON data\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the [json](ToonNet.Extensions.Json.ToonJsonConverter.md#ToonNet.Extensions.Json.ToonJsonConverter.FromJson(string).json 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.FromJson\(string\)\.json') parameter is null\.

[System\.Text\.Json\.JsonException](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonexception 'System\.Text\.Json\.JsonException')  
Thrown when the JSON parsing fails due to invalid JSON format\.

### Remarks
This method parses the provided JSON string into a [System\.Text\.Json\.JsonDocument](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsondocument 'System\.Text\.Json\.JsonDocument') and
converts its root element into a [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument') using the [FromJson\(JsonElement\)](ToonNet.Extensions.Json.ToonJsonConverter.FromJson.md#ToonNet.Extensions.Json.ToonJsonConverter.FromJson(System.Text.Json.JsonElement) 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.FromJson\(System\.Text\.Json\.JsonElement\)') method\.

<a name='ToonNet.Extensions.Json.ToonJsonConverter.FromJson(System.Text.Json.JsonElement)'></a>

## ToonJsonConverter\.FromJson\(JsonElement\) Method

Converts a JsonElement to a TOON document\.

```csharp
public static ToonNet.Core.Models.ToonDocument FromJson(System.Text.Json.JsonElement element);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonJsonConverter.FromJson(System.Text.Json.JsonElement).element'></a>

`element` [System\.Text\.Json\.JsonElement](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement 'System\.Text\.Json\.JsonElement')

The [System\.Text\.Json\.JsonElement](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement 'System\.Text\.Json\.JsonElement') to convert\. This element represents the JSON data to be transformed
into a TOON document structure\.

#### Returns
[ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument')  
A [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument') that encapsulates the converted TOON representation of the JSON data\.

### Remarks
This method utilizes [ConvertJsonElementToToonValue\(JsonElement\)](ToonNet.Extensions.Json.ToonJsonConverter.ConvertJsonElementToToonValue(System.Text.Json.JsonElement).md 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.ConvertJsonElementToToonValue\(System\.Text\.Json\.JsonElement\)') to transform the provided
[element](ToonNet.Extensions.Json.ToonJsonConverter.md#ToonNet.Extensions.Json.ToonJsonConverter.FromJson(System.Text.Json.JsonElement).element 'ToonNet\.Extensions\.Json\.ToonJsonConverter\.FromJson\(System\.Text\.Json\.JsonElement\)\.element') into a TOON value, which is then wrapped in a [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument')\.
The method assumes that the input JSON element is valid and does not perform additional validation\.