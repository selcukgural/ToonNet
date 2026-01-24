#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.Advance\(\) Method

Advances to the next token and returns the current token\.

```csharp
private ToonNet.Core.Models.ToonToken Advance();
```

#### Returns
[ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')  
The current token before advancing\.

### Remarks
Invalidates the token cache since position changes\.