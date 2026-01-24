#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core')

## ToonEncodingException Class

Exception thrown during TOON encoding\.

```csharp
public sealed class ToonEncodingException : ToonNet.Core.ToonException
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception') &#129106; [ToonException](ToonNet.Core.ToonException.md 'ToonNet\.Core\.ToonException') &#129106; ToonEncodingException

| Constructors | |
| :--- | :--- |
| [ToonEncodingException\(string\)](ToonNet.Core.ToonEncodingException..ctor.md#ToonNet.Core.ToonEncodingException.ToonEncodingException(string) 'ToonNet\.Core\.ToonEncodingException\.ToonEncodingException\(string\)') | Initializes a new instance of the ToonEncodingException class\. |
| [ToonEncodingException\(string, Exception\)](ToonNet.Core.ToonEncodingException..ctor.md#ToonNet.Core.ToonEncodingException.ToonEncodingException(string,System.Exception) 'ToonNet\.Core\.ToonEncodingException\.ToonEncodingException\(string, System\.Exception\)') | Initializes a new instance of the ToonEncodingException class with an inner exception\. |

| Properties | |
| :--- | :--- |
| [ProblematicValue](ToonNet.Core.ToonEncodingException.ProblematicValue.md 'ToonNet\.Core\.ToonEncodingException\.ProblematicValue') | Gets the value that caused the encoding error\. |
| [PropertyPath](ToonNet.Core.ToonEncodingException.PropertyPath.md 'ToonNet\.Core\.ToonEncodingException\.PropertyPath') | Gets the path to the problematic property\. |

| Methods | |
| :--- | :--- |
| [Create\(string, string, object, string\)](ToonNet.Core.ToonEncodingException.Create(string,string,object,string).md 'ToonNet\.Core\.ToonEncodingException\.Create\(string, string, object, string\)') | Creates an encoding exception with context\. |
