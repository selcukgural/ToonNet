#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonLexer](ToonNet.Core.Parsing.ToonLexer.md 'ToonNet\.Core\.Parsing\.ToonLexer')

## ToonLexer\.ReadKeyOrValue\(\) Method

Reads the next token from the input, identifying it as either a key or a value\.

```csharp
private ToonNet.Core.Models.ToonToken ReadKeyOrValue();
```

#### Returns
[ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')  
A [ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken') representing either a key or a value, based on the parsed input\.