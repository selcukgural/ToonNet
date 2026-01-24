#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing')

## ToonParser Class

Parses TOON tokens into a document structure\.

```csharp
internal sealed class ToonParser
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonParser

### Remarks
This is an internal implementation detail\. Users should use [ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer') instead\.

| Constructors | |
| :--- | :--- |
| [ToonParser\(ToonOptions\)](ToonNet.Core.Parsing.ToonParser.ToonParser(ToonNet.Core.ToonOptions).md 'ToonNet\.Core\.Parsing\.ToonParser\.ToonParser\(ToonNet\.Core\.ToonOptions\)') | Parses TOON tokens into a document structure\. |

| Methods | |
| :--- | :--- |
| [Advance\(\)](ToonNet.Core.Parsing.ToonParser.Advance().md 'ToonNet\.Core\.Parsing\.ToonParser\.Advance\(\)') | Advances to the next token and returns the current token\. |
| [ExpectToken\(ToonTokenType\)](ToonNet.Core.Parsing.ToonParser.ExpectToken(ToonNet.Core.Models.ToonTokenType).md 'ToonNet\.Core\.Parsing\.ToonParser\.ExpectToken\(ToonNet\.Core\.Models\.ToonTokenType\)') | Checks if the current token is of the expected type\. |
| [GetCurrentIndent\(\)](ToonNet.Core.Parsing.ToonParser.GetCurrentIndent().md 'ToonNet\.Core\.Parsing\.ToonParser\.GetCurrentIndent\(\)') | Gets the current indentation level\. |
| [GetCurrentIndentAndAdvance\(\)](ToonNet.Core.Parsing.ToonParser.GetCurrentIndentAndAdvance().md 'ToonNet\.Core\.Parsing\.ToonParser\.GetCurrentIndentAndAdvance\(\)') | Gets the current token's indent level and advances the parser position\. |
| [IsAtEnd\(\)](ToonNet.Core.Parsing.ToonParser.IsAtEnd().md 'ToonNet\.Core\.Parsing\.ToonParser\.IsAtEnd\(\)') | Checks if the parser has reached the end of tokens\. |
| [IsFollowedByListItem\(int\)](ToonNet.Core.Parsing.ToonParser.IsFollowedByListItem(int).md 'ToonNet\.Core\.Parsing\.ToonParser\.IsFollowedByListItem\(int\)') | Checks if the token stream is followed by a list item pattern \(Indent \+ ListItem\)\. |
| [IsValueToken\(ToonTokenType\)](ToonNet.Core.Parsing.ToonParser.IsValueToken(ToonNet.Core.Models.ToonTokenType).md 'ToonNet\.Core\.Parsing\.ToonParser\.IsValueToken\(ToonNet\.Core\.Models\.ToonTokenType\)') | Checks if a token type represents a value \(either Value or QuotedString\)\. |
| [Parse\(string\)](ToonNet.Core.Parsing.ToonParser.Parse.md#ToonNet.Core.Parsing.ToonParser.Parse(string) 'ToonNet\.Core\.Parsing\.ToonParser\.Parse\(string\)') | Parses a TOON format string into a document\. |
| [Parse\(List&lt;ToonToken&gt;\)](ToonNet.Core.Parsing.ToonParser.Parse.md#ToonNet.Core.Parsing.ToonParser.Parse(System.Collections.Generic.List_ToonNet.Core.Models.ToonToken_) 'ToonNet\.Core\.Parsing\.ToonParser\.Parse\(System\.Collections\.Generic\.List\<ToonNet\.Core\.Models\.ToonToken\>\)') | Parses a list of TOON tokens into a document\. |
| [ParseAdditionalObjectProperties\(ToonObject, int\)](ToonNet.Core.Parsing.ToonParser.ParseAdditionalObjectProperties(ToonNet.Core.Models.ToonObject,int).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseAdditionalObjectProperties\(ToonNet\.Core\.Models\.ToonObject, int\)') | Parses additional object properties at a higher indentation level\. Used for list items with properties across multiple lines\. |
| [ParseArrayNotation\(\)](ToonNet.Core.Parsing.ToonParser.ParseArrayNotation().md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseArrayNotation\(\)') | Parses array notation \(length and/or field names\) if present\. |
| [ParseAsync\(string, CancellationToken\)](ToonNet.Core.Parsing.ToonParser.ParseAsync(string,System.Threading.CancellationToken).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseAsync\(string, System\.Threading\.CancellationToken\)') | Asynchronously parses a TOON format string into a document\. |
| [ParseFromFileAsync\(string, CancellationToken\)](ToonNet.Core.Parsing.ToonParser.ParseFromFileAsync(string,System.Threading.CancellationToken).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseFromFileAsync\(string, System\.Threading\.CancellationToken\)') | Asynchronously parses a TOON document from a file\. |
| [ParseFromStreamAsync\(Stream, CancellationToken\)](ToonNet.Core.Parsing.ToonParser.ParseFromStreamAsync(System.IO.Stream,System.Threading.CancellationToken).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseFromStreamAsync\(System\.IO\.Stream, System\.Threading\.CancellationToken\)') | Asynchronously parses a TOON document from a stream\. |
| [ParseInlinePrimitiveArray\(int\)](ToonNet.Core.Parsing.ToonParser.ParseInlinePrimitiveArray(int).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseInlinePrimitiveArray\(int\)') | Parses an inline array of primitive values\. |
| [ParseList\(int\)](ToonNet.Core.Parsing.ToonParser.ParseList(int).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseList\(int\)') | Parses a list \(items prefixed with '\-'\)\. |
| [ParseListItemScalar\(\)](ToonNet.Core.Parsing.ToonParser.ParseListItemScalar().md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseListItemScalar\(\)') | Parses a scalar list item \(\- value\)\. |
| [ParseObject\(int\)](ToonNet.Core.Parsing.ToonParser.ParseObject(int).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseObject\(int\)') | Parses an object from the token stream\. |
| [ParsePrimitiveValue\(ReadOnlyMemory&lt;char&gt;\)](ToonNet.Core.Parsing.ToonParser.ParsePrimitiveValue(System.ReadOnlyMemory_char_).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParsePrimitiveValue\(System\.ReadOnlyMemory\<char\>\)') | Parses a primitive value from a token's memory\. |
| [ParseTabularArray\(int, Nullable&lt;int&gt;, string\[\]\)](ToonNet.Core.Parsing.ToonParser.ParseTabularArray(int,System.Nullable_int_,string[]).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseTabularArray\(int, System\.Nullable\<int\>, string\[\]\)') | Parses a tabular array \(array of objects with field names\)\. |
| [ParseTabularArrayInlineRow\(string\[\]\)](ToonNet.Core.Parsing.ToonParser.ParseTabularArrayInlineRow(string[]).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseTabularArrayInlineRow\(string\[\]\)') | Parses an inline tabular array row \(comma\-separated values\)\. |
| [ParseValue\(int\)](ToonNet.Core.Parsing.ToonParser.ParseValue(int).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseValue\(int\)') | Parses a value at the specified indentation level\. |
| [ParseValueAfterColon\(int, Nullable&lt;int&gt;, string\[\]\)](ToonNet.Core.Parsing.ToonParser.ParseValueAfterColon(int,System.Nullable_int_,string[]).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseValueAfterColon\(int, System\.Nullable\<int\>, string\[\]\)') | Parses the value that comes after a colon in a key\-value pair\. |
| [ParseValueToken\(ToonToken\)](ToonNet.Core.Parsing.ToonParser.ParseValueToken(ToonNet.Core.Models.ToonToken).md 'ToonNet\.Core\.Parsing\.ToonParser\.ParseValueToken\(ToonNet\.Core\.Models\.ToonToken\)') | Parses a value token \(either quoted string or primitive\)\. |
| [Peek\(\)](ToonNet.Core.Parsing.ToonParser.Peek().md 'ToonNet\.Core\.Parsing\.ToonParser\.Peek\(\)') | Peeks at the current token without advancing\. |
| [SkipNewlines\(\)](ToonNet.Core.Parsing.ToonParser.SkipNewlines().md 'ToonNet\.Core\.Parsing\.ToonParser\.SkipNewlines\(\)') | Skips all consecutive newline tokens\. |
| [SkipWhitespace\(\)](ToonNet.Core.Parsing.ToonParser.SkipWhitespace().md 'ToonNet\.Core\.Parsing\.ToonParser\.SkipWhitespace\(\)') | Skips all consecutive whitespace \(indent and newline\) tokens\. |
