#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EncodeArray\(ToonArray, int\) Method

Encodes an array to TOON format\.

```csharp
private void EncodeArray(ToonNet.Core.Models.ToonArray array, int indentLevel);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeArray(ToonNet.Core.Models.ToonArray,int).array'></a>

`array` [ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')

The array to encode\. This parameter must not be null\.

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeArray(ToonNet.Core.Models.ToonArray,int).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current indentation level, used to format the output string\.

### Remarks
This method determines the type of the array and encodes it accordingly:
\- Tabular arrays are encoded with field names and rows\.
\- Primitive arrays are encoded inline as a comma\-separated list\.
\- Mixed arrays are encoded as a list with each item on a new line\.
Empty arrays are skipped\.