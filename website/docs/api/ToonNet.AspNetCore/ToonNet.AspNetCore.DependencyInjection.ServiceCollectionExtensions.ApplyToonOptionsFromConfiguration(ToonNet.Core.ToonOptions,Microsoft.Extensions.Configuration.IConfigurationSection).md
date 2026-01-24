#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.DependencyInjection](ToonNet.AspNetCore.DependencyInjection.md 'ToonNet\.AspNetCore\.DependencyInjection').[ServiceCollectionExtensions](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.md 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions')

## ServiceCollectionExtensions\.ApplyToonOptionsFromConfiguration\(ToonOptions, IConfigurationSection\) Method

Configures the provided `ToonOptions` instance with values from the specified configuration section\.

```csharp
private static void ApplyToonOptionsFromConfiguration(ToonNet.Core.ToonOptions options, Microsoft.Extensions.Configuration.IConfigurationSection section);
```
#### Parameters

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.ApplyToonOptionsFromConfiguration(ToonNet.Core.ToonOptions,Microsoft.Extensions.Configuration.IConfigurationSection).options'></a>

`options` [ToonNet\.Core\.ToonOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.toonoptions 'ToonNet\.Core\.ToonOptions')

The `ToonOptions` instance to configure\.

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.ApplyToonOptionsFromConfiguration(ToonNet.Core.ToonOptions,Microsoft.Extensions.Configuration.IConfigurationSection).section'></a>

`section` [Microsoft\.Extensions\.Configuration\.IConfigurationSection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationsection 'Microsoft\.Extensions\.Configuration\.IConfigurationSection')

The configuration section containing the values\.