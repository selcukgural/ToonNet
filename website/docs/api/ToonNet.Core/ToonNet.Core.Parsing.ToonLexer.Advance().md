#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonLexer](ToonNet.Core.Parsing.ToonLexer.md 'ToonNet\.Core\.Parsing\.ToonLexer')

## ToonLexer\.Advance\(\) Method

Advances the current position within the input string by one character\.

```csharp
private void Advance();
```

### Remarks
Updates the internal position and column counters\.
No operation is performed if the end of the input has been reached\.