#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

## ToonSerializerOptions\.AddConverter\(IToonConverter\) Method

Adds a custom converter to the collection\.

```csharp
public void AddConverter(ToonNet.Core.Serialization.IToonConverter converter);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializerOptions.AddConverter(ToonNet.Core.Serialization.IToonConverter).converter'></a>

`converter` [IToonConverter](ToonNet.Core.Serialization.IToonConverter.md 'ToonNet\.Core\.Serialization\.IToonConverter')

The converter to add\. Cannot be null\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the converter is null\.