#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.Configuration](ToonNet.AspNetCore.Configuration.md 'ToonNet\.AspNetCore\.Configuration')

## ToonConfigurationSource Class

Represents a configuration source for a TOON file, leveraging the functionality
of [Microsoft\.Extensions\.Configuration\.FileConfigurationSource](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.fileconfigurationsource 'Microsoft\.Extensions\.Configuration\.FileConfigurationSource') to manage configuration data in a TOON\-specific format\.

```csharp
public sealed class ToonConfigurationSource : Microsoft.Extensions.Configuration.FileConfigurationSource
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [Microsoft\.Extensions\.Configuration\.FileConfigurationSource](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.fileconfigurationsource 'Microsoft\.Extensions\.Configuration\.FileConfigurationSource') &#129106; ToonConfigurationSource

| Properties | |
| :--- | :--- |
| [Options](ToonNet.AspNetCore.Configuration.ToonConfigurationSource.Options.md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationSource\.Options') | Specifies the configuration options to be used when parsing a TOON file\. |

| Methods | |
| :--- | :--- |
| [Build\(IConfigurationBuilder\)](ToonNet.AspNetCore.Configuration.ToonConfigurationSource.Build(Microsoft.Extensions.Configuration.IConfigurationBuilder).md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationSource\.Build\(Microsoft\.Extensions\.Configuration\.IConfigurationBuilder\)') | Builds the [Microsoft\.Extensions\.Configuration\.IConfigurationProvider](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationprovider 'Microsoft\.Extensions\.Configuration\.IConfigurationProvider') for this source\. |
