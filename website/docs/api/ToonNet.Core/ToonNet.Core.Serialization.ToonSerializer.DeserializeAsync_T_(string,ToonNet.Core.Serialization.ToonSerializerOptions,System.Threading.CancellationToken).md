#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.DeserializeAsync\<T\>\(string, ToonSerializerOptions, CancellationToken\) Method

Asynchronously deserializes a TOON format string to an object\.

```csharp
public static System.Threading.Tasks.ValueTask<T?> DeserializeAsync{T}(string toonString, ToonNet.Core.Serialization.ToonSerializerOptions? options=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T'></a>

`T`

The type to deserialize to\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).toonString'></a>

`toonString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The TOON format string to deserialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional deserialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Threading\.Tasks\.ValueTask&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')[T](ToonNet.Core.Serialization.ToonSerializer.DeserializeAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).md#ToonNet.Core.Serialization.ToonSerializer.DeserializeAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeAsync\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.T')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')  
A ValueTask that represents the asynchronous deserialization operation\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when parsing fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.

### Remarks
This method performs CPU\-bound synchronous deserialization with an async signature for API consistency
and cancellation support\. The ValueTask optimization ensures zero allocation in the common case\.
For I/O\-bound operations, use [DeserializeFromFileAsync&lt;T&gt;\(string, ToonSerializerOptions, CancellationToken\)](ToonNet.Core.Serialization.ToonSerializer.DeserializeFromFileAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).md 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeFromFileAsync\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)') or [DeserializeFromStreamAsync&lt;T&gt;\(Stream, ToonSerializerOptions, CancellationToken\)](ToonNet.Core.Serialization.ToonSerializer.DeserializeFromStreamAsync_T_(System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).md 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeFromStreamAsync\<T\>\(System\.IO\.Stream, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)')\.