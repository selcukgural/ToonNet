#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Http](ToonNet.AspNetCore.Mvc.Http.md 'ToonNet\.AspNetCore\.Mvc\.Http').[ToonResult](ToonNet.AspNetCore.Mvc.Http.ToonResult.md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult')

## ToonResult\.\_value Field

Represents the value to be serialized in the [ToonResult](ToonNet.AspNetCore.Mvc.Http.ToonResult.md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult') response\.
This value is typically an object that will be encoded using the TOON serialization format
specified by [ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')\.

```csharp
private readonly object? _value;
```

#### Field Value
[System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')