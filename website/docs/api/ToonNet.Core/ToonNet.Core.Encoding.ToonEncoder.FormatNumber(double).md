#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.FormatNumber\(double\) Method

Formats a numeric value as a TOON\-compatible string\.

```csharp
private static string FormatNumber(double value);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.FormatNumber(double).value'></a>

`value` [System\.Double](https://learn.microsoft.com/en-us/dotnet/api/system.double 'System\.Double')

The number to format\. Must not be NaN or Infinity\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
A string representation of the number, formatted according to the TOON specification\.
Scientific notation is used for very large or very small numbers\.

#### Exceptions

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when the value is NaN or Infinity, as these are not allowed in the TOON format\.