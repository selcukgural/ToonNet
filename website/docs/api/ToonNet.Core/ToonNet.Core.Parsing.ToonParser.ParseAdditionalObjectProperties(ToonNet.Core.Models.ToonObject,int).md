#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ParseAdditionalObjectProperties\(ToonObject, int\) Method

Parses additional object properties at a higher indentation level\.
Used for list items with properties across multiple lines\.

```csharp
private void ParseAdditionalObjectProperties(ToonNet.Core.Models.ToonObject targetObject, int listIndentLevel);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ParseAdditionalObjectProperties(ToonNet.Core.Models.ToonObject,int).targetObject'></a>

`targetObject` [ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject')

The object to add properties to\.

<a name='ToonNet.Core.Parsing.ToonParser.ParseAdditionalObjectProperties(ToonNet.Core.Models.ToonObject,int).listIndentLevel'></a>

`listIndentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The indentation level of the parent list\.