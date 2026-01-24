#### [ToonNet\.AspNetCore](index.md 'index')
### [ToonNet\.AspNetCore\.Configuration](ToonNet.AspNetCore.Configuration.md 'ToonNet\.AspNetCore\.Configuration').[ToonConfigurationExtensions](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions')

## ToonConfigurationExtensions\.AddToonFile Method

| Overloads | |
| :--- | :--- |
| [AddToonFile\(this IConfigurationBuilder, IFileProvider, string, bool, bool, ToonOptions\)](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions) 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, Microsoft\.Extensions\.FileProviders\.IFileProvider, string, bool, bool, ToonNet\.Core\.ToonOptions\)') | Adds a TOON configuration source to [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, Microsoft\.Extensions\.FileProviders\.IFileProvider, string, bool, bool, ToonNet\.Core\.ToonOptions\)\.builder')\. |
| [AddToonFile\(this IConfigurationBuilder, string\)](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string) 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string\)') | Adds the TOON configuration provider at [path](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string).path 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string\)\.path') to [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string\)\.builder')\. |
| [AddToonFile\(this IConfigurationBuilder, string, bool\)](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool) 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool\)') | Adds the TOON configuration provider at [path](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool).path 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool\)\.path') to [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool\)\.builder')\. |
| [AddToonFile\(this IConfigurationBuilder, string, bool, bool, ToonOptions\)](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions) 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool, bool, ToonNet\.Core\.ToonOptions\)') | Adds the TOON configuration provider at [path](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions).path 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool, bool, ToonNet\.Core\.ToonOptions\)\.path') to [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool, bool, ToonNet\.Core\.ToonOptions\)\.builder')\. |

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions)'></a>

## ToonConfigurationExtensions\.AddToonFile\(this IConfigurationBuilder, IFileProvider, string, bool, bool, ToonOptions\) Method

Adds a TOON configuration source to [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, Microsoft\.Extensions\.FileProviders\.IFileProvider, string, bool, bool, ToonNet\.Core\.ToonOptions\)\.builder')\.

```csharp
public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddToonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, Microsoft.Extensions.FileProviders.IFileProvider? provider, string path, bool optional, bool reloadOnChange, ToonNet.Core.ToonOptions? options=null);
```
#### Parameters

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions).builder'></a>

`builder` [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')

The [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder') to add to\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions).provider'></a>

`provider` [Microsoft\.Extensions\.FileProviders\.IFileProvider](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.fileproviders.ifileprovider 'Microsoft\.Extensions\.FileProviders\.IFileProvider')

The [Microsoft\.Extensions\.FileProviders\.IFileProvider](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.fileproviders.ifileprovider 'Microsoft\.Extensions\.FileProviders\.IFileProvider') to use to access the file\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions).path'></a>

`path` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Path relative to the base path stored in [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder\.Properties](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder.properties 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder\.Properties') of [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, Microsoft\.Extensions\.FileProviders\.IFileProvider, string, bool, bool, ToonNet\.Core\.ToonOptions\)\.builder')\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions).optional'></a>

`optional` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

Whether the file is optional\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions).reloadOnChange'></a>

`reloadOnChange` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

Whether the configuration should be reloaded if the file changes\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,Microsoft.Extensions.FileProviders.IFileProvider,string,bool,bool,ToonNet.Core.ToonOptions).options'></a>

`options` [ToonNet\.Core\.ToonOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.toonoptions 'ToonNet\.Core\.ToonOptions')

Options for parsing the TOON file\.

#### Returns
[Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')  
The [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string)'></a>

## ToonConfigurationExtensions\.AddToonFile\(this IConfigurationBuilder, string\) Method

Adds the TOON configuration provider at [path](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string).path 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string\)\.path') to [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string\)\.builder')\.

```csharp
public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddToonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path);
```
#### Parameters

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string).builder'></a>

`builder` [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')

The [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder') to add to\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string).path'></a>

`path` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Path relative to the base path stored in [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder\.Properties](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder.properties 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder\.Properties') of [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string\)\.builder')\.

#### Returns
[Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')  
The [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool)'></a>

## ToonConfigurationExtensions\.AddToonFile\(this IConfigurationBuilder, string, bool\) Method

Adds the TOON configuration provider at [path](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool).path 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool\)\.path') to [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool\)\.builder')\.

```csharp
public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddToonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional);
```
#### Parameters

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool).builder'></a>

`builder` [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')

The [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder') to add to\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool).path'></a>

`path` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Path relative to the base path stored in [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder\.Properties](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder.properties 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder\.Properties') of [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool\)\.builder')\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool).optional'></a>

`optional` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

Whether the file is optional\.

#### Returns
[Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')  
The [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions)'></a>

## ToonConfigurationExtensions\.AddToonFile\(this IConfigurationBuilder, string, bool, bool, ToonOptions\) Method

Adds the TOON configuration provider at [path](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions).path 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool, bool, ToonNet\.Core\.ToonOptions\)\.path') to [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool, bool, ToonNet\.Core\.ToonOptions\)\.builder')\.

```csharp
public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddToonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange, ToonNet.Core.ToonOptions? options=null);
```
#### Parameters

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions).builder'></a>

`builder` [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')

The [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder') to add to\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions).path'></a>

`path` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Path relative to the base path stored in [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder\.Properties](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder.properties 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder\.Properties') of [builder](ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.md#ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions).builder 'ToonNet\.AspNetCore\.Configuration\.ToonConfigurationExtensions\.AddToonFile\(this Microsoft\.Extensions\.Configuration\.IConfigurationBuilder, string, bool, bool, ToonNet\.Core\.ToonOptions\)\.builder')\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions).optional'></a>

`optional` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

Whether the file is optional\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions).reloadOnChange'></a>

`reloadOnChange` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

Whether the configuration should be reloaded if the file changes\.

<a name='ToonNet.AspNetCore.Configuration.ToonConfigurationExtensions.AddToonFile(thisMicrosoft.Extensions.Configuration.IConfigurationBuilder,string,bool,bool,ToonNet.Core.ToonOptions).options'></a>

`options` [ToonNet\.Core\.ToonOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.toonoptions 'ToonNet\.Core\.ToonOptions')

Options for parsing the TOON file\.

#### Returns
[Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')  
The [Microsoft\.Extensions\.Configuration\.IConfigurationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationbuilder 'Microsoft\.Extensions\.Configuration\.IConfigurationBuilder')\.