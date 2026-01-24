#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models')

## ToonDocument Class

Represents a complete TOON document\.

```csharp
public sealed class ToonDocument
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonDocument

### Remarks
This class encapsulates the root value of a TOON document, which can be treated as either
an object or an array depending on its type\. It provides methods to safely cast the root
to the expected type, throwing an exception if the type does not match\.

| Constructors | |
| :--- | :--- |
| [ToonDocument\(\)](ToonNet.Core.Models.ToonDocument..ctor.md#ToonNet.Core.Models.ToonDocument.ToonDocument() 'ToonNet\.Core\.Models\.ToonDocument\.ToonDocument\(\)') | Creates a new ToonDocument with an empty object as a root\. |
| [ToonDocument\(ToonValue\)](ToonNet.Core.Models.ToonDocument..ctor.md#ToonNet.Core.Models.ToonDocument.ToonDocument(ToonNet.Core.Models.ToonValue) 'ToonNet\.Core\.Models\.ToonDocument\.ToonDocument\(ToonNet\.Core\.Models\.ToonValue\)') | Represents a complete TOON document\. |

| Properties | |
| :--- | :--- |
| [Root](ToonNet.Core.Models.ToonDocument.Root.md 'ToonNet\.Core\.Models\.ToonDocument\.Root') | Gets the root value of the document\. |

| Methods | |
| :--- | :--- |
| [AsArray\(\)](ToonNet.Core.Models.ToonDocument.AsArray().md 'ToonNet\.Core\.Models\.ToonDocument\.AsArray\(\)') | Attempts to treat the document root as an array\. |
| [AsObject\(\)](ToonNet.Core.Models.ToonDocument.AsObject().md 'ToonNet\.Core\.Models\.ToonDocument\.AsObject\(\)') | Attempts to treat the document root as an object\. |
