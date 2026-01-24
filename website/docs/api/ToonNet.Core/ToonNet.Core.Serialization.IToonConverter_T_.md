#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization')

## IToonConverter\<T\> Interface

Generic converter interface for strongly typed conversions\.

```csharp
public interface IToonConverter{T} : ToonNet.Core.Serialization.IToonConverter
```
#### Type parameters

<a name='ToonNet.Core.Serialization.IToonConverter_T_.T'></a>

`T`

The type this converter handles\.

Derived  
&#8627; [ToonConverter&lt;T&gt;](ToonNet.Core.Serialization.ToonConverter_T_.md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>')

Implements [IToonConverter](ToonNet.Core.Serialization.IToonConverter.md 'ToonNet\.Core\.Serialization\.IToonConverter')

| Methods | |
| :--- | :--- |
| [Read\(ToonValue, ToonSerializerOptions\)](ToonNet.Core.Serialization.IToonConverter_T_.Read(ToonNet.Core.Models.ToonValue,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>\.Read\(ToonNet\.Core\.Models\.ToonValue, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Reads a TOON value and converts it to a strongly typed object\. |
| [Write\(T, ToonSerializerOptions\)](ToonNet.Core.Serialization.IToonConverter_T_.Write(T,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>\.Write\(T, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Writes a strongly typed value to its TOON representation\. |
