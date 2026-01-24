#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[IToonConverter](ToonNet.Core.Serialization.IToonConverter.md 'ToonNet\.Core\.Serialization\.IToonConverter')

## IToonConverter\.Write\(object, ToonSerializerOptions\) Method

Writes an object to its TOON representation\.

```csharp
ToonNet.Core.Models.ToonValue? Write(object? value, ToonNet.Core.Serialization.ToonSerializerOptions options);
```
#### Parameters

<a name='ToonNet.Core.Serialization.IToonConverter.Write(object,ToonNet.Core.Serialization.ToonSerializerOptions).value'></a>

`value` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The value to write\. Can be null\.

<a name='ToonNet.Core.Serialization.IToonConverter.Write(object,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

The serialization options to use\.

#### Returns
[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')  
The TOON representation of the value, or null if the value cannot be serialized\.