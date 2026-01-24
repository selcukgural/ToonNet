### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConverter](ToonNet.Extensions.Yaml.ToonYamlConverter.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter')

## ToonYamlConverter\.ConvertYamlMapping\(YamlMappingNode\) Method

Converts a YamlMappingNode to a ToonObject\.

```csharp
private static ToonNet.Core.Models.ToonObject ConvertYamlMapping(YamlDotNet.RepresentationModel.YamlMappingNode mapping);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertYamlMapping(YamlDotNet.RepresentationModel.YamlMappingNode).mapping'></a>

`mapping` [YamlDotNet\.RepresentationModel\.YamlMappingNode](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.representationmodel.yamlmappingnode 'YamlDotNet\.RepresentationModel\.YamlMappingNode')

The YAML mapping node to convert\. This parameter must not be null\.

#### Returns
[ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject')  
A [ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject') containing the key\-value pairs from the YAML mapping node\.

### Example

```csharp
var yamlMapping = new YamlMappingNode
{
    { new YamlScalarNode("key1"), new YamlScalarNode("value1") },
    { new YamlScalarNode("key2"), new YamlScalarNode("value2") }
};
var toonObject = ConvertYamlMapping(yamlMapping);
Console.WriteLine(toonObject["key1"]); // Outputs: value1
```

### Remarks
This method iterates through the children of the provided [YamlDotNet\.RepresentationModel\.YamlMappingNode](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.representationmodel.yamlmappingnode 'YamlDotNet\.RepresentationModel\.YamlMappingNode') and converts
each key\-value pair into a corresponding entry in the [ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject')\. Keys must be scalar nodes
and non\-empty strings; otherwise, they are skipped\. Values are recursively converted using
[ConvertYamlNodeToToonValue\(YamlNode\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertYamlNodeToToonValue(YamlDotNet.RepresentationModel.YamlNode).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertYamlNodeToToonValue\(YamlDotNet\.RepresentationModel\.YamlNode\)')\.