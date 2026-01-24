#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.DeserializeObject\(ToonValue, Type, ToonSerializerOptions, int\) Method

Deserializes a TOON object to a C\# object by reflecting over properties\.

```csharp
private static object DeserializeObject(ToonNet.Core.Models.ToonValue value, System.Type targetType, ToonNet.Core.Serialization.ToonSerializerOptions options, int depth);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeObject(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int).value'></a>

`value` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

The TOON object to deserialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeObject(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int).targetType'></a>

`targetType` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The target C\# type\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeObject(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Deserialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeObject(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int).depth'></a>

`depth` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current deserialization depth\.

#### Returns
[System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')  
The deserialized C\# object\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when the value is not an object or instance creation fails\.