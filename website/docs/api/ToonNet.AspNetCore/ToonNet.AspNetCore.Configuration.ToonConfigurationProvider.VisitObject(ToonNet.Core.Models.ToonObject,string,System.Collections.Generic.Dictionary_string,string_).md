#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.Configuration](ToonNet.AspNetCore.Configuration.md 'ToonNet\.AspNetCore\.Configuration').[ToonConfigurationProvider](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider')

## ToonConfigurationProvider\.VisitObject\(ToonObject, string, Dictionary\<string,string\>\) Method

Processes a TOON object by visiting its properties and adding flattened key\-value pairs to the provided dictionary\.

```csharp
private static void VisitObject(ToonNet.Core.Models.ToonObject obj, string prefix, System.Collections.Generic.Dictionary<string,string?> data);
```
#### Parameters

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitObject(ToonNet.Core.Models.ToonObject,string,System.Collections.Generic.Dictionary_string,string_).obj'></a>

`obj` [ToonNet\.Core\.Models\.ToonObject](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonobject 'ToonNet\.Core\.Models\.ToonObject')

The TOON object to process\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitObject(ToonNet.Core.Models.ToonObject,string,System.Collections.Generic.Dictionary_string,string_).prefix'></a>

`prefix` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The prefix to prepend to the keys of the object's properties\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitObject(ToonNet.Core.Models.ToonObject,string,System.Collections.Generic.Dictionary_string,string_).data'></a>

`data` [System\.Collections\.Generic\.Dictionary&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[,](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')

The dictionary where flattened key\-value pairs will be stored\.