#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.SerializeAsync\<T\>\(T, ToonSerializerOptions, CancellationToken\) Method

Asynchronously serializes an object to TOON format string\.

```csharp
public static System.Threading.Tasks.ValueTask<string> SerializeAsync{T}(T? value, ToonNet.Core.Serialization.ToonSerializerOptions? options=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeAsync_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T'></a>

`T`

The type of object to serialize\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeAsync_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).value'></a>

`value` [T](ToonNet.Core.Serialization.ToonSerializer.SerializeAsync_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).md#ToonNet.Core.Serialization.ToonSerializer.SerializeAsync_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.SerializeAsync\<T\>\(T, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.T')

The value to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeAsync_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional serialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeAsync_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Threading\.Tasks\.ValueTask&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')  
A ValueTask that represents the asynchronous serialization operation\.

#### Exceptions

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when serialization fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.

### Remarks
This method performs CPU\-bound synchronous serialization with an async signature for API consistency
and cancellation support\. The ValueTask optimization ensures zero allocation in the common case\.
For I/O\-bound operations, use [SerializeToFileAsync&lt;T&gt;\(T, string, ToonSerializerOptions, CancellationToken\)](ToonNet.Core.Serialization.ToonSerializer.SerializeToFileAsync_T_(T,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).md 'ToonNet\.Core\.Serialization\.ToonSerializer\.SerializeToFileAsync\<T\>\(T, string, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)') or [SerializeToStreamAsync&lt;T&gt;\(T, Stream, ToonSerializerOptions, CancellationToken\)](ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync.md#ToonNet.Core.Serialization.ToonSerializer.SerializeToStreamAsync_T_(T,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken) 'ToonNet\.Core\.Serialization\.ToonSerializer\.SerializeToStreamAsync\<T\>\(T, System\.IO\.Stream, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)')\.