#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.GetCurrentIndent\(\) Method

Gets the current indentation level\.

```csharp
private int GetCurrentIndent();
```

#### Returns
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')  
The number of spaces of indentation, or 0 if not at an indent token\.

### Remarks
Optimized to avoid repeated Peek\(\) calls by accessing tokens directly\.