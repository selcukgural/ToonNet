#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

## ToonSerializerOptions\.GetConverter\(Type\) Method

Gets a converter for the specified type\.

```csharp
public ToonNet.Core.Serialization.IToonConverter? GetConverter(System.Type type);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializerOptions.GetConverter(System.Type).type'></a>

`type` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The type to find a converter for\.

#### Returns
[IToonConverter](ToonNet.Core.Serialization.IToonConverter.md 'ToonNet\.Core\.Serialization\.IToonConverter')  
The first converter that can handle the type, or `null` if none is found\.