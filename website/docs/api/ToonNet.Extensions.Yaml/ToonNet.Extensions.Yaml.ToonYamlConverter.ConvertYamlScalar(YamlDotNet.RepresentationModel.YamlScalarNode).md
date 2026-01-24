### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConverter](ToonNet.Extensions.Yaml.ToonYamlConverter.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter')

## ToonYamlConverter\.ConvertYamlScalar\(YamlScalarNode\) Method

Converts a YamlScalarNode to a ToonValue\.

```csharp
private static ToonNet.Core.Models.ToonValue ConvertYamlScalar(YamlDotNet.RepresentationModel.YamlScalarNode scalar);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertYamlScalar(YamlDotNet.RepresentationModel.YamlScalarNode).scalar'></a>

`scalar` [YamlDotNet\.RepresentationModel\.YamlScalarNode](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.representationmodel.yamlscalarnode 'YamlDotNet\.RepresentationModel\.YamlScalarNode')

The YAML scalar node to convert\. This parameter must not be null\.

#### Returns
[ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue')  
A [ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue') representing the scalar value\. The scalar can be converted
to a null, boolean, number, or string value depending on its content\.

### Remarks
This method handles special cases for null/empty values, boolean values, and numeric
values\. If the scalar does not match any of these cases, it is treated as a string\.