#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.EscapeString\(string\) Method

Escapes special characters in a string to ensure compatibility with the TOON format\.

```csharp
private static string EscapeString(string value);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.EscapeString(string).value'></a>

`value` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The string to escape\. This parameter must not be null\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
The escaped string, where special characters such as backslashes, double quotes, newlines,
carriage returns, and tabs are replaced with their escaped equivalents\.

### Remarks
This method replaces the following characters with their escaped representations:
\- Backslash \('\\\\'\) becomes '\\\\\\\\'
\- Double quote \('"'\) becomes '\\\\\\"'
\- Newline \('\\n'\) becomes '\\\\n'
\- Carriage return \('\\r'\) becomes '\\\\r'
\- Tab \('\\t'\) becomes '\\\\t'