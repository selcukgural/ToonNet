#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.TryDeserializeDictionary\(ToonValue, Type, ToonSerializerOptions, int, object\) Method

Attempts to deserialize a TOON object as a dictionary\.

```csharp
private static bool TryDeserializeDictionary(ToonNet.Core.Models.ToonValue value, System.Type targetType, ToonNet.Core.Serialization.ToonSerializerOptions options, int depth, out object? result);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.TryDeserializeDictionary(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int,object).value'></a>

`value` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

The TOON value to deserialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.TryDeserializeDictionary(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int,object).targetType'></a>

`targetType` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The target dictionary type\.

<a name='ToonNet.Core.Serialization.ToonSerializer.TryDeserializeDictionary(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int,object).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Deserialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.TryDeserializeDictionary(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int,object).depth'></a>

`depth` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current deserialization depth\.

<a name='ToonNet.Core.Serialization.ToonSerializer.TryDeserializeDictionary(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions,int,object).result'></a>

`result` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The deserialized dictionary if successful\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if the value was successfully deserialized as a dictionary; otherwise, false\.