### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonConvert](ToonNet.Extensions.Json.ToonConvert.md 'ToonNet\.Extensions\.Json\.ToonConvert')

## ToonConvert\.DeserializeFromJson\<T\>\(string, JsonSerializerOptions\) Method

Deserializes a JSON string to a \.NET object using TOON as the intermediate format\.

```csharp
public static T? DeserializeFromJson{T}(string jsonString, System.Text.Json.JsonSerializerOptions? options=null);
```
#### Type parameters

<a name='ToonNet.Extensions.Json.ToonConvert.DeserializeFromJson_T_(string,System.Text.Json.JsonSerializerOptions).T'></a>

`T`

The type of the object to deserialize to\.
#### Parameters

<a name='ToonNet.Extensions.Json.ToonConvert.DeserializeFromJson_T_(string,System.Text.Json.JsonSerializerOptions).jsonString'></a>

`jsonString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The JSON string to be deserialized\.

<a name='ToonNet.Extensions.Json.ToonConvert.DeserializeFromJson_T_(string,System.Text.Json.JsonSerializerOptions).options'></a>

`options` [System\.Text\.Json\.JsonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions 'System\.Text\.Json\.JsonSerializerOptions')

Optional serialization options provided for JSON deserialization\.

#### Returns
[T](ToonNet.Extensions.Json.ToonConvert.DeserializeFromJson_T_(string,System.Text.Json.JsonSerializerOptions).md#ToonNet.Extensions.Json.ToonConvert.DeserializeFromJson_T_(string,System.Text.Json.JsonSerializerOptions).T 'ToonNet\.Extensions\.Json\.ToonConvert\.DeserializeFromJson\<T\>\(string, System\.Text\.Json\.JsonSerializerOptions\)\.T')  
The deserialized object of type [T](ToonNet.Extensions.Json.ToonConvert.DeserializeFromJson_T_(string,System.Text.Json.JsonSerializerOptions).md#ToonNet.Extensions.Json.ToonConvert.DeserializeFromJson_T_(string,System.Text.Json.JsonSerializerOptions).T 'ToonNet\.Extensions\.Json\.ToonConvert\.DeserializeFromJson\<T\>\(string, System\.Text\.Json\.JsonSerializerOptions\)\.T')\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown if [jsonString](ToonNet.Extensions.Json.ToonConvert.DeserializeFromJson_T_(string,System.Text.Json.JsonSerializerOptions).md#ToonNet.Extensions.Json.ToonConvert.DeserializeFromJson_T_(string,System.Text.Json.JsonSerializerOptions).jsonString 'ToonNet\.Extensions\.Json\.ToonConvert\.DeserializeFromJson\<T\>\(string, System\.Text\.Json\.JsonSerializerOptions\)\.jsonString') is null\.

[System\.Text\.Json\.JsonException](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonexception 'System\.Text\.Json\.JsonException')  
Thrown if the JSON string cannot be parsed properly\.

[System\.NotSupportedException](https://learn.microsoft.com/en-us/dotnet/api/system.notsupportedexception 'System\.NotSupportedException')  
Thrown if an error occurs during TOON deserialization\.