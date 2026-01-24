#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonLexer](ToonNet.Core.Parsing.ToonLexer.md 'ToonNet\.Core\.Parsing\.ToonLexer')

## ToonLexer Constructors

| Overloads | |
| :--- | :--- |
| [ToonLexer\(string\)](ToonNet.Core.Parsing.ToonLexer..ctor.md#ToonNet.Core.Parsing.ToonLexer.ToonLexer(string) 'ToonNet\.Core\.Parsing\.ToonLexer\.ToonLexer\(string\)') | Creates a new lexer for the specified input string\. |
| [ToonLexer\(ReadOnlyMemory&lt;char&gt;\)](ToonNet.Core.Parsing.ToonLexer..ctor.md#ToonNet.Core.Parsing.ToonLexer.ToonLexer(System.ReadOnlyMemory_char_) 'ToonNet\.Core\.Parsing\.ToonLexer\.ToonLexer\(System\.ReadOnlyMemory\<char\>\)') | Creates a new lexer for processing TOON format input\. |

<a name='ctor.md#ToonNet.Core.Parsing.ToonLexer.ToonLexer(string)'></a>

## ToonLexer\(string\) Constructor

Creates a new lexer for the specified input string\.

```csharp
public ToonLexer(string input);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonLexer.ToonLexer(string).input'></a>

`input` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The TOON format string to tokenize\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when input is null\.

<a name='ctor.md#ToonNet.Core.Parsing.ToonLexer.ToonLexer(System.ReadOnlyMemory_char_)'></a>

## ToonLexer\(ReadOnlyMemory\<char\>\) Constructor

Creates a new lexer for processing TOON format input\.

```csharp
public ToonLexer(System.ReadOnlyMemory<char> input);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonLexer.ToonLexer(System.ReadOnlyMemory_char_).input'></a>

`input` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Char](https://learn.microsoft.com/en-us/dotnet/api/system.char 'System\.Char')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

### Remarks
Represents a low\-level implementation for tokenizing TOON format data\.
For higher\-level parsing, refer to [ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')\.