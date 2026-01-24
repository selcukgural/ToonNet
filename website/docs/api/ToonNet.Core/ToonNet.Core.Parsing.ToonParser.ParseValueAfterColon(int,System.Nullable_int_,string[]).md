#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ParseValueAfterColon\(int, Nullable\<int\>, string\[\]\) Method

Parses the value that comes after a colon in a key\-value pair\.

```csharp
private ToonNet.Core.Models.ToonValue ParseValueAfterColon(int indentLevel, System.Nullable<int> arrayLength, string[]? fieldNames);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ParseValueAfterColon(int,System.Nullable_int_,string[]).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current indentation level\.

<a name='ToonNet.Core.Parsing.ToonParser.ParseValueAfterColon(int,System.Nullable_int_,string[]).arrayLength'></a>

`arrayLength` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

Optional array length from notation\.

<a name='ToonNet.Core.Parsing.ToonParser.ParseValueAfterColon(int,System.Nullable_int_,string[]).fieldNames'></a>

`fieldNames` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

Optional field names from notation\.

#### Returns
[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')  
The parsed value\.