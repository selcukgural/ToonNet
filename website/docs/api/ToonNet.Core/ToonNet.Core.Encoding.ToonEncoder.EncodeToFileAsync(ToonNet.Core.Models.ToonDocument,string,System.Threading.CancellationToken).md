#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EncodeToFileAsync\(ToonDocument, string, CancellationToken\) Method

Asynchronously encodes a TOON document and writes it to a file\.

```csharp
public System.Threading.Tasks.Task EncodeToFileAsync(ToonNet.Core.Models.ToonDocument document, string filePath, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeToFileAsync(ToonNet.Core.Models.ToonDocument,string,System.Threading.CancellationToken).document'></a>

`document` [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument')

The document to encode\.

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeToFileAsync(ToonNet.Core.Models.ToonDocument,string,System.Threading.CancellationToken).filePath'></a>

`filePath` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The file path to write to\.

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeToFileAsync(ToonNet.Core.Models.ToonDocument,string,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Token to monitor for cancellation requests\.

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A task that represents the asynchronous encoding and write operation\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when a document or filePath is null\.

[System\.IO\.IOException](https://learn.microsoft.com/en-us/dotnet/api/system.io.ioexception 'System\.IO\.IOException')  
Thrown when file I/O fails\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
Thrown when the operation is canceled\.