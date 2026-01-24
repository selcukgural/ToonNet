#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EncodeObject\(ToonObject, int\) Method

Encodes an object to TOON format\.

```csharp
private void EncodeObject(ToonNet.Core.Models.ToonObject obj, int indentLevel);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeObject(ToonNet.Core.Models.ToonObject,int).obj'></a>

`obj` [ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject')

The object to encode\. This parameter must not be null\.

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeObject(ToonNet.Core.Models.ToonObject,int).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current indentation level, used to format the output string\.

### Remarks
This method iterates through the properties of the provided [ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject') and encodes
each key\-value pair\. Keys are quoted if they contain special characters\. Values are encoded
recursively based on their type\. The method ensures proper indentation and formatting for nested
structures\.