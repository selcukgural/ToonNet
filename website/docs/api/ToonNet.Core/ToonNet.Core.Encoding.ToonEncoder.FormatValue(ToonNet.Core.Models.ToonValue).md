#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.FormatValue\(ToonValue\) Method

Formats a TOON value as a string\.

```csharp
private static string FormatValue(ToonNet.Core.Models.ToonValue? value);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.FormatValue(ToonNet.Core.Models.ToonValue).value'></a>

`value` [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')

The value to format\. This can be null or any type derived from [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
A string representation of the TOON value\. Returns "null" for null values or [ToonNull](ToonNet.Core.Models.ToonNull.md 'ToonNet\.Core\.Models\.ToonNull'),
"true"/"false" for [ToonBoolean](ToonNet.Core.Models.ToonBoolean.md 'ToonNet\.Core\.Models\.ToonBoolean'), a formatted number for [ToonNumber](ToonNet.Core.Models.ToonNumber.md 'ToonNet\.Core\.Models\.ToonNumber'),
a quoted string for [ToonString](ToonNet.Core.Models.ToonString.md 'ToonNet\.Core\.Models\.ToonString'), or the result of [System\.Object\.ToString](https://learn.microsoft.com/en-us/dotnet/api/system.object.tostring 'System\.Object\.ToString') for other types\.