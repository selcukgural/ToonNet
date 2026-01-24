#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ParsePrimitiveValue\(ReadOnlyMemory\<char\>\) Method

Parses a primitive value from a token's memory\.

```csharp
private static ToonNet.Core.Models.ToonValue ParsePrimitiveValue(System.ReadOnlyMemory<char> valueMemory);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ParsePrimitiveValue(System.ReadOnlyMemory_char_).valueMemory'></a>

`valueMemory` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Char](https://learn.microsoft.com/en-us/dotnet/api/system.char 'System\.Char')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

The memory containing the value\.

#### Returns
[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')  
A ToonValue representing the primitive \(null, boolean, number, or string\)\.