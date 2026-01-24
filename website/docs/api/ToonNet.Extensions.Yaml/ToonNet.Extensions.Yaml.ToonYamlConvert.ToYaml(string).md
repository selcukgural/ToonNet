### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConvert](ToonNet.Extensions.Yaml.ToonYamlConvert.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConvert')

## ToonYamlConvert\.ToYaml\(string\) Method

Converts a TOON format string to YAML format string\.

```csharp
public static string ToYaml(string toonString);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConvert.ToYaml(string).toonString'></a>

`toonString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The TOON string to convert\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
YAML format string\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when toonString is null\.

[ToonNet\.Core\.ToonParseException](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.toonparseexception 'ToonNet\.Core\.ToonParseException')  
Thrown when TOON parsing fails\.

### Remarks
This method provides a simple, developer\-friendly API for TOON to YAML conversion:

```csharp
string yamlString = ToonYamlConvert.ToYaml(toonString);
```