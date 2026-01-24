#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.DeserializeFromFileAsync\<T\>\(string, ToonSerializerOptions, CancellationToken\) Method

Asynchronously reads a file and deserializes its contents to an object\.

```csharp
public static System.Threading.Tasks.ValueTask<T?> DeserializeFromFileAsync{T}(string filePath, ToonNet.Core.Serialization.ToonSerializerOptions? options=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeFromFileAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T'></a>

`T`

The type to deserialize to\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeFromFileAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).filePath'></a>

`filePath` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The file path to read from\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeFromFileAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional deserialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeFromFileAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Threading\.Tasks\.ValueTask&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')[T](ToonNet.Core.Serialization.ToonSerializer.DeserializeFromFileAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).md#ToonNet.Core.Serialization.ToonSerializer.DeserializeFromFileAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeFromFileAsync\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.T')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')  
A task that represents the asynchronous read and deserialization operation\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when filePath is null\.

[System\.IO\.FileNotFoundException](https://learn.microsoft.com/en-us/dotnet/api/system.io.filenotfoundexception 'System\.IO\.FileNotFoundException')  
Thrown when the file does not exist\.

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when parsing fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.