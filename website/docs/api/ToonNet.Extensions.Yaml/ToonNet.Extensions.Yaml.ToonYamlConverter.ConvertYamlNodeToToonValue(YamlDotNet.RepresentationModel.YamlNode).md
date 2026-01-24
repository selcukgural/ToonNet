### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConverter](ToonNet.Extensions.Yaml.ToonYamlConverter.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter')

## ToonYamlConverter\.ConvertYamlNodeToToonValue\(YamlNode\) Method

Converts a YamlNode to a ToonValue\.

```csharp
private static ToonNet.Core.Models.ToonValue ConvertYamlNodeToToonValue(YamlDotNet.RepresentationModel.YamlNode node);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertYamlNodeToToonValue(YamlDotNet.RepresentationModel.YamlNode).node'></a>

`node` [YamlDotNet\.RepresentationModel\.YamlNode](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.representationmodel.yamlnode 'YamlDotNet\.RepresentationModel\.YamlNode')

The YAML node to convert\. This parameter must not be null\.

#### Returns
[ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue')  
A [ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue') representing the YAML node\.

#### Exceptions

[YamlDotNet\.Core\.YamlException](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.core.yamlexception 'YamlDotNet\.Core\.YamlException')  
Thrown when an unsupported YAML node type is encountered\.

### Remarks
This method uses a switch expression to determine the type of the provided YAML node
and delegates the conversion to the appropriate helper method\. Supported node types
include scalar, mapping, and sequence nodes\. If the node type is unsupported, a
[YamlDotNet\.Core\.YamlException](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.core.yamlexception 'YamlDotNet\.Core\.YamlException') is thrown\.