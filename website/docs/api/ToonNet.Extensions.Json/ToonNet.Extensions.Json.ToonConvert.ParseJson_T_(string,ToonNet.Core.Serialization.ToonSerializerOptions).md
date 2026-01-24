### [ToonNet\.Extensions\.Json](ToonNet.Extensions.Json.md 'ToonNet\.Extensions\.Json').[ToonConvert](ToonNet.Extensions.Json.ToonConvert.md 'ToonNet\.Extensions\.Json\.ToonConvert')

## ToonConvert\.ParseJson\<T\>\(string, ToonSerializerOptions\) Method

Converts a JSON string to TOON and deserializes to a \.NET object\.

```csharp
public static T? ParseJson{T}(string jsonString, ToonNet.Core.Serialization.ToonSerializerOptions? options=null);
```
#### Type parameters

<a name='ToonNet.Extensions.Json.ToonConvert.ParseJson_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions).T'></a>

`T`

The type to deserialize to\.
#### Parameters

<a name='ToonNet.Extensions.Json.ToonConvert.ParseJson_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions).jsonString'></a>

`jsonString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The JSON string to convert and deserialize\.

<a name='ToonNet.Extensions.Json.ToonConvert.ParseJson_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional serialization options\.

#### Returns
[T](ToonNet.Extensions.Json.ToonConvert.ParseJson_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions).md#ToonNet.Extensions.Json.ToonConvert.ParseJson_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions).T 'ToonNet\.Extensions\.Json\.ToonConvert\.ParseJson\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)\.T')  
The deserialized object\.

### Remarks
This is a convenience method that combines JSON to TOON conversion with deserialization:

```csharp
var person = ToonConvert.ParseJson<Person>(jsonString);
```
Equivalent to: Deserialize\<T\>\(FromJson\(jsonString\)\)