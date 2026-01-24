### [ToonNet\.Extensions\.Yaml](ToonNet.Extensions.Yaml.md 'ToonNet\.Extensions\.Yaml').[ToonYamlConverter](ToonNet.Extensions.Yaml.ToonYamlConverter.md 'ToonNet\.Extensions\.Yaml\.ToonYamlConverter')

## ToonYamlConverter\.ConvertToonValueToObject\(ToonValue\) Method

Converts a ToonValue to a plain \.NET object for YamlDotNet serialization\.

```csharp
private static object? ConvertToonValueToObject(ToonNet.Core.Models.ToonValue value);
```
#### Parameters

<a name='ToonNet.Extensions.Yaml.ToonYamlConverter.ConvertToonValueToObject(ToonNet.Core.Models.ToonValue).value'></a>

`value` [ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue')

The TOON value to convert\. This parameter must not be null\.

#### Returns
[System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')  
A plain \.NET object representing the TOON value\. The returned object can be:
\- null for [ToonNet\.Core\.Models\.ToonNull](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonnull 'ToonNet\.Core\.Models\.ToonNull')
\- a boolean for [ToonNet\.Core\.Models\.ToonBoolean](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonboolean 'ToonNet\.Core\.Models\.ToonBoolean')
\- a double for [ToonNet\.Core\.Models\.ToonNumber](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonnumber 'ToonNet\.Core\.Models\.ToonNumber')
\- a string for [ToonNet\.Core\.Models\.ToonString](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonstring 'ToonNet\.Core\.Models\.ToonString')
\- a Dictionary for [ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject')
\- a List for [ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray')

#### Exceptions

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
Thrown when the provided TOON value type is unsupported\.

### Remarks
This method uses a switch expression to determine the type of the provided TOON value
and converts it to the corresponding \.NET object\. Unsupported TOON value types will
result in an exception\.