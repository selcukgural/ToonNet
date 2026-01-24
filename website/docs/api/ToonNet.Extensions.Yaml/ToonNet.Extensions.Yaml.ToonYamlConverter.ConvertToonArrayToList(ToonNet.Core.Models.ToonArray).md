### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConverter](ToonNet.Extensions.Yaml.ToonYamlConverter.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter')

## ToonYamlConverter\.ConvertToonArrayToList\(ToonArray\) Method

Converts a ToonArray to a List for YAML serialization\.

```csharp
private static System.Collections.Generic.List<object?> ConvertToonArrayToList(ToonNet.Core.Models.ToonArray array);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertToonArrayToList(ToonNet.Core.Models.ToonArray).array'></a>

`array` [ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray')

The TOON array to convert\. This parameter must not be null\.

#### Returns
[System\.Collections\.Generic\.List&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1 'System\.Collections\.Generic\.List\`1')[System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1 'System\.Collections\.Generic\.List\`1')  
A List of plain \.NET objects representing the items in the TOON array\.

### Remarks
This method iterates through the items of the provided TOON array and converts
each item to a plain \.NET object using [ConvertToonValueToObject\(ToonValue\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertToonValueToObject(ToonNet.Core.Models.ToonValue).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertToonValueToObject\(ToonNet\.Core\.Models\.ToonValue\)')\.