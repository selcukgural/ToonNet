#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing')

## ToonLexer Class

Tokenizes TOON format input into a stream of tokens\.

```csharp
internal sealed class ToonLexer
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonLexer

### Remarks
This is an internal implementation detail\. Users should use [ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser') instead\.

| Constructors | |
| :--- | :--- |
| [ToonLexer\(string\)](ToonNet.Core.Parsing.ToonLexer..ctor.md#ToonNet.Core.Parsing.ToonLexer.ToonLexer(string) 'ToonNet\.Core\.Parsing\.ToonLexer\.ToonLexer\(string\)') | Creates a new lexer for the specified input string\. |
| [ToonLexer\(ReadOnlyMemory&lt;char&gt;\)](ToonNet.Core.Parsing.ToonLexer..ctor.md#ToonNet.Core.Parsing.ToonLexer.ToonLexer(System.ReadOnlyMemory_char_) 'ToonNet\.Core\.Parsing\.ToonLexer\.ToonLexer\(System\.ReadOnlyMemory\<char\>\)') | Creates a new lexer for processing TOON format input\. |

| Methods | |
| :--- | :--- |
| [Advance\(\)](ToonNet.Core.Parsing.ToonLexer.Advance().md 'ToonNet\.Core\.Parsing\.ToonLexer\.Advance\(\)') | Advances the current position within the input string by one character\. |
| [IsAtEnd\(\)](ToonNet.Core.Parsing.ToonLexer.IsAtEnd().md 'ToonNet\.Core\.Parsing\.ToonLexer\.IsAtEnd\(\)') | Checks if the lexer has reached the end of the input\. |
| [NextToken\(\)](ToonNet.Core.Parsing.ToonLexer.NextToken().md 'ToonNet\.Core\.Parsing\.ToonLexer\.NextToken\(\)') | Reads the next token from the input stream\. |
| [Peek\(\)](ToonNet.Core.Parsing.ToonLexer.Peek().md 'ToonNet\.Core\.Parsing\.ToonLexer\.Peek\(\)') | Peeks at the current character in the input stream without advancing the position\. |
| [PeekNext\(\)](ToonNet.Core.Parsing.ToonLexer.PeekNext().md 'ToonNet\.Core\.Parsing\.ToonLexer\.PeekNext\(\)') | Peeks at the character following the current position without advancing the lexer\. |
| [PreviousWasNewline\(\)](ToonNet.Core.Parsing.ToonLexer.PreviousWasNewline().md 'ToonNet\.Core\.Parsing\.ToonLexer\.PreviousWasNewline\(\)') | Checks if the previous character was a newline\. |
| [ReadArrayFields\(\)](ToonNet.Core.Parsing.ToonLexer.ReadArrayFields().md 'ToonNet\.Core\.Parsing\.ToonLexer\.ReadArrayFields\(\)') | Reads the fields of an array from the input and returns a token representing the content\. |
| [ReadArrayLength\(\)](ToonNet.Core.Parsing.ToonLexer.ReadArrayLength().md 'ToonNet\.Core\.Parsing\.ToonLexer\.ReadArrayLength\(\)') | Reads an array length token \(e\.g\., "\[3\]"\) from the input\. |
| [ReadIndentation\(\)](ToonNet.Core.Parsing.ToonLexer.ReadIndentation().md 'ToonNet\.Core\.Parsing\.ToonLexer\.ReadIndentation\(\)') | Reads an indentation \(sequence of spaces\) from the input string at the current position\. |
| [ReadKeyOrValue\(\)](ToonNet.Core.Parsing.ToonLexer.ReadKeyOrValue().md 'ToonNet\.Core\.Parsing\.ToonLexer\.ReadKeyOrValue\(\)') | Reads the next token from the input, identifying it as either a key or a value\. |
| [ReadQuotedStringToken\(\)](ToonNet.Core.Parsing.ToonLexer.ReadQuotedStringToken().md 'ToonNet\.Core\.Parsing\.ToonLexer\.ReadQuotedStringToken\(\)') | Reads a quoted string token and determines if it represents a key or value based on the subsequent content\. |
| [ReadQuotedStringValue\(\)](ToonNet.Core.Parsing.ToonLexer.ReadQuotedStringValue().md 'ToonNet\.Core\.Parsing\.ToonLexer\.ReadQuotedStringValue\(\)') | Reads the content of a quoted string from the input while handling escape sequences\. |
| [SkipWhitespace\(\)](ToonNet.Core.Parsing.ToonLexer.SkipWhitespace().md 'ToonNet\.Core\.Parsing\.ToonLexer\.SkipWhitespace\(\)') | Skips whitespace characters \(spaces only, not including newlines\) in the input\. |
| [Tokenize\(\)](ToonNet.Core.Parsing.ToonLexer.Tokenize().md 'ToonNet\.Core\.Parsing\.ToonLexer\.Tokenize\(\)') | Tokenizes the input into a list of TOON tokens\. |
