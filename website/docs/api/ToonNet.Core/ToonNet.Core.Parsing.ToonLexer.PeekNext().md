#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonLexer](ToonNet.Core.Parsing.ToonLexer.md 'ToonNet\.Core\.Parsing\.ToonLexer')

## ToonLexer\.PeekNext\(\) Method

Peeks at the character following the current position without advancing the lexer\.

```csharp
private char PeekNext();
```

#### Returns
[System\.Char](https://learn.microsoft.com/en-us/dotnet/api/system.char 'System\.Char')  
The character after the current position, or '\\0' if beyond the end of the input\.