#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Formatters](ToonNet.AspNetCore.Mvc.Formatters.md 'ToonNet\.AspNetCore\.Mvc\.Formatters').[ToonInputFormatter](ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.md 'ToonNet\.AspNetCore\.Mvc\.Formatters\.ToonInputFormatter')

## ToonInputFormatter\.ReadRequestBodyAsync\(InputFormatterContext, Encoding\) Method

Asynchronously reads the request body, deserializes it into a \.NET object, and returns the result\.

```csharp
public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Formatters.InputFormatterResult> ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context, System.Text.Encoding encoding);
```
#### Parameters

<a name='ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext,System.Text.Encoding).context'></a>

`context` [Microsoft\.AspNetCore\.Mvc\.Formatters\.InputFormatterContext](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.formatters.inputformattercontext 'Microsoft\.AspNetCore\.Mvc\.Formatters\.InputFormatterContext')

The context containing information about the request to process\.

<a name='ToonNet.AspNetCore.Mvc.Formatters.ToonInputFormatter.ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext,System.Text.Encoding).encoding'></a>

`encoding` [System\.Text\.Encoding](https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding 'System\.Text\.Encoding')

The character encoding of the request body\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Microsoft\.AspNetCore\.Mvc\.Formatters\.InputFormatterResult](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.formatters.inputformatterresult 'Microsoft\.AspNetCore\.Mvc\.Formatters\.InputFormatterResult')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
A [System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task') that, when completed, contains an [Microsoft\.AspNetCore\.Mvc\.Formatters\.InputFormatterResult](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.formatters.inputformatterresult 'Microsoft\.AspNetCore\.Mvc\.Formatters\.InputFormatterResult') representing
the deserialization outcome\.
Returns a successful result with the deserialized object if deserialization is successful;
otherwise, returns a failure result\.