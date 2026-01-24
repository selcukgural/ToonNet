#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.Peek\(\) Method

Peeks at the current token without advancing\.

```csharp
private ToonNet.Core.Models.ToonToken Peek();
```

#### Returns
[ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')  
The current token, or a cached EndOfInput token if at end\.

### Remarks
Optimized with token cache to avoid repeated access at the same position\.
Uses AggressiveInlining for maximum performance\.