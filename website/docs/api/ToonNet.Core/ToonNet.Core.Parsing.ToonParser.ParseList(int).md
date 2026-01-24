#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ParseList\(int\) Method

Parses a list \(items prefixed with '\-'\)\.

```csharp
private ToonNet.Core.Models.ToonArray ParseList(int indentLevel);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ParseList(int).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current indentation level\.

#### Returns
[ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')  
A ToonArray containing the list items\.