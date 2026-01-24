#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonConverter&lt;T&gt;](ToonNet.Core.Serialization.ToonConverter_T_.md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>')

## ToonConverter\<T\>\.Read\(ToonValue, ToonSerializerOptions\) Method

Reads a TOON value and converts it to a strongly typed object\.

```csharp
public abstract T? Read(ToonNet.Core.Models.ToonValue value, ToonNet.Core.Serialization.ToonSerializerOptions options);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonConverter_T_.Read(ToonNet.Core.Models.ToonValue,ToonNet.Core.Serialization.ToonSerializerOptions).value'></a>

`value` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

The TOON value to read\.

<a name='ToonNet.Core.Serialization.ToonConverter_T_.Read(ToonNet.Core.Models.ToonValue,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

The deserialization options to use\.

Implements [Read\(ToonValue, ToonSerializerOptions\)](ToonNet.Core.Serialization.IToonConverter_T_.Read(ToonNet.Core.Models.ToonValue,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>\.Read\(ToonNet\.Core\.Models\.ToonValue, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)')

#### Returns
[T](ToonNet.Core.Serialization.ToonConverter_T_.md#ToonNet.Core.Serialization.ToonConverter_T_.T 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>\.T')  
The deserialized object, or null if the value cannot be deserialized\.