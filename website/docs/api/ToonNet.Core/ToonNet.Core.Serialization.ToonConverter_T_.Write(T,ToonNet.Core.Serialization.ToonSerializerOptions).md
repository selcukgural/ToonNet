#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonConverter&lt;T&gt;](ToonNet.Core.Serialization.ToonConverter_T_.md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>')

## ToonConverter\<T\>\.Write\(T, ToonSerializerOptions\) Method

Writes a strongly typed value to its TOON representation\.

```csharp
public abstract ToonNet.Core.Models.ToonValue? Write(T? value, ToonNet.Core.Serialization.ToonSerializerOptions options);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonConverter_T_.Write(T,ToonNet.Core.Serialization.ToonSerializerOptions).value'></a>

`value` [T](ToonNet.Core.Serialization.ToonConverter_T_.md#ToonNet.Core.Serialization.ToonConverter_T_.T 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>\.T')

The value to write\. Can be null\.

<a name='ToonNet.Core.Serialization.ToonConverter_T_.Write(T,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

The serialization options to use\.

Implements [Write\(T, ToonSerializerOptions\)](ToonNet.Core.Serialization.IToonConverter_T_.Write(T,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>\.Write\(T, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)')

#### Returns
[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')  
The TOON representation of the value, or null if the value cannot be serialized\.