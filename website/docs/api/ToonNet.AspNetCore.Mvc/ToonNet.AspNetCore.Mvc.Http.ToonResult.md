#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Http](ToonNet.AspNetCore.Mvc.Http.md 'ToonNet\.AspNetCore\.Mvc\.Http')

## ToonResult Class

Represents a result that writes TOON encoded content as an [Microsoft\.AspNetCore\.Http\.IResult](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.iresult 'Microsoft\.AspNetCore\.Http\.IResult')\.

```csharp
public sealed class ToonResult : Microsoft.AspNetCore.Http.IResult
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonResult

Implements [Microsoft\.AspNetCore\.Http\.IResult](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.iresult 'Microsoft\.AspNetCore\.Http\.IResult')

| Constructors | |
| :--- | :--- |
| [ToonResult\(object, ToonSerializerOptions\)](ToonNet.AspNetCore.Mvc.Http.ToonResult.ToonResult(object,ToonNet.Core.Serialization.ToonSerializerOptions).md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult\.ToonResult\(object, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | An [Microsoft\.AspNetCore\.Http\.IResult](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.iresult 'Microsoft\.AspNetCore\.Http\.IResult') that writes TOON encoded content\. |

| Fields | |
| :--- | :--- |
| [\_options](ToonNet.AspNetCore.Mvc.Http.ToonResult._options.md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult\.\_options') | Represents the serialization options for the ToonResult output\. When provided, these options determine the behavior of TOON serialization such as property naming policies, null value handling, type information inclusion, and more\. |
| [\_value](ToonNet.AspNetCore.Mvc.Http.ToonResult._value.md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult\.\_value') | Represents the value to be serialized in the [ToonResult](ToonNet.AspNetCore.Mvc.Http.ToonResult.md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult') response\. This value is typically an object that will be encoded using the TOON serialization format specified by [ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')\. |

| Methods | |
| :--- | :--- |
| [ExecuteAsync\(HttpContext\)](ToonNet.AspNetCore.Mvc.Http.ToonResult.ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext).md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult\.ExecuteAsync\(Microsoft\.AspNetCore\.Http\.HttpContext\)') | Executes the result operation of the current context asynchronously\. |
