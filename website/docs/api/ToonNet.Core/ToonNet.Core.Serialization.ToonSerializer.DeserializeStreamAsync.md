#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.DeserializeStreamAsync Method

| Overloads | |
| :--- | :--- |
| [DeserializeStreamAsync&lt;T&gt;\(string, ToonSerializerOptions, CancellationToken\)](ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync.md#ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken) 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeStreamAsync\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)') | Asynchronously deserializes a stream of TOON objects from a file\. |
| [DeserializeStreamAsync&lt;T&gt;\(string, ToonSerializerOptions, ToonMultiDocumentReadOptions, CancellationToken\)](ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync.md#ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken) 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeStreamAsync\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions, ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions, System\.Threading\.CancellationToken\)') | Asynchronously deserializes a stream of TOON objects from a file using multi\-document options\. |
| [DeserializeStreamAsync&lt;T&gt;\(StreamReader, ToonSerializerOptions, CancellationToken\)](ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync.md#ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken) 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeStreamAsync\<T\>\(System\.IO\.StreamReader, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)') | Asynchronously deserializes a stream of TOON objects from a StreamReader\. |
| [DeserializeStreamAsync&lt;T&gt;\(StreamReader, ToonSerializerOptions, ToonMultiDocumentReadOptions, CancellationToken\)](ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync.md#ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken) 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeStreamAsync\<T\>\(System\.IO\.StreamReader, ToonNet\.Core\.Serialization\.ToonSerializerOptions, ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions, System\.Threading\.CancellationToken\)') | Asynchronously deserializes a stream of TOON objects from a StreamReader using multi\-document options\. |

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken)'></a>

## ToonSerializer\.DeserializeStreamAsync\<T\>\(string, ToonSerializerOptions, CancellationToken\) Method

Asynchronously deserializes a stream of TOON objects from a file\.

```csharp
public static System.Collections.Generic.IAsyncEnumerable<T?> DeserializeStreamAsync{T}(string filePath, ToonNet.Core.Serialization.ToonSerializerOptions? options=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T'></a>

`T`

The type to deserialize to\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).filePath'></a>

`filePath` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The file path to read from\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional deserialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Collections\.Generic\.IAsyncEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1 'System\.Collections\.Generic\.IAsyncEnumerable\`1')[T](ToonNet.Core.Serialization.ToonSerializer.md#ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeStreamAsync\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.T')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1 'System\.Collections\.Generic\.IAsyncEnumerable\`1')  
An async enumerable of deserialized objects\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when filePath is null\.

[System\.IO\.FileNotFoundException](https://learn.microsoft.com/en-us/dotnet/api/system.io.filenotfoundexception 'System\.IO\.FileNotFoundException')  
Thrown when the file does not exist\.

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when parsing fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.

### Remarks
This method assumes the file contains multiple TOON objects separated by blank lines\.
Each object is parsed and yielded individually, making it memory\-efficient for large files\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken)'></a>

## ToonSerializer\.DeserializeStreamAsync\<T\>\(string, ToonSerializerOptions, ToonMultiDocumentReadOptions, CancellationToken\) Method

Asynchronously deserializes a stream of TOON objects from a file using multi\-document options\.

```csharp
public static System.Collections.Generic.IAsyncEnumerable<T?> DeserializeStreamAsync{T}(string filePath, ToonNet.Core.Serialization.ToonSerializerOptions? options, ToonNet.Core.Serialization.ToonMultiDocumentReadOptions multiDocumentOptions, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).T'></a>

`T`

The type to deserialize to\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).filePath'></a>

`filePath` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The file path to read from\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional deserialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).multiDocumentOptions'></a>

`multiDocumentOptions` [ToonMultiDocumentReadOptions](ToonNet.Core.Serialization.ToonMultiDocumentReadOptions.md 'ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions')

Options that control how multiple TOON documents are delimited\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Collections\.Generic\.IAsyncEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1 'System\.Collections\.Generic\.IAsyncEnumerable\`1')[T](ToonNet.Core.Serialization.ToonSerializer.md#ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeStreamAsync\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions, ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions, System\.Threading\.CancellationToken\)\.T')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1 'System\.Collections\.Generic\.IAsyncEnumerable\`1')  
An async enumerable of deserialized objects\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when filePath or multiDocumentOptions is null\.

[System\.IO\.FileNotFoundException](https://learn.microsoft.com/en-us/dotnet/api/system.io.filenotfoundexception 'System\.IO\.FileNotFoundException')  
Thrown when the file does not exist\.

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when parsing fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.

### Remarks
This overload supports both legacy blank\-line separation and deterministic explicit separator lines \(for example: `---`\)\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken)'></a>

## ToonSerializer\.DeserializeStreamAsync\<T\>\(StreamReader, ToonSerializerOptions, CancellationToken\) Method

Asynchronously deserializes a stream of TOON objects from a StreamReader\.

```csharp
public static System.Collections.Generic.IAsyncEnumerable<T?> DeserializeStreamAsync{T}(System.IO.StreamReader reader, ToonNet.Core.Serialization.ToonSerializerOptions? options=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T'></a>

`T`

The type to deserialize to\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).reader'></a>

`reader` [System\.IO\.StreamReader](https://learn.microsoft.com/en-us/dotnet/api/system.io.streamreader 'System\.IO\.StreamReader')

The StreamReader to read from\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional deserialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Collections\.Generic\.IAsyncEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1 'System\.Collections\.Generic\.IAsyncEnumerable\`1')[T](ToonNet.Core.Serialization.ToonSerializer.md#ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeStreamAsync\<T\>\(System\.IO\.StreamReader, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)\.T')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1 'System\.Collections\.Generic\.IAsyncEnumerable\`1')  
An async enumerable of deserialized objects\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the reader is null\.

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when parsing fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.

### Remarks
This method assumes the stream contains multiple TOON objects separated by blank lines\.
Each object is parsed and yielded individually, making it memory\-efficient for large streams\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken)'></a>

## ToonSerializer\.DeserializeStreamAsync\<T\>\(StreamReader, ToonSerializerOptions, ToonMultiDocumentReadOptions, CancellationToken\) Method

Asynchronously deserializes a stream of TOON objects from a StreamReader using multi\-document options\.

```csharp
public static System.Collections.Generic.IAsyncEnumerable<T?> DeserializeStreamAsync{T}(System.IO.StreamReader reader, ToonNet.Core.Serialization.ToonSerializerOptions? options, ToonNet.Core.Serialization.ToonMultiDocumentReadOptions multiDocumentOptions, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).T'></a>

`T`

The type to deserialize to\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).reader'></a>

