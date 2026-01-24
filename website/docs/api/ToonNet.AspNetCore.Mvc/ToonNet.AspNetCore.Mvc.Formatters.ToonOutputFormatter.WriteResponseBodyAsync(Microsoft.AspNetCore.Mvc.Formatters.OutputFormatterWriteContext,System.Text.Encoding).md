#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Formatters](ToonNet.AspNetCore.Mvc.Formatters.md 'ToonNet\.AspNetCore\.Mvc\.Formatters').[ToonOutputFormatter](ToonNet.AspNetCore.Mvc.Formatters.ToonOutputFormatter.md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonOutputFormatter')

## ToonOutputFormatter\.WriteResponseBodyAsync\(OutputFormatterWriteContext, Encoding\) Method

Writes the response body by serializing the specified object into TOON format
and writing it to the HTTP response stream asynchronously\.

```csharp
public override System.Threading.Tasks.Task WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context, System.Text.Encoding selectedEncoding);
```
#### Parameters

<a name='ToonNet.AspNetCore.Mvc.Formatters.ToonOutputFormatter.WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext,System.Text.Encoding).context'></a>

`context` [Microsoft\.AspNetCore\.Mvc\.Formatters\.OutputFormatterWriteContext](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.formatters.outputformatterwritecontext 'Microsoft\.AspNetCore\.Mvc\.Formatters\.OutputFormatterWriteContext')

The context containing the object to serialize and HTTP response details\.

<a name='ToonNet.AspNetCore.Mvc.Formatters.ToonOutputFormatter.WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext,System.Text.Encoding).selectedEncoding'></a>

`selectedEncoding` [System\.Text\.Encoding](https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding 'System\.Text\.Encoding')

The character encoding used for the serialized response body\.

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A task that represents the asynchronous operation of writing the response body\.