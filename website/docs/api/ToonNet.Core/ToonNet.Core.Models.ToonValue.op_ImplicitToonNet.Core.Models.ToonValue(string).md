#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models').[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

## ToonValue\.implicit operator ToonValue\(string\) Operator

Implicitly converts a string value to a ToonValue\.

```csharp
public static ToonNet.Core.Models.ToonValue? implicit operator ToonNet.Core.Models.ToonValue?(string? value);
```
#### Parameters

<a name='ToonNet.Core.Models.ToonValue.op_ImplicitToonNet.Core.Models.ToonValue(string).value'></a>

`value` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The string value to convert\. Null values are converted to ToonNull\.

#### Returns
[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')