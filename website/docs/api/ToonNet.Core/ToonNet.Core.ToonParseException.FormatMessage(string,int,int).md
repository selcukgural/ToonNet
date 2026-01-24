#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core').[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')

## ToonParseException\.FormatMessage\(string, int, int\) Method

Formats the error message with line and column information\.

```csharp
private static string FormatMessage(string message, int line, int column);
```
#### Parameters

<a name='ToonNet.Core.ToonParseException.FormatMessage(string,int,int).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The error message\.

<a name='ToonNet.Core.ToonParseException.FormatMessage(string,int,int).line'></a>

`line` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The line number\.

<a name='ToonNet.Core.ToonParseException.FormatMessage(string,int,int).column'></a>

`column` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The column number\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
A formatted error message\.