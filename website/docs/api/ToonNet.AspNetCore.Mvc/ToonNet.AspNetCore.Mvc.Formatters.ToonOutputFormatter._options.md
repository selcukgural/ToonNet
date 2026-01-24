#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Formatters](ToonNet.AspNetCore.Mvc.Formatters.md 'ToonNet\.AspNetCore\.Mvc\.Formatters').[ToonOutputFormatter](ToonNet.AspNetCore.Mvc.Formatters.ToonOutputFormatter.md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonOutputFormatter')

## ToonOutputFormatter\.\_options Field

Holds the configuration settings required for TOON serialization processing
within the [ToonOutputFormatter](ToonNet.AspNetCore.Mvc.Formatters.ToonOutputFormatter.md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonOutputFormatter')\. This includes options
that govern serialization rules such as property naming conventions,
handling of null values, depth restrictions, and custom converter support\.

```csharp
private readonly ToonSerializerOptions _options;
```

#### Field Value
[ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')