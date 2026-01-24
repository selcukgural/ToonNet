#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core')

## ToonParseException Class

Exception thrown during TOON parsing\.

```csharp
public sealed class ToonParseException : ToonNet.Core.ToonException
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception') &#129106; [ToonException](ToonNet.Core.ToonException.md 'ToonNet\.Core\.ToonException') &#129106; ToonParseException

| Constructors | |
| :--- | :--- |
| [ToonParseException\(string, int, int\)](ToonNet.Core.ToonParseException.ToonParseException(string,int,int).md 'ToonNet\.Core\.ToonParseException\.ToonParseException\(string, int, int\)') | Initializes a new instance of the ToonParseException class\. |

| Properties | |
| :--- | :--- |
| [ActualToken](ToonNet.Core.ToonParseException.ActualToken.md 'ToonNet\.Core\.ToonParseException\.ActualToken') | Gets the actual token that was encountered\. |
| [Column](ToonNet.Core.ToonParseException.Column.md 'ToonNet\.Core\.ToonParseException\.Column') | Gets the column number where the error occurred\. |
| [ExpectedToken](ToonNet.Core.ToonParseException.ExpectedToken.md 'ToonNet\.Core\.ToonParseException\.ExpectedToken') | Gets the expected token type\. |
| [Line](ToonNet.Core.ToonParseException.Line.md 'ToonNet\.Core\.ToonParseException\.Line') | Gets the line number where the error occurred\. |

| Methods | |
| :--- | :--- |
| [Create\(string, int, int, string, string, string, string\)](ToonNet.Core.ToonParseException.Create(string,int,int,string,string,string,string).md 'ToonNet\.Core\.ToonParseException\.Create\(string, int, int, string, string, string, string\)') | Creates a parse exception with detailed context\. |
| [FormatMessage\(string, int, int\)](ToonNet.Core.ToonParseException.FormatMessage(string,int,int).md 'ToonNet\.Core\.ToonParseException\.FormatMessage\(string, int, int\)') | Formats the error message with line and column information\. |
