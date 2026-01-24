#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding')

## ToonEncoder Class

Provides functionality to encode a [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument') into a TOON format string\.

```csharp
public sealed class ToonEncoder
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonEncoder

### Example

```csharp
var document = new ToonDocument(new ToonObject
{
    { "key", new ToonString("value") }
});
var encoder = new ToonEncoder();
var encodedString = encoder.Encode(document);
Console.WriteLine(encodedString);
```

### Remarks
This class is designed to be used for serializing [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument') instances into 
their string representation in the TOON format\. It supports various TOON value types such as 
objects, arrays, strings, numbers, booleans, and nulls\. The encoder ensures proper formatting 
and handles indentation, quoting, and escaping as per the TOON specification\.

| Constructors | |
| :--- | :--- |
| [ToonEncoder\(ToonOptions\)](ToonNet.Core.Encoding.ToonEncoder.ToonEncoder(ToonNet.Core.ToonOptions).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.ToonEncoder\(ToonNet\.Core\.ToonOptions\)') | Provides functionality to encode a [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument') into a TOON format string\. |

| Methods | |
| :--- | :--- |
| [Encode\(ToonDocument\)](ToonNet.Core.Encoding.ToonEncoder.Encode(ToonNet.Core.Models.ToonDocument).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.Encode\(ToonNet\.Core\.Models\.ToonDocument\)') | Encodes a TOON document into its string representation\. |
| [EncodeArray\(ToonArray, int\)](ToonNet.Core.Encoding.ToonEncoder.EncodeArray(ToonNet.Core.Models.ToonArray,int).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodeArray\(ToonNet\.Core\.Models\.ToonArray, int\)') | Encodes an array to TOON format\. |
| [EncodeArrayHeader\(ToonArray\)](ToonNet.Core.Encoding.ToonEncoder.EncodeArrayHeader(ToonNet.Core.Models.ToonArray).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodeArrayHeader\(ToonNet\.Core\.Models\.ToonArray\)') | Encodes the array header with length and optional field names\. |
| [EncodeAsync\(ToonDocument, CancellationToken\)](ToonNet.Core.Encoding.ToonEncoder.EncodeAsync(ToonNet.Core.Models.ToonDocument,System.Threading.CancellationToken).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodeAsync\(ToonNet\.Core\.Models\.ToonDocument, System\.Threading\.CancellationToken\)') | Asynchronously encodes a TOON document into its string representation\. |
| [EncodeListArray\(ToonArray, int\)](ToonNet.Core.Encoding.ToonEncoder.EncodeListArray(ToonNet.Core.Models.ToonArray,int).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodeListArray\(ToonNet\.Core\.Models\.ToonArray, int\)') | Encodes an array as a list with each item prefixed by a '\-' character\. |
| [EncodeObject\(ToonObject, int\)](ToonNet.Core.Encoding.ToonEncoder.EncodeObject(ToonNet.Core.Models.ToonObject,int).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodeObject\(ToonNet\.Core\.Models\.ToonObject, int\)') | Encodes an object to TOON format\. |
| [EncodeObjectInline\(ToonObject, int\)](ToonNet.Core.Encoding.ToonEncoder.EncodeObjectInline(ToonNet.Core.Models.ToonObject,int).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodeObjectInline\(ToonNet\.Core\.Models\.ToonObject, int\)') | Encodes an object inline \(first property on the same line, rest indented\)\. |
| [EncodePrimitiveArrayInline\(ToonArray\)](ToonNet.Core.Encoding.ToonEncoder.EncodePrimitiveArrayInline(ToonNet.Core.Models.ToonArray).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodePrimitiveArrayInline\(ToonNet\.Core\.Models\.ToonArray\)') | Encodes a primitive array as an inline comma\-separated list\. |
| [EncodeTabularArray\(ToonArray, int\)](ToonNet.Core.Encoding.ToonEncoder.EncodeTabularArray(ToonNet.Core.Models.ToonArray,int).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodeTabularArray\(ToonNet\.Core\.Models\.ToonArray, int\)') | Encodes a tabular array \(array of objects with field names\) into the TOON format\. |
| [EncodeToFileAsync\(ToonDocument, string, CancellationToken\)](ToonNet.Core.Encoding.ToonEncoder.EncodeToFileAsync(ToonNet.Core.Models.ToonDocument,string,System.Threading.CancellationToken).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodeToFileAsync\(ToonNet\.Core\.Models\.ToonDocument, string, System\.Threading\.CancellationToken\)') | Asynchronously encodes a TOON document and writes it to a file\. |
| [EncodeToStreamAsync\(ToonDocument, Stream, CancellationToken\)](ToonNet.Core.Encoding.ToonEncoder.EncodeToStreamAsync(ToonNet.Core.Models.ToonDocument,System.IO.Stream,System.Threading.CancellationToken).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodeToStreamAsync\(ToonNet\.Core\.Models\.ToonDocument, System\.IO\.Stream, System\.Threading\.CancellationToken\)') | Asynchronously encodes a TOON document and writes it to a stream\. |
| [EncodeValue\(ToonValue, int\)](ToonNet.Core.Encoding.ToonEncoder.EncodeValue(ToonNet.Core.Models.ToonValue,int).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EncodeValue\(ToonNet\.Core\.Models\.ToonValue, int\)') | Encodes a [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue') into the internal string builder\. |
| [EscapeString\(string\)](ToonNet.Core.Encoding.ToonEncoder.EscapeString(string).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.EscapeString\(string\)') | Escapes special characters in a string to ensure compatibility with the TOON format\. |
| [FormatNumber\(double\)](ToonNet.Core.Encoding.ToonEncoder.FormatNumber(double).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.FormatNumber\(double\)') | Formats a numeric value as a TOON\-compatible string\. |
| [FormatValue\(ToonValue\)](ToonNet.Core.Encoding.ToonEncoder.FormatValue(ToonNet.Core.Models.ToonValue).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.FormatValue\(ToonNet\.Core\.Models\.ToonValue\)') | Formats a TOON value as a string\. |
| [IsPrimitiveArray\(ToonArray\)](ToonNet.Core.Encoding.ToonEncoder.IsPrimitiveArray(ToonNet.Core.Models.ToonArray).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.IsPrimitiveArray\(ToonNet\.Core\.Models\.ToonArray\)') | Checks if an array contains only primitive values\. |
| [NeedsQuoting\(string\)](ToonNet.Core.Encoding.ToonEncoder.NeedsQuoting(string).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.NeedsQuoting\(string\)') | Checks if a string needs to be quoted based on the TOON format specification\. |
| [QuoteIfNeeded\(string\)](ToonNet.Core.Encoding.ToonEncoder.QuoteIfNeeded(string).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.QuoteIfNeeded\(string\)') | Quotes a string value if it needs quoting\. |
| [QuoteKeyIfNeeded\(string\)](ToonNet.Core.Encoding.ToonEncoder.QuoteKeyIfNeeded(string).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.QuoteKeyIfNeeded\(string\)') | Quotes a key if it needs quoting\. |
| [WriteIndent\(int\)](ToonNet.Core.Encoding.ToonEncoder.WriteIndent(int).md 'ToonNet\.Core\.Encoding\.ToonEncoder\.WriteIndent\(int\)') | Writes indentation to the output string builder based on the specified indentation level\. |
