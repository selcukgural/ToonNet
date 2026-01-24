#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ParseObject\(int\) Method

Parses an object from the token stream\.

```csharp
private ToonNet.Core.Models.ToonObject ParseObject(int indentLevel);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ParseObject(int).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current indentation level\.

#### Returns
[ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject')  
A ToonObject containing the parsed key\-value pairs\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when the object structure is invalid\.