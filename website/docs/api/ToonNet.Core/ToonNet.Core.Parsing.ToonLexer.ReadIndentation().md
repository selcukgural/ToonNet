#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonLexer](ToonNet.Core.Parsing.ToonLexer.md 'ToonNet\.Core\.Parsing\.ToonLexer')

## ToonLexer\.ReadIndentation\(\) Method

Reads an indentation \(sequence of spaces\) from the input string at the current position\.

```csharp
private ToonNet.Core.Models.ToonToken ReadIndentation();
```

#### Returns
[ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')  
A token representing the indentation, including the number of spaces and their position in the input\.