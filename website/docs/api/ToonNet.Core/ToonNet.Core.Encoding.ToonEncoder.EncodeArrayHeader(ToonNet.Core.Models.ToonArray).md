#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EncodeArrayHeader\(ToonArray\) Method

Encodes the array header with length and optional field names\.

```csharp
private void EncodeArrayHeader(ToonNet.Core.Models.ToonArray array);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeArrayHeader(ToonNet.Core.Models.ToonArray).array'></a>

`array` [ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')

The array to encode the header for\. This parameter must not be null\.

### Remarks
This method writes the array length in square brackets\. If the array is tabular and contains
d names, those field names are written in curly braces after the length\.