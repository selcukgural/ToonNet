### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConverter](ToonNet.Extensions.Yaml.ToonYamlConverter.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter')

## ToonYamlConverter\.ToYaml Method

| Overloads | |
| :--- | :--- |
| [ToYaml\(ToonDocument\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml.md#ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml(ToonNet.Core.Models.ToonDocument) 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ToYaml\(ToonNet\.Core\.Models\.ToonDocument\)') | Converts a TOON document to a YAML string\. |
| [ToYaml\(ToonValue\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml.md#ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml(ToonNet.Core.Models.ToonValue) 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ToYaml\(ToonNet\.Core\.Models\.ToonValue\)') | Converts a TOON value to a YAML string\. |

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml(ToonNet.Core.Models.ToonDocument)'></a>

## ToonYamlConverter\.ToYaml\(ToonDocument\) Method

Converts a TOON document to a YAML string\.

```csharp
public static string ToYaml(ToonNet.Core.Models.ToonDocument document);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml(ToonNet.Core.Models.ToonDocument).document'></a>

`document` [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument')

The [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument') to convert\. This parameter must not be null\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
A YAML string representation of the TOON document\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the [document](ToonNet.Extensions.Yaml.ToonYamlConverter.md#ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml(ToonNet.Core.Models.ToonDocument).document 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ToYaml\(ToonNet\.Core\.Models\.ToonDocument\)\.document') parameter is null\.

### Remarks
This method serializes the root value of the provided [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument') into a YAML string\.

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml(ToonNet.Core.Models.ToonValue)'></a>

## ToonYamlConverter\.ToYaml\(ToonValue\) Method

Converts a TOON value to a YAML string\.

```csharp
public static string ToYaml(ToonNet.Core.Models.ToonValue value);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml(ToonNet.Core.Models.ToonValue).value'></a>

`value` [ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue')

The TOON value to convert\. This parameter must not be null\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
A YAML string representation of the TOON value\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the [value](ToonNet.Extensions.Yaml.ToonYamlConverter.md#ToonNet.Extensions.Yaml.ToonYamlConverter.ToYaml(ToonNet.Core.Models.ToonValue).value 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ToYaml\(ToonNet\.Core\.Models\.ToonValue\)\.value') parameter is null\.

### Example

```csharp
var toonValue = new ToonString("example");
var yamlString = ToonYamlConverter.ToYaml(toonValue);
Console.WriteLine(yamlString);
```

### Remarks
This method uses the YamlDotNet library to serialize the provided TOON value into a YAML string\.
The TOON value is first converted to a plain \.NET object using [ConvertToonValueToObject\(ToonValue\)](ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertToonValueToObject(ToonNet.Core.Models.ToonValue).md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter\.ConvertToonValueToObject\(ToonNet\.Core\.Models\.ToonValue\)')\.
The resulting object is then serialized into YAML format using a configured serializer\.