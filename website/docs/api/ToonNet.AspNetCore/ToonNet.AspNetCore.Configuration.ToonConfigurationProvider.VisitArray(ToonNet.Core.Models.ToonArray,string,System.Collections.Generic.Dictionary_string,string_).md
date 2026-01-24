#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.Configuration](ToonNet.AspNetCore.Configuration.md 'ToonNet\.AspNetCore\.Configuration').[ToonConfigurationProvider](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider')

## ToonConfigurationProvider\.VisitArray\(ToonArray, string, Dictionary\<string,string\>\) Method

Visits the elements of a [ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray') and processes each
element while maintaining a hierarchical key structure\.

```csharp
private static void VisitArray(ToonNet.Core.Models.ToonArray array, string prefix, System.Collections.Generic.Dictionary<string,string?> data);
```
#### Parameters

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitArray(ToonNet.Core.Models.ToonArray,string,System.Collections.Generic.Dictionary_string,string_).array'></a>

`array` [ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray')

The array to be visited\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitArray(ToonNet.Core.Models.ToonArray,string,System.Collections.Generic.Dictionary_string,string_).prefix'></a>

`prefix` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The key prefix to be used for the elements in the array\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitArray(ToonNet.Core.Models.ToonArray,string,System.Collections.Generic.Dictionary_string,string_).data'></a>

`data` [System\.Collections\.Generic\.Dictionary&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[,](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')

The dictionary to store key\-value pairs generated from the array elements\.