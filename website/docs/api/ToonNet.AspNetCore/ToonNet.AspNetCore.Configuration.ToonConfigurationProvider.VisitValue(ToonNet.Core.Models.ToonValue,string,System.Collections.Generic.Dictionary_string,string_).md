#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.Configuration](ToonNet.AspNetCore.Configuration.md 'ToonNet\.AspNetCore\.Configuration').[ToonConfigurationProvider](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider')

## ToonConfigurationProvider\.VisitValue\(ToonValue, string, Dictionary\<string,string\>\) Method

Processes a ToonValue instance and adds its data representation to the given dictionary\.

```csharp
private static void VisitValue(ToonNet.Core.Models.ToonValue value, string key, System.Collections.Generic.Dictionary<string,string?> data);
```
#### Parameters

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitValue(ToonNet.Core.Models.ToonValue,string,System.Collections.Generic.Dictionary_string,string_).value'></a>

`value` [ToonNet\.Core\.Models\.ToonValue](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonvalue 'ToonNet\.Core\.Models\.ToonValue')

The ToonValue to be processed\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitValue(ToonNet.Core.Models.ToonValue,string,System.Collections.Generic.Dictionary_string,string_).key'></a>

`key` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The configuration key associated with the value\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitValue(ToonNet.Core.Models.ToonValue,string,System.Collections.Generic.Dictionary_string,string_).data'></a>

`data` [System\.Collections\.Generic\.Dictionary&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[,](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')

A dictionary where the processed configuration data will be stored\.
Keys and corresponding values are added to this dictionary based on the structure of the ToonValue\.