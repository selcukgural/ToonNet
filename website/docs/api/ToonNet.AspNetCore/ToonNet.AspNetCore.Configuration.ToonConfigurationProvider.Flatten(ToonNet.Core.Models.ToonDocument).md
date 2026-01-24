#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.Configuration](ToonNet.AspNetCore.Configuration.md 'ToonNet\.AspNetCore\.Configuration').[ToonConfigurationProvider](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider')

## ToonConfigurationProvider\.Flatten\(ToonDocument\) Method

Flattens a ToonDocument into a dictionary of key\-value pairs\.

```csharp
private static System.Collections.Generic.Dictionary<string,string?> Flatten(ToonNet.Core.Models.ToonDocument doc);
```
#### Parameters

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.Flatten(ToonNet.Core.Models.ToonDocument).doc'></a>

`doc` [ToonNet\.Core\.Models\.ToonDocument](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toondocument 'ToonNet\.Core\.Models\.ToonDocument')

The ToonDocument to flatten\. This represents the structured configuration data\.

#### Returns
[System\.Collections\.Generic\.Dictionary&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[,](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')  
A dictionary where keys represent the flattened paths of the configuration properties
and values represent the corresponding values as strings or null\.