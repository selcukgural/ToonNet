#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.TrySerializePrimitive\(object, ToonValue\) Method

Attempts to serialize a primitive value\.

```csharp
private static bool TrySerializePrimitive(object value, out ToonNet.Core.Models.ToonValue? result);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.TrySerializePrimitive(object,ToonNet.Core.Models.ToonValue).value'></a>

`value` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The value to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.TrySerializePrimitive(object,ToonNet.Core.Models.ToonValue).result'></a>

`result` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

The resulting ToonValue if successful\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if the value was successfully serialized as a primitive; otherwise, false\.