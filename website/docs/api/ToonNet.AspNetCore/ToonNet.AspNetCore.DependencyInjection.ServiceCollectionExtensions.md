#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.DependencyInjection](ToonNet.AspNetCore.DependencyInjection.md 'ToonNet\.AspNetCore\.DependencyInjection')

## ServiceCollectionExtensions Class

Provides extension methods for registering ToonNet services with ASP\.NET Core dependency injection\.

```csharp
public static class ServiceCollectionExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ServiceCollectionExtensions

| Fields | |
| :--- | :--- |
| [DefaultSectionName](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.DefaultSectionName.md 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions\.DefaultSectionName') | Represents the default section name used for binding configuration options related to ToonNet\. |

| Methods | |
| :--- | :--- |
| [AddToonNet\(this IServiceCollection\)](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet.md#ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection) 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions\.AddToonNet\(this Microsoft\.Extensions\.DependencyInjection\.IServiceCollection\)') | Registers ToonNet services with default configurations\. |
| [AddToonNet\(this IServiceCollection, IConfiguration, string\)](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet.md#ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,Microsoft.Extensions.Configuration.IConfiguration,string) 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions\.AddToonNet\(this Microsoft\.Extensions\.DependencyInjection\.IServiceCollection, Microsoft\.Extensions\.Configuration\.IConfiguration, string\)') | Registers ToonNet services and binds options from configuration\. |
| [AddToonNet\(this IServiceCollection, Action&lt;ToonOptions&gt;, Action&lt;ToonSerializerOptions&gt;\)](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet.md#ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_ToonNet.Core.ToonOptions_,System.Action_ToonNet.Core.Serialization.ToonSerializerOptions_) 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions\.AddToonNet\(this Microsoft\.Extensions\.DependencyInjection\.IServiceCollection, System\.Action\<ToonNet\.Core\.ToonOptions\>, System\.Action\<ToonNet\.Core\.Serialization\.ToonSerializerOptions\>\)') | Registers ToonNet services and binds options from configuration\. |
| [ApplyToonOptionsFromConfiguration\(ToonOptions, IConfigurationSection\)](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.ApplyToonOptionsFromConfiguration(ToonNet.Core.ToonOptions,Microsoft.Extensions.Configuration.IConfigurationSection).md 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions\.ApplyToonOptionsFromConfiguration\(ToonNet\.Core\.ToonOptions, Microsoft\.Extensions\.Configuration\.IConfigurationSection\)') | Configures the provided `ToonOptions` instance with values from the specified configuration section\. |
| [ApplyToonSerializerOptionsFromConfiguration\(ToonSerializerOptions, IConfigurationSection\)](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.ApplyToonSerializerOptionsFromConfiguration(ToonNet.Core.Serialization.ToonSerializerOptions,Microsoft.Extensions.Configuration.IConfigurationSection).md 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions\.ApplyToonSerializerOptionsFromConfiguration\(ToonNet\.Core\.Serialization\.ToonSerializerOptions, Microsoft\.Extensions\.Configuration\.IConfigurationSection\)') | Configures the specified `ToonSerializerOptions` instance with values from the provided configuration section\. |
