#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.SerializeValue\(object, Type, ToonSerializerOptions, int\) Method

Serializes a value to its TOON representation\.

```csharp
private static ToonNet.Core.Models.ToonValue? SerializeValue(object? value, System.Type type, ToonNet.Core.Serialization.ToonSerializerOptions options, int depth);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeValue(object,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int).value'></a>

`value` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The value to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeValue(object,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int).type'></a>

`type` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The type of the value\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeValue(object,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Serialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeValue(object,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int).depth'></a>

`depth` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current serialization depth\.

#### Returns
[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')  
A ToonValue representing the serialized value\.

#### Exceptions

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when serialization fails or depth exceeded\.