`reader` [System\.IO\.StreamReader](https://learn.microsoft.com/en-us/dotnet/api/system.io.streamreader 'System\.IO\.StreamReader')

The StreamReader to read from\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional deserialization options\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).multiDocumentOptions'></a>

`multiDocumentOptions` [ToonMultiDocumentReadOptions](ToonNet.Core.Serialization.ToonMultiDocumentReadOptions.md 'ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions')

Options that control how multiple TOON documents are delimited\.

<a name='ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Collections\.Generic\.IAsyncEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1 'System\.Collections\.Generic\.IAsyncEnumerable\`1')[T](ToonNet.Core.Serialization.ToonSerializer.md#ToonNet.Core.Serialization.ToonSerializer.DeserializeStreamAsync_T_(System.IO.StreamReader,ToonNet.Core.Serialization.ToonSerializerOptions,ToonNet.Core.Serialization.ToonMultiDocumentReadOptions,System.Threading.CancellationToken).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.DeserializeStreamAsync\<T\>\(System\.IO\.StreamReader, ToonNet\.Core\.Serialization\.ToonSerializerOptions, ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions, System\.Threading\.CancellationToken\)\.T')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1 'System\.Collections\.Generic\.IAsyncEnumerable\`1')  
An async enumerable of deserialized objects\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the reader or multiDocumentOptions is null\.

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when parsing fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.

### Remarks
When using [ExplicitSeparator](ToonNet.Core.Serialization.ToonMultiDocumentSeparatorMode.md#ToonNet.Core.Serialization.ToonMultiDocumentSeparatorMode.ExplicitSeparator 'ToonNet\.Core\.Serialization\.ToonMultiDocumentSeparatorMode\.ExplicitSeparator'), a separator line is recognized only when the line matches exactly\.