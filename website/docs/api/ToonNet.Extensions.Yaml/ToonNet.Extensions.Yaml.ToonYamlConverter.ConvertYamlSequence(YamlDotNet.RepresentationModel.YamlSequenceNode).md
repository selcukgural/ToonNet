### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConverter](ToonNet.Extensions.Yaml.ToonYamlConverter.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter')

## ToonYamlConverter\.ConvertYamlSequence\(YamlSequenceNode\) Method

Converts a YamlSequenceNode to a ToonArray\.

```csharp
private static ToonNet.Core.Models.ToonArray ConvertYamlSequence(YamlDotNet.RepresentationModel.YamlSequenceNode sequence);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertYamlSequence(YamlDotNet.RepresentationModel.YamlSequenceNode).sequence'></a>

`sequence` [YamlDotNet\.RepresentationModel\.YamlSequenceNode](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.representationmodel.yamlsequencenode 'YamlDotNet\.RepresentationModel\.YamlSequenceNode')

The YAML sequence node to convert\. This parameter must not be null\.

#### Returns
[ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray')  
A [ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray') containing the sequence items\. Each item in the sequence
is converted to a [ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue') using the [ConvertYamlNodeToToonValue\(YamlNode\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertYamlNodeToToonValue(YamlDotNet.RepresentationModel.YamlNode).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertYamlNodeToToonValue\(YamlDotNet\.RepresentationModel\.YamlNode\)') method\.

### Example

```csharp
var yamlSequence = new YamlSequenceNode
{
    new YamlScalarNode("item1"),
    new YamlScalarNode("item2")
};
var toonArray = ConvertYamlSequence(yamlSequence);
Console.WriteLine(toonArray[0]); // Outputs: item1
```

### Remarks
This method iterates through the children of the provided [YamlDotNet\.RepresentationModel\.YamlSequenceNode](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.representationmodel.yamlsequencenode 'YamlDotNet\.RepresentationModel\.YamlSequenceNode') and converts
each child node into a corresponding [ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue')\. The resulting values are added to a
[ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray') which is then returned\. This method assumes that the input sequence node is valid
and does not perform additional validation\.