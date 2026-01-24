#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonLexer](ToonNet.Core.Parsing.ToonLexer.md 'ToonNet\.Core\.Parsing\.ToonLexer')

## ToonLexer\.ReadQuotedStringValue\(\) Method

Reads the content of a quoted string from the input while handling escape sequences\.

```csharp
private string ReadQuotedStringValue();
```

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
The unescaped content of the quoted string\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when a quoted string is not properly terminated in the input\.