#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonConverter&lt;T&gt;](ToonNet.Core.Serialization.ToonConverter_T_.md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>')

## ToonConverter\<T\>\.CanConvert\(Type\) Method

Determines whether this converter can handle the specified type\.

```csharp
public virtual bool CanConvert(System.Type type);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonConverter_T_.CanConvert(System.Type).type'></a>

`type` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The type to check\.

Implements [CanConvert\(Type\)](ToonNet.Core.Serialization.IToonConverter.CanConvert(System.Type).md 'ToonNet\.Core\.Serialization\.IToonConverter\.CanConvert\(System\.Type\)')

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if the type is assignable to T; otherwise, false\.