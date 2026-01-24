#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.Configuration](ToonNet.AspNetCore.Configuration.md 'ToonNet\.AspNetCore\.Configuration')

## ToonConfigurationProvider Class

Provides a configuration source for TOON files, allowing configuration
settings to be loaded into an application from a TOON\-formatted file\.

```csharp
public sealed class ToonConfigurationProvider : Microsoft.Extensions.Configuration.FileConfigurationProvider
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [Microsoft\.Extensions\.Configuration\.ConfigurationProvider](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.configurationprovider 'Microsoft\.Extensions\.Configuration\.ConfigurationProvider') &#129106; [Microsoft\.Extensions\.Configuration\.FileConfigurationProvider](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.fileconfigurationprovider 'Microsoft\.Extensions\.Configuration\.FileConfigurationProvider') &#129106; ToonConfigurationProvider

| Constructors | |
| :--- | :--- |
| [ToonConfigurationProvider\(ToonConfigurationSource, ToonOptions\)](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.ToonConfigurationProvider(ToonNet.AspNetCore.Configuration.ToonConfigurationSource,ToonNet.Core.ToonOptions).md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider\.ToonConfigurationProvider\(ToonNet\.AspNetCore\.Configuration\.ToonConfigurationSource, ToonNet\.Core\.ToonOptions\)') | Provides configuration using the TOON file format\. |

| Fields | |
| :--- | :--- |
| [\_options](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider._options.md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider\.\_options') | Represents a set of configuration options used for parsing TOON configuration files\. |

| Methods | |
| :--- | :--- |
| [Flatten\(ToonDocument\)](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.Flatten(ToonNet.Core.Models.ToonDocument).md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider\.Flatten\(ToonNet\.Core\.Models\.ToonDocument\)') | Flattens a ToonDocument into a dictionary of key\-value pairs\. |
| [Load\(Stream\)](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.Load(System.IO.Stream).md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider\.Load\(System\.IO\.Stream\)') | Loads configuration data from the provided stream\. |
| [VisitArray\(ToonArray, string, Dictionary&lt;string,string&gt;\)](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitArray(ToonNet.Core.Models.ToonArray,string,System.Collections.Generic.Dictionary_string,string_).md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider\.VisitArray\(ToonNet\.Core\.Models\.ToonArray, string, System\.Collections\.Generic\.Dictionary\<string,string\>\)') | Visits the elements of a [ToonNet\.Core\.Models\.ToonArray](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.models.toonarray 'ToonNet\.Core\.Models\.ToonArray') and processes each element while maintaining a hierarchical key structure\. |
| [VisitObject\(ToonObject, string, Dictionary&lt;string,string&gt;\)](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitObject(ToonNet.Core.Models.ToonObject,string,System.Collections.Generic.Dictionary_string,string_).md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider\.VisitObject\(ToonNet\.Core\.Models\.ToonObject, string, System\.Collections\.Generic\.Dictionary\<string,string\>\)') | Processes a TOON object by visiting its properties and adding flattened key\-value pairs to the provided dictionary\. |
| [VisitValue\(ToonValue, string, Dictionary&lt;string,string&gt;\)](ToonNet.AspNetCore.Configuration.ToonConfigurationProvider.VisitValue(ToonNet.Core.Models.ToonValue,string,System.Collections.Generic.Dictionary_string,string_).md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationProvider\.VisitValue\(ToonNet\.Core\.Models\.ToonValue, string, System\.Collections\.Generic\.Dictionary\<string,string\>\)') | Processes a ToonValue instance and adds its data representation to the given dictionary\. |
