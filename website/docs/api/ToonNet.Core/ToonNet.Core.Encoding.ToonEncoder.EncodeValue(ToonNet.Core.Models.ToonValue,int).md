#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EncodeValue\(ToonValue, int\) Method

Encodes a [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue') into the internal string builder\.

```csharp
private void EncodeValue(ToonNet.Core.Models.ToonValue value, int indentLevel);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeValue(ToonNet.Core.Models.ToonValue,int).value'></a>

`value` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

The value to encode\. This parameter must not be null\.

<a name='ToonNet.Core.Encoding.ToonEncoder.EncodeValue(ToonNet.Core.Models.ToonValue,int).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current indentation level, used to format the output string\.

#### Exceptions

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when encoding exceeds the maximum depth specified in the options\.

### Remarks
This method uses a switch statement to determine the type of the provided value
and delegates the encoding to the appropriate helper method\. Supported value types
include null, boolean, number, string, object, and array\. The depth counter is
incremented and decremented to ensure proper tracking of nested structures\.