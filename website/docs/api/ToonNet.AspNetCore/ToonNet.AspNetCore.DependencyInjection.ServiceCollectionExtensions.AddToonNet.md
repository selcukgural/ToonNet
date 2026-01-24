#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.DependencyInjection](ToonNet.AspNetCore.DependencyInjection.md 'ToonNet\.AspNetCore\.DependencyInjection').[ServiceCollectionExtensions](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.md 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions')

## ServiceCollectionExtensions\.AddToonNet Method

| Overloads | |
| :--- | :--- |
| [AddToonNet\(this IServiceCollection\)](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet.md#ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection) 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions\.AddToonNet\(this Microsoft\.Extensions\.DependencyInjection\.IServiceCollection\)') | Registers ToonNet services with default configurations\. |
| [AddToonNet\(this IServiceCollection, IConfiguration, string\)](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet.md#ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,Microsoft.Extensions.Configuration.IConfiguration,string) 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions\.AddToonNet\(this Microsoft\.Extensions\.DependencyInjection\.IServiceCollection, Microsoft\.Extensions\.Configuration\.IConfiguration, string\)') | Registers ToonNet services and binds options from configuration\. |
| [AddToonNet\(this IServiceCollection, Action&lt;ToonOptions&gt;, Action&lt;ToonSerializerOptions&gt;\)](ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet.md#ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_ToonNet.Core.ToonOptions_,System.Action_ToonNet.Core.Serialization.ToonSerializerOptions_) 'ToonNet\.AspNetCore\.DependencyInjection\.ServiceCollectionExtensions\.AddToonNet\(this Microsoft\.Extensions\.DependencyInjection\.IServiceCollection, System\.Action\<ToonNet\.Core\.ToonOptions\>, System\.Action\<ToonNet\.Core\.Serialization\.ToonSerializerOptions\>\)') | Registers ToonNet services and binds options from configuration\. |

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection)'></a>

## ServiceCollectionExtensions\.AddToonNet\(this IServiceCollection\) Method

Registers ToonNet services with default configurations\.

```csharp
public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddToonNet(this Microsoft.Extensions.DependencyInjection.IServiceCollection services);
```
#### Parameters

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection).services'></a>

`services` [Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')

The service collection\.

#### Returns
[Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')  
The same service collection to allow chaining\.

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,Microsoft.Extensions.Configuration.IConfiguration,string)'></a>

## ServiceCollectionExtensions\.AddToonNet\(this IServiceCollection, IConfiguration, string\) Method

Registers ToonNet services and binds options from configuration\.

```csharp
public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddToonNet(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration, string sectionName="ToonNet");
```
#### Parameters

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,Microsoft.Extensions.Configuration.IConfiguration,string).services'></a>

`services` [Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')

The service collection\.

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,Microsoft.Extensions.Configuration.IConfiguration,string).configuration'></a>

`configuration` [Microsoft\.Extensions\.Configuration\.IConfiguration](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfiguration 'Microsoft\.Extensions\.Configuration\.IConfiguration')

The configuration root\.

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,Microsoft.Extensions.Configuration.IConfiguration,string).sectionName'></a>

`sectionName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The root section name\. Default is `ToonNet`\.

#### Returns
[Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')  
The same service collection to allow chaining\.

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_ToonNet.Core.ToonOptions_,System.Action_ToonNet.Core.Serialization.ToonSerializerOptions_)'></a>

## ServiceCollectionExtensions\.AddToonNet\(this IServiceCollection, Action\<ToonOptions\>, Action\<ToonSerializerOptions\>\) Method

Registers ToonNet services and binds options from configuration\.

```csharp
public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddToonNet(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<ToonNet.Core.ToonOptions>? configureToonOptions, System.Action<ToonNet.Core.Serialization.ToonSerializerOptions>? configureSerializerOptions);
```
#### Parameters

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_ToonNet.Core.ToonOptions_,System.Action_ToonNet.Core.Serialization.ToonSerializerOptions_).services'></a>

`services` [Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')

The service collection\.

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_ToonNet.Core.ToonOptions_,System.Action_ToonNet.Core.Serialization.ToonSerializerOptions_).configureToonOptions'></a>

`configureToonOptions` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[ToonNet\.Core\.ToonOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.toonoptions 'ToonNet\.Core\.ToonOptions')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

An optional action to configure Toon options\.

<a name='ToonNet.AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddToonNet(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_ToonNet.Core.ToonOptions_,System.Action_ToonNet.Core.Serialization.ToonSerializerOptions_).configureSerializerOptions'></a>

`configureSerializerOptions` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

An optional action to configure serialization options\.

#### Returns
[Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')  
The same service collection to allow chaining\.