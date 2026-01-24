#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ParseValueToken\(ToonToken\) Method

Parses a value token \(either quoted string or primitive\)\.

```csharp
private static ToonNet.Core.Models.ToonValue ParseValueToken(ToonNet.Core.Models.ToonToken token);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ParseValueToken(ToonNet.Core.Models.ToonToken).token'></a>

`token` [ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')

The token to parse\.

#### Returns
[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')  
A ToonValue representing the token\.