### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConvert](ToonNet.Extensions.Yaml.ToonYamlConvert.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConvert')

## ToonYamlConvert\.FromYaml\(string, ToonOptions\) Method

Converts a YAML string directly to TOON format string\.

```csharp
public static string FromYaml(string yamlString, ToonNet.Core.ToonOptions? options=null);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConvert.FromYaml(string,ToonNet.Core.ToonOptions).yamlString'></a>

`yamlString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The YAML string to convert\.

<a name='ToonNet.Extensions.Yaml.ToonYamlConvert.FromYaml(string,ToonNet.Core.ToonOptions).options'></a>

`options` [ToonNet\.Core\.ToonOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.toonoptions 'ToonNet\.Core\.ToonOptions')

Optional TOON encoding options\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
TOON format string\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when yamlString is null\.

[YamlDotNet\.Core\.YamlException](https://learn.microsoft.com/en-us/dotnet/api/yamldotnet.core.yamlexception 'YamlDotNet\.Core\.YamlException')  
Thrown when YAML parsing fails\.

### Remarks
This method provides a simple, developer\-friendly API for YAML to TOON conversion:

```csharp
string toonString = ToonYamlConvert.FromYaml(yamlString);
```
No need to deal with ToonDocument or ToonEncoder \- just like System\.Text\.Json\!