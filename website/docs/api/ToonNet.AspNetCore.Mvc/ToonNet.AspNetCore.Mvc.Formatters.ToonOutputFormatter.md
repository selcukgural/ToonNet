#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Formatters](ToonNet.AspNetCore.Mvc.Formatters.md 'ToonNet\.AspNetCore\.Mvc\.Formatters')

## ToonOutputFormatter Class

A formatter that writes responses in the TOON serialized format, extending [Microsoft\.AspNetCore\.Mvc\.Formatters\.TextOutputFormatter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.formatters.textoutputformatter 'Microsoft\.AspNetCore\.Mvc\.Formatters\.TextOutputFormatter')\.

```csharp
public sealed class ToonOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextOutputFormatter
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [Microsoft\.AspNetCore\.Mvc\.Formatters\.OutputFormatter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.formatters.outputformatter 'Microsoft\.AspNetCore\.Mvc\.Formatters\.OutputFormatter') &#129106; [Microsoft\.AspNetCore\.Mvc\.Formatters\.TextOutputFormatter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.formatters.textoutputformatter 'Microsoft\.AspNetCore\.Mvc\.Formatters\.TextOutputFormatter') &#129106; ToonOutputFormatter

| Constructors | |
| :--- | :--- |
| [ToonOutputFormatter\(ToonSerializerOptions\)](ToonNet.AspNetCore.Mvc.Formatters.ToonOutputFormatter.ToonOutputFormatter(ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonOutputFormatter\.ToonOutputFormatter\(ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | A formatter that outputs TOON encoded content for ASP\.NET Core MVC responses\. |

| Fields | |
| :--- | :--- |
| [\_options](ToonNet.AspNetCore.Mvc.Formatters.ToonOutputFormatter._options.md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonOutputFormatter\.\_options') | Holds the configuration settings required for TOON serialization processing within the [ToonOutputFormatter](ToonNet.AspNetCore.Mvc.Formatters.ToonOutputFormatter.md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonOutputFormatter')\. This includes options that govern serialization rules such as property naming conventions, handling of null values, depth restrictions, and custom converter support\. |

| Methods | |
| :--- | :--- |
| [WriteResponseBodyAsync\(OutputFormatterWriteContext, Encoding\)](ToonNet.AspNetCore.Mvc.Formatters.ToonOutputFormatter.WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext,System.Text.Encoding).md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonOutputFormatter\.WriteResponseBodyAsync\(Microsoft\.AspNetCore\.Mvc\.Formatters\.OutputFormatterWriteContext, System\.Text\.Encoding\)') | Writes the response body by serializing the specified object into TOON format and writing it to the HTTP response stream asynchronously\. |
