#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EncodePrimitiveArrayInline\(ToonArray\) Method

Encodes a primitive array as an inline comma\-separated list\.

```csharp
private void EncodePrimitiveArrayInline(ToonNet.Core.Models.ToonArray array);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodePrimitiveArrayInline(ToonNet.Core.Models.ToonArray).array'></a>

`array` [ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')

The array to encode\. This parameter must not be null and should contain only
primitive values \(e\.g\., null, boolean, number, or string\)\.

### Remarks
This method formats the array as a single line of comma\-separated values\.
It is optimized for arrays containing only primitive types\.