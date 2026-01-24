#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ParseTabularArrayInlineRow\(string\[\]\) Method

Parses an inline tabular array row \(comma\-separated values\)\.

```csharp
private ToonNet.Core.Models.ToonArray ParseTabularArrayInlineRow(string[]? fieldNames);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ParseTabularArrayInlineRow(string[]).fieldNames'></a>

`fieldNames` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

The field names for tabular data\.

#### Returns
[ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')  
A ToonArray with the parsed row values\.