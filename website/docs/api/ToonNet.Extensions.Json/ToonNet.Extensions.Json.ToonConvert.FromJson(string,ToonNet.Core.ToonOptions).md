### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonConvert](ToonNet.Extensions.Json.ToonConvert.md 'ToonNet\.Extensions\.Json\.ToonConvert')

## ToonConvert\.FromJson\(string, ToonOptions\) Method

Converts a JSON string directly to TOON format string\.

```csharp
public static string FromJson(string jsonString, ToonNet.Core.ToonOptions? options=null);
```
#### Parameters

<a name='ToonNet.Extensions.Json.ToonConvert.FromJson(string,ToonNet.Core.ToonOptions).jsonString'></a>

`jsonString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The JSON string to convert\.

<a name='ToonNet.Extensions.Json.ToonConvert.FromJson(string,ToonNet.Core.ToonOptions).options'></a>

`options` [ToonNet\.Core\.ToonOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.toonoptions 'ToonNet\.Core\.ToonOptions')

Optional TOON encoding options\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
TOON format string\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when jsonString is null\.

[System\.Text\.Json\.JsonException](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonexception 'System\.Text\.Json\.JsonException')  
Thrown when JSON parsing fails\.

### Remarks
This method provides a simple, developer\-friendly API for JSON to TOON conversion:

```csharp
string toonString = ToonConvert.FromJson(jsonString);
```
No need to deal with ToonDocument or ToonEncoder \- just like System\.Text\.Json\!