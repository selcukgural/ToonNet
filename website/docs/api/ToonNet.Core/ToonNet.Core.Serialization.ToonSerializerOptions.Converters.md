#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

## ToonSerializerOptions\.Converters Property

Gets the collection of custom converters to use during serialization\.

```csharp
public System.Collections.Generic.List<ToonNet.Core.Serialization.IToonConverter> Converters { get; }
```

#### Property Value
[System\.Collections\.Generic\.List&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1 'System\.Collections\.Generic\.List\`1')[IToonConverter](ToonNet.Core.Serialization.IToonConverter.md 'ToonNet\.Core\.Serialization\.IToonConverter')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1 'System\.Collections\.Generic\.List\`1')

### Remarks
Use [AddConverter\(IToonConverter\)](ToonNet.Core.Serialization.ToonSerializerOptions.AddConverter(ToonNet.Core.Serialization.IToonConverter).md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.AddConverter\(ToonNet\.Core\.Serialization\.IToonConverter\)') to safely add converters with null checking\.