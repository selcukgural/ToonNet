#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonLexer](ToonNet.Core.Parsing.ToonLexer.md 'ToonNet\.Core\.Parsing\.ToonLexer')

## ToonLexer\.ReadQuotedStringToken\(\) Method

Reads a quoted string token and determines if it represents a key or value based on the subsequent content\.

```csharp
private ToonNet.Core.Models.ToonToken ReadQuotedStringToken();
```

#### Returns
[ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')  
A token of type Key or QuotedString, depending on the context of the parsed content\.