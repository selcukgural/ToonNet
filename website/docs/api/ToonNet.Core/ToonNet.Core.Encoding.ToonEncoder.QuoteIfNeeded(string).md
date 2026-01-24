#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.QuoteIfNeeded\(string\) Method

Quotes a string value if it needs quoting\.

```csharp
private static string QuoteIfNeeded(string value);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.QuoteIfNeeded(string).value'></a>

`value` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The string vaLue to potentially quote\. Can be null or empty\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
The quoted string if quoting is necessary; otherwise, the original string\.