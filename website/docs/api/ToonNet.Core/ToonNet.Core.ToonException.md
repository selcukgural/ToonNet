#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core')

## ToonException Class

Base exception for TOON\-related errors\.

```csharp
public class ToonException : System.Exception
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception') &#129106; ToonException

Derived  
&#8627; [ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
&#8627; [ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
&#8627; [ToonSerializationException](ToonNet.Core.ToonSerializationException.md 'ToonNet\.Core\.ToonSerializationException')

| Constructors | |
| :--- | :--- |
| [ToonException\(string\)](ToonNet.Core.ToonException..ctor.md#ToonNet.Core.ToonException.ToonException(string) 'ToonNet\.Core\.ToonException\.ToonException\(string\)') | Initializes a new instance of the ToonException class with a specified error message\. |
| [ToonException\(string, Exception\)](ToonNet.Core.ToonException..ctor.md#ToonNet.Core.ToonException.ToonException(string,System.Exception) 'ToonNet\.Core\.ToonException\.ToonException\(string, System\.Exception\)') | Initializes a new instance of the ToonException class with a specified error message and a reference to the inner exception\. |

| Properties | |
| :--- | :--- |
| [CodeSnippet](ToonNet.Core.ToonException.CodeSnippet.md 'ToonNet\.Core\.ToonException\.CodeSnippet') | Optional code snippet showing the problematic area\. |
| [Suggestion](ToonNet.Core.ToonException.Suggestion.md 'ToonNet\.Core\.ToonException\.Suggestion') | Optional suggestion for fixing the error\. |

| Methods | |
| :--- | :--- |
| [ToString\(\)](ToonNet.Core.ToonException.ToString().md 'ToonNet\.Core\.ToonException\.ToString\(\)') | Returns a string representation of the exception, including suggestions and code snippets\. |
