#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.DependencyInjection](ToonNet.AspNetCore.DependencyInjection.md 'ToonNet\.AspNetCore\.DependencyInjection').[ServiceCollectionExtensions](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.md 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions')

## ServiceCollectionExtensions\.ApplyToonSerializerOptionsFromConfiguration\(ToonSerializerOptions, IConfigurationSection\) Method

Configures the specified `ToonSerializerOptions` instance with values from the provided configuration section\.

```csharp
private static void ApplyToonSerializerOptionsFromConfiguration(ToonNet.Core.Serialization.ToonSerializerOptions options, Microsoft.Extensions.Configuration.IConfigurationSection section);
```
#### Parameters

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.ApplyToonSerializerOptionsFromConfiguration(ToonNet.Core.Serialization.ToonSerializerOptions,Microsoft.Extensions.Configuration.IConfigurationSection).options'></a>

`options` [ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

The `ToonSerializerOptions` instance to configure\.

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.ApplyToonSerializerOptionsFromConfiguration(ToonNet.Core.Serialization.ToonSerializerOptions,Microsoft.Extensions.Configuration.IConfigurationSection).section'></a>

`section` [Microsoft\.Extensions\.Configuration\.IConfigurationSection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationsection 'Microsoft\.Extensions\.Configuration\.IConfigurationSection')

The configuration section containing serializer option values\.