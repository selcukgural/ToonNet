### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConverter](ToonNet.Extensions.Yaml.ToonYamlConverter.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter')

## ToonYamlConverter\.FromYaml\(string\) Method

Converts a YAML string to a TOON document\.

```csharp
public static ToonNet.Core.Models.ToonDocument FromYaml(string yaml);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.FromYaml(string).yaml'></a>

`yaml` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The YAML string to convert\. This parameter must not be null\.

#### Returns
[ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument')  
A [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument') representing the YAML data\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the [yaml](ToonNet.Extensions.Yaml.ToonYamlConverter.FromYaml(string).md#ToonNet.Extensions.Yaml.ToonYamlConverter.FromYaml(string).yaml 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.FromYaml\(string\)\.yaml') parameter is null\.

[YamlDotNet\.Core\.YamlException](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.core.yamlexception 'YamlDotNet\.Core\.YamlException')  
Thrown when YAML parsing fails due to invalid format or other issues\.

### Remarks
This method parses the provided YAML string into a [YamlDotNet\.RepresentationModel\.YamlStream](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.representationmodel.yamlstream 'YamlDotNet\.RepresentationModel\.YamlStream') and converts
its root node into a [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument')\. If the YAML stream contains no documents,
an empty [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument') is returned\.