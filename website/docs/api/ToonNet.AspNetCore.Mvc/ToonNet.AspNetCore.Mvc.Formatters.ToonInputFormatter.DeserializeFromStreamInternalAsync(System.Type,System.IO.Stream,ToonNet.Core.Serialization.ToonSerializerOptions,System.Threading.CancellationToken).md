#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Formatters](ToonNet.AspNetCore.Mvc.Formatters.md 'ToonNet\.AspNetCore\.Mvc\.Formatters').[ToonInputFormatter](ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonInputFormatter')

## ToonInputFormatter\.DeserializeFromStreamInternalAsync\(Type, Stream, ToonSerializerOptions, CancellationToken\) Method

Deserializes the content of a stream into an object of the specified type using the provided options\.

```csharp
private static System.Threading.Tasks.Task<object?> DeserializeFromStreamInternalAsync(System.Type type, System.IO.Stream stream, ToonNet.Core.Serialization.ToonSerializerOptions options, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.DeserializeFromStreamInternalAsync(System.Type,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).type'></a>

`type` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The target type of the deserialization\.

<a name='ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.DeserializeFromStreamInternalAsync(System.Type,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).stream'></a>

`stream` [System\.IO\.Stream](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream 'System\.IO\.Stream')

The input stream containing the serialized data\.

<a name='ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.DeserializeFromStreamInternalAsync(System.Type,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

The options to control deserialization behavior\.

<a name='ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.DeserializeFromStreamInternalAsync(System.Type,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token to observe while waiting for the task to complete\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The deserialized object, or null if the stream is empty\.