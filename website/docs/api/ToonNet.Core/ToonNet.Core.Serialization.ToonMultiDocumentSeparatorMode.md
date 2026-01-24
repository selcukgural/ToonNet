#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization')

## ToonMultiDocumentSeparatorMode Enum

Specifies how multiple TOON documents are delimited when deserializing from a stream\.

```csharp
public enum ToonMultiDocumentSeparatorMode
```
### Fields

<a name='ToonNet.Core.Serialization.ToonMultiDocumentSeparatorMode.BlankLine'></a>

`BlankLine` 0

Documents are separated by blank lines\.

<a name='ToonNet.Core.Serialization.ToonMultiDocumentSeparatorMode.ExplicitSeparator'></a>

`ExplicitSeparator` 1

Documents are separated by an explicit separator line \(for example: `---`\)\.