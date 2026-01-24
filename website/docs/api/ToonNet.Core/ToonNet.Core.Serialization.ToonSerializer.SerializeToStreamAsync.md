#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.SerializeToStreamAsync Method

| Overloads | |
| :--- | :--- |
| [SerializeToStreamAsync\(Type, object, Stream, ToonSerializerOptions, CancellationToken\)](ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync.md#ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync(System.Type,object,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken) 'ToonNet\.Core\.Serialization\.ToonSerializer\.SerializeToStreamAsync\(System\.Type, object, System\.IO\.Stream, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)') | Asynchronously serializes an object and writes it to a stream \(non\-generic overload\)\. |
| [SerializeToStreamAsync&lt;T&gt;\(T, Stream, ToonSerializerOptions, CancellationToken\)](ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync.md#ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync_T_(T,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken) 'ToonNet\.Core\.Serialization\.ToonSerializer\.SerializeToStreamAsync\<T\>\(T, System\.IO\.Stream, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)') | Asynchronously serializes an object and writes it to a stream\. |

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync(System.Type,object,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken)'></a>

## ToonSerializer\.SerializeToStreamAsync\(Type, object, Stream, ToonSerializerOptions, CancellationToken\) Method

Asynchronously serializes an object and writes it to a stream \(non\-generic overload\)\.

```csharp
public static System.Threading.Tasks.ValueTask SerializeToStreamAsync(System.Type type, object? value, System.IO.Stream stream, ToonNet.Core.Serialization.ToonSerializerOptions? options=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync(System.Type,object,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).type'></a>

`type` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The type of object to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync(System.Type,object,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).value'></a>

`value` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The value to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync(System.Type,object,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).stream'></a>

`stream` [System\.IO\.Stream](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream 'System\.IO\.Stream')

The stream to write the serialized data to\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync(System.Type,object,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional serialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync(System.Type,object,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Threading\.Tasks\.ValueTask](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask 'System\.Threading\.Tasks\.ValueTask')  
A ValueTask that represents the asynchronous serialization and write operation\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the [type](ToonNet.Core.Serialization.ToonSerializer.md#ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync(System.Type,object,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).type 'ToonNet\.Core\.Serialization\.ToonSerializer\.SerializeToStreamAsync\(System\.Type, object, System\.IO\.Stream, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.type') or [stream](ToonNet.Core.Serialization.ToonSerializer.md#ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync(System.Type,object,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).stream 'ToonNet\.Core\.Serialization\.ToonSerializer\.SerializeToStreamAsync\(System\.Type, object, System\.IO\.Stream, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.stream') is null\.

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when serialization fails\.

[System\.IO\.IOException](https://learn.microsoft.com/en-us/dotnet/api/system.io.ioexception 'System\.IO\.IOException')  
Thrown when stream I/O fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync_T_(T,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken)'></a>

## ToonSerializer\.SerializeToStreamAsync\<T\>\(T, Stream, ToonSerializerOptions, CancellationToken\) Method

Asynchronously serializes an object and writes it to a stream\.

```csharp
public static System.Threading.Tasks.ValueTask SerializeToStreamAsync{T}(T? value, System.IO.Stream stream, ToonNet.Core.Serialization.ToonSerializerOptions? options=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync_T_(T,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T'></a>

`T`

The type of object to serialize\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync_T_(T,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).value'></a>

`value` [T](ToonNet.Core.Serialization.ToonSerializer.md#ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync_T_(T,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.SerializeToStreamAsync\<T\>\(T, System\.IO\.Stream, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.T')

The value to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync_T_(T,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).stream'></a>

`stream` [System\.IO\.Stream](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream 'System\.IO\.Stream')

The stream to write to\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync_T_(T,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional serialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync_T_(T,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Threading\.Tasks\.ValueTask](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask 'System\.Threading\.Tasks\.ValueTask')  
A ValueTask that represents the asynchronous serialization and write operation\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the stream is null\.

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when serialization fails\.

[System\.IO\.IOException](https://learn.microsoft.com/en-us/dotnet/api/system.io.ioexception 'System\.IO\.IOException')  
Thrown when stream I/O fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.