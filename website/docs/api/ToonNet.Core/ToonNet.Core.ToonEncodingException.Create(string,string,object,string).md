#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core').[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')

## ToonEncodingException\.Create\(string, string, object, string\) Method

Creates an encoding exception with context\.

```csharp
public static ToonNet.Core.ToonEncodingException Create(string message, string? propertyPath=null, object? problematicValue=null, string? suggestion=null);
```
#### Parameters

<a name='ToonNet.Core.ToonEncodingException.Create(string,string,object,string).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The error message\.

<a name='ToonNet.Core.ToonEncodingException.Create(string,string,object,string).propertyPath'></a>

`propertyPath` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The optional path to the problematic property\.

<a name='ToonNet.Core.ToonEncodingException.Create(string,string,object,string).problematicValue'></a>

`problematicValue` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The value that caused the encoding error\.

<a name='ToonNet.Core.ToonEncodingException.Create(string,string,object,string).suggestion'></a>

`suggestion` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Optional suggestion for fixing the error\.

#### Returns
[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
A new ToonEncodingException instance\.