#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonLexer](ToonNet.Core.Parsing.ToonLexer.md 'ToonNet\.Core\.Parsing\.ToonLexer')

## ToonLexer\.ReadArrayFields\(\) Method

Reads the fields of an array from the input and returns a token representing the content\.

```csharp
private ToonNet.Core.Models.ToonToken ReadArrayFields();
```

#### Returns
[ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')  
A [ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken') of type `ArrayFields` containing the array fields content
as well as the starting line and column of the token in the input\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when the array fields are not properly terminated in the input\.