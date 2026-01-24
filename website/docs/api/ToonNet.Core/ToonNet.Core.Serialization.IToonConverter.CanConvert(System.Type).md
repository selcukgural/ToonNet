#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[IToonConverter](ToonNet.Core.Serialization.IToonConverter.md 'ToonNet\.Core\.Serialization\.IToonConverter')

## IToonConverter\.CanConvert\(Type\) Method

Determines whether this converter can handle the specified type\.

```csharp
bool CanConvert(System.Type type);
```
#### Parameters

<a name='ToonNet.Core.Serialization.IToonConverter.CanConvert(System.Type).type'></a>

`type` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The type to check\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if this converter can handle the type; otherwise, false\.