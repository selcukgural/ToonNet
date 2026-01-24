#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization')

## ToonConverter\<T\> Class

Base class for implementing type converters\.

```csharp
public abstract class ToonConverter{T} : ToonNet.Core.Serialization.IToonConverter{T}, ToonNet.Core.Serialization.IToonConverter
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonConverter_T_.T'></a>

`T`

The type this converter handles\.

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonConverter\<T\>

Implements [ToonNet\.Core\.Serialization\.IToonConverter&lt;](ToonNet.Core.Serialization.IToonConverter_T_.md 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>')[T](ToonNet.Core.Serialization.ToonConverter_T_.md#ToonNet.Core.Serialization.ToonConverter_T_.T 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>\.T')[&gt;](ToonNet.Core.Serialization.IToonConverter_T_.md 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>'), [IToonConverter](ToonNet.Core.Serialization.IToonConverter.md 'ToonNet\.Core\.Serialization\.IToonConverter')

| Methods | |
| :--- | :--- |
| [CanConvert\(Type\)](ToonNet.Core.Serialization.ToonConverter_T_.CanConvert(System.Type).md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>\.CanConvert\(System\.Type\)') | Determines whether this converter can handle the specified type\. |
| [Read\(ToonValue, ToonSerializerOptions\)](ToonNet.Core.Serialization.ToonConverter_T_.Read(ToonNet.Core.Models.ToonValue,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>\.Read\(ToonNet\.Core\.Models\.ToonValue, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Reads a TOON value and converts it to a strongly typed object\. |
| [Write\(T, ToonSerializerOptions\)](ToonNet.Core.Serialization.ToonConverter_T_.Write(T,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>\.Write\(T, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Writes a strongly typed value to its TOON representation\. |

| Explicit Interface Implementations | |
| :--- | :--- |
| [ToonNet\.Core\.Serialization\.IToonConverter\.Read\(ToonValue, Type, ToonSerializerOptions\)](ToonNet.Core.Serialization.ToonConverter_T_.ToonNet.Core.Serialization.IToonConverter.Read(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>\.ToonNet\.Core\.Serialization\.IToonConverter\.Read\(ToonNet\.Core\.Models\.ToonValue, System\.Type, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Non\-generic read implementation that delegates to the strongly typed version\. |
| [ToonNet\.Core\.Serialization\.IToonConverter\.Write\(object, ToonSerializerOptions\)](ToonNet.Core.Serialization.ToonConverter_T_.ToonNet.Core.Serialization.IToonConverter.Write(object,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>\.ToonNet\.Core\.Serialization\.IToonConverter\.Write\(object, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Non\-generic write implementation that delegates to the strongly typed version\. |
