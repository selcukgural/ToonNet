#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[IToonConverter&lt;T&gt;](ToonNet.Core.Serialization.IToonConverter_T_.md 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>')

## IToonConverter\<T\>\.Read\(ToonValue, ToonSerializerOptions\) Method

Reads a TOON value and converts it to a strongly typed object\.

```csharp
T? Read(ToonNet.Core.Models.ToonValue value, ToonNet.Core.Serialization.ToonSerializerOptions options);
```
#### Parameters

<a name='ToonNet.Core.Serialization.IToonConverter_T_.Read(ToonNet.Core.Models.ToonValue,ToonNet.Core.Serialization.ToonSerializerOptions).value'></a>

`value` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

The TOON value to read\.

<a name='ToonNet.Core.Serialization.IToonConverter_T_.Read(ToonNet.Core.Models.ToonValue,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

The deserialization options to use\.

#### Returns
[T](ToonNet.Core.Serialization.IToonConverter_T_.md#ToonNet.Core.Serialization.IToonConverter_T_.T 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>\.T')  
The deserialized object, or null if the value cannot be deserialized\.