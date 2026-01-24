#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.SerializeToFileAsync\<T\>\(T, string, ToonSerializerOptions, CancellationToken\) Method

Asynchronously serializes an object and writes it to a file\.

```csharp
public static System.Threading.Tasks.ValueTask SerializeToFileAsync{T}(T? value, string filePath, ToonNet.Core.Serialization.ToonSerializerOptions? options=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToFileAsync_T_(T,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T'></a>

`T`

The type of object to serialize\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToFileAsync_T_(T,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).value'></a>

`value` [T](ToonNet.Core.Serialization.ToonSerializer.SerializeToFileAsync_T_(T,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).md#ToonNet.Core.Serialization.ToonSerializer.SerializeToFileAsync_T_(T,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.SerializeToFileAsync\<T\>\(T, string, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.T')

The value to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToFileAsync_T_(T,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).filePath'></a>

`filePath` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The file path to write to\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToFileAsync_T_(T,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional serialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeToFileAsync_T_(T,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Threading\.Tasks\.ValueTask](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask 'System\.Threading\.Tasks\.ValueTask')  
A ValueTask that represents the asynchronous serialization and write operation\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when filePath is null\.

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when serialization fails\.

[System\.IO\.IOException](https://learn.microsoft.com/en-us/dotnet/api/system.io.ioexception 'System\.IO\.IOException')  
Thrown when file I/O fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.