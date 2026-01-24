#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.GetPropertyName\(PropertyInfo, ToonSerializerOptions\) Method

Gets the serialized name for a property based on naming policy and attributes\.

```csharp
private static string GetPropertyName(System.Reflection.PropertyInfo property, ToonNet.Core.Serialization.ToonSerializerOptions options);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.GetPropertyName(System.Reflection.PropertyInfo,ToonNet.Core.Serialization.ToonSerializerOptions).property'></a>

`property` [System\.Reflection\.PropertyInfo](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.propertyinfo 'System\.Reflection\.PropertyInfo')

The property to get the name for\.

<a name='ToonNet.Core.Serialization.ToonSerializer.GetPropertyName(System.Reflection.PropertyInfo,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Serialization options containing naming policy\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
The name to use in TOON format\.