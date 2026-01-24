#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonConverter&lt;T&gt;](ToonNet.Core.Serialization.ToonConverter_T_.md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>')

## ToonNet\.Core\.Serialization\.IToonConverter\.Read\(ToonValue, Type, ToonSerializerOptions\) Method

Non\-generic read implementation that delegates to the strongly typed version\.

```csharp
object? ToonNet.Core.Serialization.IToonConverter.Read(ToonNet.Core.Models.ToonValue value, System.Type targetType, ToonNet.Core.Serialization.ToonSerializerOptions options);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonConverter_T_.ToonNet.Core.Serialization.IToonConverter.Read(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).value'></a>

`value` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

The TOON value to read\.

<a name='ToonNet.Core.Serialization.ToonConverter_T_.ToonNet.Core.Serialization.IToonConverter.Read(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).targetType'></a>

`targetType` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The target type \(ignored, T is used\)\.

<a name='ToonNet.Core.Serialization.ToonConverter_T_.ToonNet.Core.Serialization.IToonConverter.Read(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

The deserialization options to use\.

Implements [Read\(ToonValue, Type, ToonSerializerOptions\)](ToonNet.Core.Serialization.IToonConverter.Read(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.IToonConverter\.Read\(ToonNet\.Core\.Models\.ToonValue, System\.Type, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)')