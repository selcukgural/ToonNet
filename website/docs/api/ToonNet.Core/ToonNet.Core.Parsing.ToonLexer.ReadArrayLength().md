#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonLexer](ToonNet.Core.Parsing.ToonLexer.md 'ToonNet\.Core\.Parsing\.ToonLexer')

## ToonLexer\.ReadArrayLength\(\) Method

Reads an array length token \(e\.g\., "\[3\]"\) from the input\.

```csharp
private ToonNet.Core.Models.ToonToken ReadArrayLength();
```

#### Returns
[ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')  
A token representing the array length\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when the array length is not properly terminated with a closing bracket\.