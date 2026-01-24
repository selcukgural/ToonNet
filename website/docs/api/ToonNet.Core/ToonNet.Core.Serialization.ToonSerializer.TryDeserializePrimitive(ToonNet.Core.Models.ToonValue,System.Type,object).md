#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.TryDeserializePrimitive\(ToonValue, Type, object\) Method

Attempts to deserialize a primitive value from a [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue') to the specified target type\.

```csharp
private static bool TryDeserializePrimitive(ToonNet.Core.Models.ToonValue value, System.Type targetType, out object? result);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.TryDeserializePrimitive(ToonNet.Core.Models.ToonValue,System.Type,object).value'></a>

`value` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

The [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue') to deserialize\. This value must represent a primitive type such as a string, number, or boolean\.

<a name='ToonNet.Core.Serialization.ToonSerializer.TryDeserializePrimitive(ToonNet.Core.Models.ToonValue,System.Type,object).targetType'></a>

`targetType` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The target C\# type to which the value should be deserialized\. Supported types include primitive types, enums, 
[System\.DateTime](https://learn.microsoft.com/en-us/dotnet/api/system.datetime 'System\.DateTime'), [System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset'), and [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')\.

<a name='ToonNet.Core.Serialization.ToonSerializer.TryDeserializePrimitive(ToonNet.Core.Models.ToonValue,System.Type,object).result'></a>

`result` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

When this method returns, contains the deserialized object if the operation was successful; otherwise, null\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
`true` if the value was successfully deserialized as a primitive; otherwise, `false`\.

### Remarks
This method supports deserialization of the following types:
- String
- Boolean
- Numeric types (e.g., byte, int, long, float, double, decimal)
- DateTime and DateTimeOffset (parsed from ISO 8601 strings)
- Guid
- Enums (parsed from their string representation)

If the [value](ToonNet.Core.Serialization.ToonSerializer.TryDeserializePrimitive(ToonNet.Core.Models.ToonValue,System.Type,object).md#ToonNet.Core.Serialization.ToonSerializer.TryDeserializePrimitive(ToonNet.Core.Models.ToonValue,System.Type,object).value 'ToonNet\.Core\.Serialization\.ToonSerializer\.TryDeserializePrimitive\(ToonNet\.Core\.Models\.ToonValue, System\.Type, object\)\.value') does not match the expected type or cannot be converted, the method returns `false`\.