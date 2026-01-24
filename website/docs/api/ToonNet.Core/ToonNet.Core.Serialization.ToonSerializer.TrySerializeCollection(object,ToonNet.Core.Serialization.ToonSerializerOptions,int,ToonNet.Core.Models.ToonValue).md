#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.TrySerializeCollection\(object, ToonSerializerOptions, int, ToonValue\) Method

Attempts to serialize a collection value\.

```csharp
private static bool TrySerializeCollection(object value, ToonNet.Core.Serialization.ToonSerializerOptions options, int depth, out ToonNet.Core.Models.ToonValue? result);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.TrySerializeCollection(object,ToonNet.Core.Serialization.ToonSerializerOptions,int,ToonNet.Core.Models.ToonValue).value'></a>

`value` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The value to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.TrySerializeCollection(object,ToonNet.Core.Serialization.ToonSerializerOptions,int,ToonNet.Core.Models.ToonValue).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Serialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.TrySerializeCollection(object,ToonNet.Core.Serialization.ToonSerializerOptions,int,ToonNet.Core.Models.ToonValue).depth'></a>

`depth` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current serialization depth\.

<a name='ToonNet.Core.Serialization.ToonSerializer.TrySerializeCollection(object,ToonNet.Core.Serialization.ToonSerializerOptions,int,ToonNet.Core.Models.ToonValue).result'></a>

`result` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

The resulting ToonValue if successful\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if the value was successfully serialized as a collection; otherwise, false\.