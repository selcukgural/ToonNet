#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ExpectToken\(ToonTokenType\) Method

Checks if the current token is of the expected type\.

```csharp
private bool ExpectToken(ToonNet.Core.Models.ToonTokenType expectedType);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ExpectToken(ToonNet.Core.Models.ToonTokenType).expectedType'></a>

`expectedType` [ToonTokenType](ToonNet.Core.Models.ToonTokenType.md 'ToonNet\.Core\.Models\.ToonTokenType')

The expected token type\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if the current token matches the expected type; otherwise, false\.

### Remarks
Helper method to simplify repeated token type checks throughout the parser\.
Uses a cached Peek\(\) result for optimal performance\.