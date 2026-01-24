#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\(ToonOptions\) Constructor

Provides functionality to encode a [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument') into a TOON format string\.

```csharp
public ToonEncoder(ToonNet.Core.ToonOptions? options=null);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.ToonEncoder(ToonNet.Core.ToonOptions).options'></a>

`options` [ToonOptions](ToonNet.Core.ToonOptions.md 'ToonNet\.Core\.ToonOptions')

Optional encoding options to customize the behavior of the encoder\. If not provided, 
the default options \([Default](ToonNet.Core.ToonOptions.Default.md 'ToonNet\.Core\.ToonOptions\.Default')\) will be used\.

### Example

```csharp
var document = new ToonDocument(new ToonObject
{
    { "key", new ToonString("value") }
});
var encoder = new ToonEncoder();
var encodedString = encoder.Encode(document);
Console.WriteLine(encodedString);
```

### Remarks
This class is designed to be used for serializing [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument') instances into 
their string representation in the TOON format\. It supports various TOON value types such as 
objects, arrays, strings, numbers, booleans, and nulls\. The encoder ensures proper formatting 
and handles indentation, quoting, and escaping as per the TOON specification\.