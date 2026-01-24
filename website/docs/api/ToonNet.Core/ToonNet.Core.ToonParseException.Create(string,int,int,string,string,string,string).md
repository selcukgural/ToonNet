#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core').[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')

## ToonParseException\.Create\(string, int, int, string, string, string, string\) Method

Creates a parse exception with detailed context\.

```csharp
public static ToonNet.Core.ToonParseException Create(string message, int line, int column, string? actual=null, string? expected=null, string? suggestion=null, string? codeSnippet=null);
```
#### Parameters

<a name='ToonNet.Core.ToonParseException.Create(string,int,int,string,string,string,string).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The error message\.

<a name='ToonNet.Core.ToonParseException.Create(string,int,int,string,string,string,string).line'></a>

`line` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The line number where the error occurred\.

<a name='ToonNet.Core.ToonParseException.Create(string,int,int,string,string,string,string).column'></a>

`column` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The column number where the error occurred\.

<a name='ToonNet.Core.ToonParseException.Create(string,int,int,string,string,string,string).actual'></a>

`actual` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Optional actual token that was encountered\.

<a name='ToonNet.Core.ToonParseException.Create(string,int,int,string,string,string,string).expected'></a>

`expected` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Optional expected token type\.

<a name='ToonNet.Core.ToonParseException.Create(string,int,int,string,string,string,string).suggestion'></a>

`suggestion` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Optional suggestion for fixing the error\.

<a name='ToonNet.Core.ToonParseException.Create(string,int,int,string,string,string,string).codeSnippet'></a>

`codeSnippet` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Optional code snippet showing the problematic area\.

#### Returns
[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
A new ToonParseException instance\.