#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.SerializeCollectionToFileAsync\<T\>\(IEnumerable\<T\>, string, ToonSerializerOptions, CancellationToken\) Method

Asynchronously serializes a collection of objects to a file with each object separated by a blank line\.

```csharp
public static System.Threading.Tasks.ValueTask SerializeCollectionToFileAsync{T}(System.Collections.Generic.IEnumerable{T} values, string filePath, ToonNet.Core.Serialization.ToonSerializerOptions? options=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeCollectionToFileAsync_T_(System.Collections.Generic.IEnumerable_T_,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T'></a>

`T`

The type of objects to serialize\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeCollectionToFileAsync_T_(System.Collections.Generic.IEnumerable_T_,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).values'></a>

`values` [System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[T](ToonNet.Core.Serialization.ToonSerializer.SerializeCollectionToFileAsync_T_(System.Collections.Generic.IEnumerable_T_,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).md#ToonNet.Core.Serialization.ToonSerializer.SerializeCollectionToFileAsync_T_(System.Collections.Generic.IEnumerable_T_,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.SerializeCollectionToFileAsync\<T\>\(System\.Collections\.Generic\.IEnumerable\<T\>, string, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.T')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')

The collection of values to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeCollectionToFileAsync_T_(System.Collections.Generic.IEnumerable_T_,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).filePath'></a>

`filePath` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The file path to write to\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeCollectionToFileAsync_T_(System.Collections.Generic.IEnumerable_T_,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional serialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.SerializeCollectionToFileAsync_T_(System.Collections.Generic.IEnumerable_T_,string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Threading\.Tasks\.ValueTask](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask 'System\.Threading\.Tasks\.ValueTask')  
A ValueTask that represents the asynchronous serialization and write operation\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when values or filePath is null\.

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when serialization fails\.

[System\.IO\.IOException](https://learn.microsoft.com/en-us/dotnet/api/system.io.ioexception 'System\.IO\.IOException')  
Thrown when file I/O fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.

### Remarks
This method writes each object as a separate TOON document, separated by blank lines\.
This format is compatible with DeserializeStreamAsync for reading back multiple objects\.