#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.Encode\(ToonDocument\) Method

Encodes a TOON document into its string representation\.

```csharp
public string Encode(ToonNet.Core.Models.ToonDocument document);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.Encode(ToonNet.Core.Models.ToonDocument).document'></a>

`document` [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument')

The document to encode\. This parameter must not be null\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
The encoded TOON format string\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when a document is null or document\.Root is null\.

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when encoding exceeds the maximum depth specified in the options\.

### Remarks
This method gets a StringBuilder from the pool, encodes the document,
and returns the StringBuilder to the pool after use\.