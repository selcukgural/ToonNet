#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ParseInlinePrimitiveArray\(int\) Method

Parses an inline array of primitive values\.

```csharp
private ToonNet.Core.Models.ToonArray ParseInlinePrimitiveArray(int expectedLength);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ParseInlinePrimitiveArray(int).expectedLength'></a>

`expectedLength` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The expected number of elements\.

#### Returns
[ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')  
A ToonArray with the parsed primitive values\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when array length mismatch occurs in strict mode\.