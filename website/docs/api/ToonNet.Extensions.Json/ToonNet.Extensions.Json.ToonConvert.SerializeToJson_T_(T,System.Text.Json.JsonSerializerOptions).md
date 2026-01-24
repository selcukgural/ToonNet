### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonConvert](ToonNet.Extensions.Json.ToonConvert.md 'ToonNet\.Extensions\.Json\.ToonConvert')

## ToonConvert\.SerializeToJson\<T\>\(T, JsonSerializerOptions\) Method

Serializes a \.NET object to JSON string\.

```csharp
public static string SerializeToJson{T}(T value, System.Text.Json.JsonSerializerOptions? options=null);
```
#### Type parameters

<a name='ToonNet.Extensions.Json.ToonConvert.SerializeToJson_T_(T,System.Text.Json.JsonSerializerOptions).T'></a>

`T`

The type of object to serialize\.
#### Parameters

<a name='ToonNet.Extensions.Json.ToonConvert.SerializeToJson_T_(T,System.Text.Json.JsonSerializerOptions).value'></a>

`value` [T](ToonNet.Extensions.Json.ToonConvert.SerializeToJson_T_(T,System.Text.Json.JsonSerializerOptions).md#ToonNet.Extensions.Json.ToonConvert.SerializeToJson_T_(T,System.Text.Json.JsonSerializerOptions).T 'ToonNet\.Extensions\.Json\.ToonConvert\.SerializeToJson\<T\>\(T, System\.Text\.Json\.JsonSerializerOptions\)\.T')

The value to serialize\.

<a name='ToonNet.Extensions.Json.ToonConvert.SerializeToJson_T_(T,System.Text.Json.JsonSerializerOptions).options'></a>

`options` [System\.Text\.Json\.JsonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions 'System\.Text\.Json\.JsonSerializerOptions')

Optional JSON serializer options\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
JSON string representation\.

### Remarks
This method provides a consistent API for format conversion:

```csharp
string json = ToonConvert.SerializeToJson(person);
```