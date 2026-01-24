#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.GetCurrentIndentAndAdvance\(\) Method

Gets the current token's indent level and advances the parser position\.

```csharp
private int GetCurrentIndentAndAdvance();
```

#### Returns
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')  
The indent level of the current token before advancing\.

### Remarks
Helper method that combines a common pattern of reading indent level and consuming the token\.
Optimized for frequent indent processing in nested structures\.