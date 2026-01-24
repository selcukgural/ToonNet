#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EncodeObjectInline\(ToonObject, int\) Method

Encodes an object inline \(first property on the same line, rest indented\)\.

```csharp
private void EncodeObjectInline(ToonNet.Core.Models.ToonObject obj, int indentLevel);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeObjectInline(ToonNet.Core.Models.ToonObject,int).obj'></a>

`obj` [ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject')

The object to encode\. This parameter must not be null\.

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeObjectInline(ToonNet.Core.Models.ToonObject,int).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The base indentation level for subsequent properties\.

### Remarks
This method is used when encoding objects as array items\. The first property
appears on the same line as the array marker \('\-'\), while subsequent properties
are indented at the appropriate level\.