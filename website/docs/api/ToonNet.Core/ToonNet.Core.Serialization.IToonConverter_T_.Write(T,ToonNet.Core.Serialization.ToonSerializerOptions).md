#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[IToonConverter&lt;T&gt;](ToonNet.Core.Serialization.IToonConverter_T_.md 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>')

## IToonConverter\<T\>\.Write\(T, ToonSerializerOptions\) Method

Writes a strongly typed value to its TOON representation\.

```csharp
ToonNet.Core.Models.ToonValue? Write(T? value, ToonNet.Core.Serialization.ToonSerializerOptions options);
```
#### Parameters

<a name='ToonNet.Core.Serialization.IToonConverter_T_.Write(T,ToonNet.Core.Serialization.ToonSerializerOptions).value'></a>

`value` [T](ToonNet.Core.Serialization.IToonConverter_T_.md#ToonNet.Core.Serialization.IToonConverter_T_.T 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>\.T')

The value to write\. Can be null\.

<a name='ToonNet.Core.Serialization.IToonConverter_T_.Write(T,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

The serialization options to use\.

#### Returns
[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')  
The TOON representation of the value, or null if the value cannot be serialized\.