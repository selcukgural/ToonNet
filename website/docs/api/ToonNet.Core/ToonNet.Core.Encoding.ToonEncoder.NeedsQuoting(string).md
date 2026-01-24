#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.NeedsQuoting\(string\) Method

Checks if a string needs to be quoted based on the TOON format specification\.

```csharp
private static bool NeedsQuoting(string value);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.NeedsQuoting(string).value'></a>

`value` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The string to check\. This parameter must not be null\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
`true` if the string needs quoting; otherwise, `false`\.

### Remarks
A string requires quoting if it is empty, contains leading or trailing whitespace,
includes spaces, matches reserved keywords \("true", "false", "null"\), resembles a number,
or contains special characters such as ':', ',', '\[', '\]', '\{', '\}', newline, carriage return,
double quotes, or backslashes\.