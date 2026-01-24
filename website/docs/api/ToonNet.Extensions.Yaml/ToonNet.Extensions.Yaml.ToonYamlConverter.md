### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml')

## ToonYamlConverter Class

Provides bidirectional conversion between YAML and TOON formats\.
Supports YamlDotNet integration for seamless interoperability\.

```csharp
public static class ToonYamlConverter
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonYamlConverter

### Remarks
This static class acts as a utility for converting data between YAML and TOON formats\.
It includes methods for serializing and deserializing TOON documents and values,
ensuring compatibility with the YamlDotNet library\.

| Methods | |
| :--- | :--- |
| [ConvertToonArrayToList\(ToonArray\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertToonArrayToList(ToonNet.Core.Models.ToonArray).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertToonArrayToList\(ToonNet\.Core\.Models\.ToonArray\)') | Converts a ToonArray to a List for YAML serialization\. |
| [ConvertToonObjectToDict\(ToonObject\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertToonObjectToDict(ToonNet.Core.Models.ToonObject).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertToonObjectToDict\(ToonNet\.Core\.Models\.ToonObject\)') | Converts a ToonObject to a Dictionary for YAML serialization\. |
| [ConvertToonValueToObject\(ToonValue\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertToonValueToObject(ToonNet.Core.Models.ToonValue).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertToonValueToObject\(ToonNet\.Core\.Models\.ToonValue\)') | Converts a ToonValue to a plain \.NET object for YamlDotNet serialization\. |
| [ConvertYamlMapping\(YamlMappingNode\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertYamlMapping(YamlDotNet.RepresentationModel.YamlMappingNode).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertYamlMapping\(YamlDotNet\.RepresentationModel\.YamlMappingNode\)') | Converts a YamlMappingNode to a ToonObject\. |
| [ConvertYamlNodeToToonValue\(YamlNode\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertYamlNodeToToonValue(YamlDotNet.RepresentationModel.YamlNode).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertYamlNodeToToonValue\(YamlDotNet\.RepresentationModel\.YamlNode\)') | Converts a YamlNode to a ToonValue\. |
| [ConvertYamlScalar\(YamlScalarNode\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertYamlScalar(YamlDotNet.RepresentationModel.YamlScalarNode).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertYamlScalar\(YamlDotNet\.RepresentationModel\.YamlScalarNode\)') | Converts a YamlScalarNode to a ToonValue\. |
| [ConvertYamlSequence\(YamlSequenceNode\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertYamlSequence(YamlDotNet.RepresentationModel.YamlSequenceNode).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertYamlSequence\(YamlDotNet\.RepresentationModel\.YamlSequenceNode\)') | Converts a YamlSequenceNode to a ToonArray\. |
| [FromYaml\(string\)](ToonNet.Extensions.Yaml.ToonYamlConverter.FromYaml(string).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.FromYaml\(string\)') | Converts a YAML string to a TOON document\. |
| [ToYaml\(ToonDocument\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml.md#ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml(ToonNet.Core.Models.ToonDocument) 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ToYaml\(ToonNet\.Core\.Models\.ToonDocument\)') | Converts a TOON document to a YAML string\. |
| [ToYaml\(ToonValue\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml.md#ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml(ToonNet.Core.Models.ToonValue) 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ToYaml\(ToonNet\.Core\.Models\.ToonValue\)') | Converts a TOON value to a YAML string\. |
