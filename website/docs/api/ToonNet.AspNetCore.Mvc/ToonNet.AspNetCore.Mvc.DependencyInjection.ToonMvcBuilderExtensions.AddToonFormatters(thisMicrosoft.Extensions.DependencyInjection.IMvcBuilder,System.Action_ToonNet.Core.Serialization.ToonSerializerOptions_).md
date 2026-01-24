#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.DependencyInjection](ToonNet.AspNetCore.Mvc.DependencyInjection.md 'ToonNet\.AspNetCore\.Mvc\.DependencyInjection').[ToonMvcBuilderExtensions](ToonNet.AspNetCore.Mvc.DependencyInjection.ToonMvcBuilderExtensions.md 'ToonNet\.AspNetCore\.Mvc\.DependencyInjection\.ToonMvcBuilderExtensions')

## ToonMvcBuilderExtensions\.AddToonFormatters\(this IMvcBuilder, Action\<ToonSerializerOptions\>\) Method

Adds TOON input and output formatters to MVC\.

```csharp
public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddToonFormatters(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<ToonNet.Core.Serialization.ToonSerializerOptions>? configureOptions=null);
```
#### Parameters

<a name='ToonNet.AspNetCore.Mvc.DependencyInjection.ToonMvcBuilderExtensions.AddToonFormatters(thisMicrosoft.Extensions.DependencyInjection.IMvcBuilder,System.Action_ToonNet.Core.Serialization.ToonSerializerOptions_).builder'></a>

`builder` [Microsoft\.Extensions\.DependencyInjection\.IMvcBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.imvcbuilder 'Microsoft\.Extensions\.DependencyInjection\.IMvcBuilder')

The MVC builder instance\.

<a name='ToonNet.AspNetCore.Mvc.DependencyInjection.ToonMvcBuilderExtensions.AddToonFormatters(thisMicrosoft.Extensions.DependencyInjection.IMvcBuilder,System.Action_ToonNet.Core.Serialization.ToonSerializerOptions_).configureOptions'></a>

`configureOptions` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

An optional action to configure TOON serializer options\.

#### Returns
[Microsoft\.Extensions\.DependencyInjection\.IMvcBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.imvcbuilder 'Microsoft\.Extensions\.DependencyInjection\.IMvcBuilder')  
The MVC builder instance for chaining further configuration\.