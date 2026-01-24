#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Formatters](ToonNet.AspNetCore.Mvc.Formatters.md 'ToonNet\.AspNetCore\.Mvc\.Formatters')

## ToonInputFormatter Class

A [Microsoft\.AspNetCore\.Mvc\.Formatters\.TextInputFormatter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.formatters.textinputformatter 'Microsoft\.AspNetCore\.Mvc\.Formatters\.TextInputFormatter') that processes incoming TOON\-encoded content and deserializes it into \.NET objects\.

```csharp
public sealed class ToonInputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextInputFormatter
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [Microsoft\.AspNetCore\.Mvc\.Formatters\.InputFormatter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.formatters.inputformatter 'Microsoft\.AspNetCore\.Mvc\.Formatters\.InputFormatter') &#129106; [Microsoft\.AspNetCore\.Mvc\.Formatters\.TextInputFormatter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.formatters.textinputformatter 'Microsoft\.AspNetCore\.Mvc\.Formatters\.TextInputFormatter') &#129106; ToonInputFormatter

### Remarks
This formatter supports TOON\-specific media types as defined by [ToonFormatterDefaults](ToonNet.AspNetCore.Mvc.Formatters.ToonFormatterDefaults.md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonFormatterDefaults') and ensures compatibility
with UTF\-8 and Unicode encodings\. The deserialization behavior can be customized using [ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')\.

| Constructors | |
| :--- | :--- |
| [ToonInputFormatter\(ToonSerializerOptions\)](ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.ToonInputFormatter(ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonInputFormatter\.ToonInputFormatter\(ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Initializes a new instance of [ToonInputFormatter](ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonInputFormatter')\. |

| Methods | |
| :--- | :--- |
| [DeserializeFromStreamInternalAsync\(Type, Stream, ToonSerializerOptions, CancellationToken\)](ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.DeserializeFromStreamInternalAsync(System.Type,System.IO.Stream,ToonNet.Core.Serialization.ToonSerializerOptions,System.Threading.CancellationToken).md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonInputFormatter\.DeserializeFromStreamInternalAsync\(System\.Type, System\.IO\.Stream, ToonNet\.Core\.Serialization\.ToonSerializerOptions, System\.Threading\.CancellationToken\)') | Deserializes the content of a stream into an object of the specified type using the provided options\. |
| [ReadRequestBodyAsync\(InputFormatterContext, Encoding\)](ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext,System.Text.Encoding).md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonInputFormatter\.ReadRequestBodyAsync\(Microsoft\.AspNetCore\.Mvc\.Formatters\.InputFormatterContext, System\.Text\.Encoding\)') | Asynchronously reads the request body, deserializes it into a \.NET object, and returns the result\. |
