#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ParseValue\(int\) Method

Parses a value at the specified indentation level\.

```csharp
private ToonNet.Core.Models.ToonValue ParseValue(int indentLevel);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ParseValue(int).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current indentation level\.

#### Returns
[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')  
The parsed ToonValue\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when an unexpected token is encountered\.