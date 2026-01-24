#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.DeserializeFromStreamAsync\<T\>\(Stream, ToonSerializerOptions, CancellationToken\) Method

Asynchronously reads from a stream and deserializes the content to an object\.

```csharp
public static System.Threading.Tasks.ValueTask<T?> DeserializeFromStreamAsync{T}(System.IO.Stream stream, ToonNet.Core.Serialization.ToonSerializerOptions? options=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeFromStreamAsync_T_(System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T'></a>

`T`

The type to deserialize to\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeFromStreamAsync_T_(System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).stream'></a>

`stream` [System\.IO\.Stream](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream 'System\.IO\.Stream')

The stream to read from\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeFromStreamAsync_T_(System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional deserialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeFromStreamAsync_T_(System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Threading\.Tasks\.ValueTask&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')[T](ToonNet.Core.Serialization.ToonSerializer.DeserializeFromStreamAsync_T_(System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).md#ToonNet.Core.Serialization.ToonSerializer.DeserializeFromStreamAsync_T_(System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeFromStreamAsync\<T\>\(System\.IO\.Stream, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.T')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')  
A task that represents the asynchronous read and deserialization operation\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the stream is null\.

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when parsing fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.