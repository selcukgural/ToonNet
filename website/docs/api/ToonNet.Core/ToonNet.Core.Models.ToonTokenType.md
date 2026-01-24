#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models')

## ToonTokenType Enum

Represents the type of token in TOON format\.

```csharp
internal enum ToonTokenType
```
### Fields

<a name='ToonNet.Core.Models.ToonTokenType.Key'></a>

`Key` 0

A field name or key \(e\.g\., "name" in "name: value"\)\.

<a name='ToonNet.Core.Models.ToonTokenType.Colon'></a>

`Colon` 1

A colon separator \(:\)\.

<a name='ToonNet.Core.Models.ToonTokenType.Value'></a>

`Value` 2

A simple value \(string, number, boolean, null\)\.

<a name='ToonNet.Core.Models.ToonTokenType.QuotedString'></a>

`QuotedString` 3

A quoted string value\.

<a name='ToonNet.Core.Models.ToonTokenType.ArrayLength'></a>

`ArrayLength` 4

Array length indicator \(e\.g\., "\[3\]" in "tags\[3\]:"\)\.

<a name='ToonNet.Core.Models.ToonTokenType.ArrayFields'></a>

`ArrayFields` 5

Array field definition \(e\.g\., "\{id,name,role\}" in "users\[2\]\{id,name,role\}:"\)\.

<a name='ToonNet.Core.Models.ToonTokenType.Comma'></a>

`Comma` 6

Comma separator in arrays or field definitions\.

<a name='ToonNet.Core.Models.ToonTokenType.Indent'></a>

`Indent` 7

Indentation \(spaces\) indicating nesting level\.

<a name='ToonNet.Core.Models.ToonTokenType.Newline'></a>

`Newline` 8

Newline character\.

<a name='ToonNet.Core.Models.ToonTokenType.ListItem'></a>

`ListItem` 9

List item indicator \(hyphen followed by space\)\.

<a name='ToonNet.Core.Models.ToonTokenType.EndOfInput'></a>

`EndOfInput` 10

End of input\.

### Remarks
This is an internal implementation detail used by the lexer and parser\.
Users should interact with parsed TOON documents through [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument') and [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')\.