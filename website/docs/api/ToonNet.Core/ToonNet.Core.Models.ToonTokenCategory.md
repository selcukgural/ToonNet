#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models')

## ToonTokenCategory Enum

Bitmask categories for fast token type checking\.

```csharp
internal enum ToonTokenCategory
```
### Fields

<a name='ToonNet.Core.Models.ToonTokenCategory.ValueStart'></a>

`ValueStart` 1

Tokens that can start a value: Key, Value, QuotedString, ListItem

<a name='ToonNet.Core.Models.ToonTokenCategory.ActualValue'></a>

`ActualValue` 2

Tokens that represent actual values: Value, QuotedString

<a name='ToonNet.Core.Models.ToonTokenCategory.Structural'></a>

`Structural` 4

Structural tokens: Colon, Comma, Newline, Indent

<a name='ToonNet.Core.Models.ToonTokenCategory.ArrayRelated'></a>

`ArrayRelated` 8

Array\-related tokens: ArrayLength, ArrayFields

<a name='ToonNet.Core.Models.ToonTokenCategory.Terminating'></a>

`Terminating` 16

Terminating tokens: Newline, EndOfInput

<a name='ToonNet.Core.Models.ToonTokenCategory.Whitespace'></a>

`Whitespace` 32

Whitespace tokens: Indent, Newline

### Remarks
Using bitmasks allows for O\(1\) category checks with better branch prediction\.