#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models')

## ToonToken Struct

Represents a token in TOON format with its type, value, and position\.

```csharp
internal readonly struct ToonToken
```

### Remarks
This is an internal implementation detail used by the lexer and parser\.
Users should use [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument') and [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue') instead\.

| Constructors | |
| :--- | :--- |
| [ToonToken\(ToonTokenType, ReadOnlyMemory&lt;char&gt;, int, int\)](ToonNet.Core.Models.ToonToken.ToonToken(ToonNet.Core.Models.ToonTokenType,System.ReadOnlyMemory_char_,int,int).md 'ToonNet\.Core\.Models\.ToonToken\.ToonToken\(ToonNet\.Core\.Models\.ToonTokenType, System\.ReadOnlyMemory\<char\>, int, int\)') | Represents a token in TOON format with its type, value, and position\. |

| Properties | |
| :--- | :--- |
| [Column](ToonNet.Core.Models.ToonToken.Column.md 'ToonNet\.Core\.Models\.ToonToken\.Column') | Gets the column number where this token appears\. |
| [Line](ToonNet.Core.Models.ToonToken.Line.md 'ToonNet\.Core\.Models\.ToonToken\.Line') | Gets the line number where this token appears\. |
| [Type](ToonNet.Core.Models.ToonToken.Type.md 'ToonNet\.Core\.Models\.ToonToken\.Type') | Gets the type of this token\. |
| [Value](ToonNet.Core.Models.ToonToken.Value.md 'ToonNet\.Core\.Models\.ToonToken\.Value') | Gets the value of this token\. |

| Methods | |
| :--- | :--- |
| [ToString\(\)](ToonNet.Core.Models.ToonToken.ToString().md 'ToonNet\.Core\.Models\.ToonToken\.ToString\(\)') | Returns a string representation of this token\. |
