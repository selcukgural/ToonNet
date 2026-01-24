#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EncodeToStreamAsync\(ToonDocument, Stream, CancellationToken\) Method

Asynchronously encodes a TOON document and writes it to a stream\.

```csharp
public System.Threading.Tasks.Task EncodeToStreamAsync(ToonNet.Core.Models.ToonDocument document, System.IO.Stream stream, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeToStreamAsync(ToonNet.Core.Models.ToonDocument,System.IO.Stream,System.Threading.CancellationToken).document'></a>

`document` [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument')

The document to encode\.

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeToStreamAsync(ToonNet.Core.Models.ToonDocument,System.IO.Stream,System.Threading.CancellationToken).stream'></a>

`stream` [System\.IO\.Stream](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream 'System\.IO\.Stream')

The stream to write to\.

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeToStreamAsync(ToonNet.Core.Models.ToonDocument,System.IO.Stream,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A task that represents the asynchronous encoding and write operation\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when a document or stream is null\.

[System\.IO\.IOException](https://learn.microsoft.com/en-us/dotnet/api/system.io.ioexception 'System\.IO\.IOException')  
Thrown when stream I/O fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.