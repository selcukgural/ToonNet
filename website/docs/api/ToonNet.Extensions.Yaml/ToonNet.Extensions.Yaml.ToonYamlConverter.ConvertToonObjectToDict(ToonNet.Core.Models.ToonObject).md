### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConverter](ToonNet.Extensions.Yaml.ToonYamlConverter.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter')

## ToonYamlConverter\.ConvertToonObjectToDict\(ToonObject\) Method

Converts a ToonObject to a Dictionary for YAML serialization\.

```csharp
private static System.Collections.Generic.Dictionary<string,object?> ConvertToonObjectToDict(ToonNet.Core.Models.ToonObject obj);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertToonObjectToDict(ToonNet.Core.Models.ToonObject).obj'></a>

`obj` [ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject')

The TOON object to convert\. This parameter must not be null\.

#### Returns
[System\.Collections\.Generic\.Dictionary&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[,](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')  
A Dictionary where the keys are strings and the values are plain \.NET objects
representing the properties of the TOON object\.

### Remarks
This method iterates through the properties of the provided TOON object and converts
each property value to a plain \.NET object using [ConvertToonValueToObject\(ToonValue\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertToonValueToObject(ToonNet.Core.Models.ToonValue).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertToonValueToObject\(ToonNet\.Core\.Models\.ToonValue\)')\.