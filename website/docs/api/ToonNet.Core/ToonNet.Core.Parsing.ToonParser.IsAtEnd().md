#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.IsAtEnd\(\) Method

Checks if the parser has reached the end of tokens\.

```csharp
private bool IsAtEnd();
```

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if at the end of tokens; otherwise, false\.

### Remarks
Optimized to check position and EndOfInput token type\.