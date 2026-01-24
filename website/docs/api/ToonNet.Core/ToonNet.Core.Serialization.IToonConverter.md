#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization')

## IToonConverter Interface

Base interface for type converters\.

```csharp
public interface IToonConverter
```

Derived  
&#8627; [IToonConverter&lt;T&gt;](ToonNet.Core.Serialization.IToonConverter_T_.md 'ToonNet\.Core\.Serialization\.IToonConverter\<T\>')  
&#8627; [ToonConverter&lt;T&gt;](ToonNet.Core.Serialization.ToonConverter_T_.md 'ToonNet\.Core\.Serialization\.ToonConverter\<T\>')

| Methods | |
| :--- | :--- |
| [CanConvert\(Type\)](ToonNet.Core.Serialization.IToonConverter.CanConvert(System.Type).md 'ToonNet\.Core\.Serialization\.IToonConverter\.CanConvert\(System\.Type\)') | Determines whether this converter can handle the specified type\. |
| [Read\(ToonValue, Type, ToonSerializerOptions\)](ToonNet.Core.Serialization.IToonConverter.Read(ToonNet.Core.Models.ToonValue,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.IToonConverter\.Read\(ToonNet\.Core\.Models\.ToonValue, System\.Type, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Reads a TOON value and converts it to a C\# object\. |
| [Write\(object, ToonSerializerOptions\)](ToonNet.Core.Serialization.IToonConverter.Write(object,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.Core\.Serialization\.IToonConverter\.Write\(object, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Writes an object to its TOON representation\. |
