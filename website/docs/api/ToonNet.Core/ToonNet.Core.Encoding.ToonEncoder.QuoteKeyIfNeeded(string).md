#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.QuoteKeyIfNeeded\(string\) Method

Quotes a key if it needs quoting\.

```csharp
private static string QuoteKeyIfNeeded(string key);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.QuoteKeyIfNeeded(string).key'></a>

`key` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The key to potentially quote\. Can be null or empty\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
The quoted key if quoting is necessary; otherwise, the original key\.