#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EncodeTabularArray\(ToonArray, int\) Method

Encodes a tabular array \(array of objects with field names\) into the TOON format\.

```csharp
private void EncodeTabularArray(ToonNet.Core.Models.ToonArray array, int indentLevel);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeTabularArray(ToonNet.Core.Models.ToonArray,int).array'></a>

`array` [ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')

The tabular array to encode\. This parameter must not be null and should contain
objects with consistent field names\.

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeTabularArray(ToonNet.Core.Models.ToonArray,int).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current indentation level, used to format the output string\.

### Remarks
This method iterates through the items in the array and encodes each row as a
comma\-separated list of field values\. If the array contains objects with field names,
the values are extracted and formatted accordingly\. Non\-object items are formatted
directly\. Proper indentation is applied for each row\.