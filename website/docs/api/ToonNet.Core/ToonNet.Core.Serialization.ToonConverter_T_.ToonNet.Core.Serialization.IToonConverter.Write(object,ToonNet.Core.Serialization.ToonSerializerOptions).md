#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonConverter&lt;T&gt;](ToonNet.Core.Serialization.ToonConverter_T_.md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>')

## ToonNet\.Core\.Serialization\.IToonConverter\.Write\(object, ToonSerializerOptions\) Method

Non\-generic write implementation that delegates to the strongly typed version\.

```csharp
ToonNet.Core.Models.ToonValue? ToonNet.Core.Serialization.IToonConverter.Write(object? value, ToonNet.Core.Serialization.ToonSerializerOptions options);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonConverter_T_.ToonNet.Core.Serialization.IToonConverter.Write(object,ToonNet.Core.Serialization.ToonSerializerOptions).value'></a>

`value` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The value to write\. Can be null\.

<a name='ToonNet.Core.Serialization.ToonConverter_T_.ToonNet.Core.Serialization.IToonConverter.Write(object,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

The serialization options to use\.

Implements [Write\(object, ToonSerializerOptions\)](ToonNet.Core.Serialization.IToonConverter.Write(object,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.IToonConverter\.Write\(object, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)')