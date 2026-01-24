#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EncodeListArray\(ToonArray, int\) Method

Encodes an array as a list with each item prefixed by a '\-' character\.

```csharp
private void EncodeListArray(ToonNet.Core.Models.ToonArray array, int indentLevel);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeListArray(ToonNet.Core.Models.ToonArray,int).array'></a>

`array` [ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')

The array to encode\. This parameter must not be null\.

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeListArray(ToonNet.Core.Models.ToonArray,int).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current indentation level, used to format the output string\.

### Remarks
This method encodes each item in the array as a separate line\. If an item is
an object or another array, it is encoded recursively with increased indentation\.
Primitive items are encoded inline\. Proper indentation is applied for each item\.