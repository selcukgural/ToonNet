#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models').[ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument')

## ToonDocument Constructors

| Overloads | |
| :--- | :--- |
| [ToonDocument\(\)](ToonNet.Core.Models.ToonDocument..ctor.md#ToonNet.Core.Models.ToonDocument.ToonDocument() 'ToonNet\.Core\.Models\.ToonDocument\.ToonDocument\(\)') | Creates a new ToonDocument with an empty object as a root\. |
| [ToonDocument\(ToonValue\)](ToonNet.Core.Models.ToonDocument..ctor.md#ToonNet.Core.Models.ToonDocument.ToonDocument(ToonNet.Core.Models.ToonValue) 'ToonNet\.Core\.Models\.ToonDocument\.ToonDocument\(ToonNet\.Core\.Models\.ToonValue\)') | Represents a complete TOON document\. |

<a name='ctor.md#ToonNet.Core.Models.ToonDocument.ToonDocument()'></a>

## ToonDocument\(\) Constructor

Creates a new ToonDocument with an empty object as a root\.

```csharp
public ToonDocument();
```

### Remarks
This constructor initializes the document with a default root value of type [ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject')\.

<a name='ctor.md#ToonNet.Core.Models.ToonDocument.ToonDocument(ToonNet.Core.Models.ToonValue)'></a>

## ToonDocument\(ToonValue\) Constructor

Represents a complete TOON document\.

```csharp
public ToonDocument(ToonNet.Core.Models.ToonValue root);
```
#### Parameters

<a name='ToonNet.Core.Models.ToonDocument.ToonDocument(ToonNet.Core.Models.ToonValue).root'></a>

`root` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

### Remarks
This class encapsulates the root value of a TOON document, which can be treated as either
an object or an array depending on its type\. It provides methods to safely cast the root
to the expected type, throwing an exception if the type does not match\.