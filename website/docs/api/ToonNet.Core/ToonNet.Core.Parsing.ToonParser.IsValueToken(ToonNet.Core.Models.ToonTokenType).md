#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.IsValueToken\(ToonTokenType\) Method

Checks if a token type represents a value \(either Value or QuotedString\)\.

```csharp
private static bool IsValueToken(ToonNet.Core.Models.ToonTokenType type);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.IsValueToken(ToonNet.Core.Models.ToonTokenType).type'></a>

`type` [ToonTokenType](ToonNet.Core.Models.ToonTokenType.md 'ToonNet\.Core\.Models\.ToonTokenType')

The token type to check\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if the token is a value token; otherwise, false\.