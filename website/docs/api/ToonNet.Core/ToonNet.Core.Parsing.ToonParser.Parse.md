#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.Parse Method

| Overloads | |
| :--- | :--- |
| [Parse\(string\)](ToonNet.Core.Parsing.ToonParser.Parse.md#ToonNet.Core.Parsing.ToonParser.Parse(string) 'ToonNet\.Core\.Parsing\.ToonParser\.Parse\(string\)') | Parses a TOON format string into a document\. |
| [Parse\(List&lt;ToonToken&gt;\)](ToonNet.Core.Parsing.ToonParser.Parse.md#ToonNet.Core.Parsing.ToonParser.Parse(System.Collections.Generic.List_ToonNet.Core.Models.ToonToken_) 'ToonNet\.Core\.Parsing\.ToonParser\.Parse\(System\.Collections\.Generic\.List\<ToonNet\.Core\.Models\.ToonToken\>\)') | Parses a list of TOON tokens into a document\. |

<a name='ToonNet.Core.Parsing.ToonParser.Parse(string)'></a>

## ToonParser\.Parse\(string\) Method

Parses a TOON format string into a document\.

```csharp
public ToonNet.Core.Models.ToonDocument Parse(string input);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.Parse(string).input'></a>

`input` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The TOON format string to parse\.

#### Returns
[ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument')  
A ToonDocument representing the parsed input\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when input is null\.

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when the input is invalid\.

<a name='ToonNet.Core.Parsing.ToonParser.Parse(System.Collections.Generic.List_ToonNet.Core.Models.ToonToken_)'></a>

## ToonParser\.Parse\(List\<ToonToken\>\) Method

Parses a list of TOON tokens into a document\.

```csharp
internal ToonNet.Core.Models.ToonDocument Parse(System.Collections.Generic.List<ToonNet.Core.Models.ToonToken> tokens);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.Parse(System.Collections.Generic.List_ToonNet.Core.Models.ToonToken_).tokens'></a>

`tokens` [System\.Collections\.Generic\.List&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1 'System\.Collections\.Generic\.List\`1')[ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1 'System\.Collections\.Generic\.List\`1')

The tokens to parse\.

#### Returns
[ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument')  
A ToonDocument representing the parsed tokens\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when the tokens are invalid\.