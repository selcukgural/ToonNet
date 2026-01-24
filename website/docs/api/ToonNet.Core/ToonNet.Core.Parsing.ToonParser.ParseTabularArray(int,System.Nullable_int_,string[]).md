#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.ParseTabularArray\(int, Nullable\<int\>, string\[\]\) Method

Parses a tabular array \(array of objects with field names\)\.

```csharp
private ToonNet.Core.Models.ToonArray ParseTabularArray(int indentLevel, System.Nullable<int> expectedLength, string[]? fieldNames);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.ParseTabularArray(int,System.Nullable_int_,string[]).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The current indentation level\.

<a name='ToonNet.Core.Parsing.ToonParser.ParseTabularArray(int,System.Nullable_int_,string[]).expectedLength'></a>

`expectedLength` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

The expected number of array elements, if specified\.

<a name='ToonNet.Core.Parsing.ToonParser.ParseTabularArray(int,System.Nullable_int_,string[]).fieldNames'></a>

`fieldNames` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

The field names for tabular data, if specified\.

#### Returns
[ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')  
A ToonArray with the parsed tabular data\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when array length mismatch occurs in strict mode\.