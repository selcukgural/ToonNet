#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Http](ToonNet.AspNetCore.Mvc.Http.md 'ToonNet\.AspNetCore\.Mvc\.Http').[ToonResult](ToonNet.AspNetCore.Mvc.Http.ToonResult.md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult')

## ToonResult\.\_options Field

Represents the serialization options for the ToonResult output\.
When provided, these options determine the behavior of TOON serialization such as
property naming policies, null value handling, type information inclusion, and more\.

```csharp
private readonly ToonSerializerOptions? _options;
```

#### Field Value
[ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')