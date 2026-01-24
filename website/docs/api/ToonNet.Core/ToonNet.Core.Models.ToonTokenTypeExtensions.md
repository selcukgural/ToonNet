#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models')

## ToonTokenTypeExtensions Class

Extension methods for fast token type checking using bitmasks\.

```csharp
internal static class ToonTokenTypeExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonTokenTypeExtensions

| Methods | |
| :--- | :--- |
| [CanStartValue\(this ToonTokenType\)](ToonNet.Core.Models.ToonTokenTypeExtensions.CanStartValue(thisToonNet.Core.Models.ToonTokenType).md 'ToonNet\.Core\.Models\.ToonTokenTypeExtensions\.CanStartValue\(this ToonNet\.Core\.Models\.ToonTokenType\)') | Checks if a token can start a value \(Key, Value, QuotedString, ListItem\)\. |
| [Is\(this ToonTokenType, ToonTokenCategory\)](ToonNet.Core.Models.ToonTokenTypeExtensions.Is(thisToonNet.Core.Models.ToonTokenType,ToonNet.Core.Models.ToonTokenCategory).md 'ToonNet\.Core\.Models\.ToonTokenTypeExtensions\.Is\(this ToonNet\.Core\.Models\.ToonTokenType, ToonNet\.Core\.Models\.ToonTokenCategory\)') | Checks if a token type belongs to the specified category using bitmask operations\. |
| [IsActualValue\(this ToonTokenType\)](ToonNet.Core.Models.ToonTokenTypeExtensions.IsActualValue(thisToonNet.Core.Models.ToonTokenType).md 'ToonNet\.Core\.Models\.ToonTokenTypeExtensions\.IsActualValue\(this ToonNet\.Core\.Models\.ToonTokenType\)') | Checks if a token is an actual value \(Value, QuotedString\)\. |
| [IsArrayRelated\(this ToonTokenType\)](ToonNet.Core.Models.ToonTokenTypeExtensions.IsArrayRelated(thisToonNet.Core.Models.ToonTokenType).md 'ToonNet\.Core\.Models\.ToonTokenTypeExtensions\.IsArrayRelated\(this ToonNet\.Core\.Models\.ToonTokenType\)') | Checks if a token is array\-related \(ArrayLength, ArrayFields\)\. |
| [IsStructural\(this ToonTokenType\)](ToonNet.Core.Models.ToonTokenTypeExtensions.IsStructural(thisToonNet.Core.Models.ToonTokenType).md 'ToonNet\.Core\.Models\.ToonTokenTypeExtensions\.IsStructural\(this ToonNet\.Core\.Models\.ToonTokenType\)') | Checks if a token is structural \(Colon, Comma, Newline, Indent\)\. |
| [IsTerminating\(this ToonTokenType\)](ToonNet.Core.Models.ToonTokenTypeExtensions.IsTerminating(thisToonNet.Core.Models.ToonTokenType).md 'ToonNet\.Core\.Models\.ToonTokenTypeExtensions\.IsTerminating\(this ToonNet\.Core\.Models\.ToonTokenType\)') | Checks if a token is terminating \(Newline, EndOfInput\)\. |
