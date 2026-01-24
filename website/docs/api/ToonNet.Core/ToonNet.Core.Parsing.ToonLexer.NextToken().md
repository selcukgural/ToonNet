#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonLexer](ToonNet.Core.Parsing.ToonLexer.md 'ToonNet\.Core\.Parsing\.ToonLexer')

## ToonLexer\.NextToken\(\) Method

Reads the next token from the input stream\.

```csharp
private ToonNet.Core.Models.ToonToken NextToken();
```

#### Returns
[ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')  
An instance of [ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken') representing the next token in the stream\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when an unexpected character or invalid sequence is encountered during parsing\.