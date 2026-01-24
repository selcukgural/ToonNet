#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Http](ToonNet.AspNetCore.Mvc.Http.md 'ToonNet\.AspNetCore\.Mvc\.Http').[ToonResult](ToonNet.AspNetCore.Mvc.Http.ToonResult.md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult')

## ToonResult\.ExecuteAsync\(HttpContext\) Method

Executes the result operation of the current context asynchronously\.

```csharp
public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext httpContext);
```
#### Parameters

<a name='ToonNet.AspNetCore.Mvc.Http.ToonResult.ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext).httpContext'></a>

`httpContext` [Microsoft\.AspNetCore\.Http\.HttpContext](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext 'Microsoft\.AspNetCore\.Http\.HttpContext')

The [Microsoft\.AspNetCore\.Http\.HttpContext](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext 'Microsoft\.AspNetCore\.Http\.HttpContext') containing the HTTP response to write to\.

Implements [ExecuteAsync\(HttpContext\)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.iresult.executeasync#microsoft-aspnetcore-http-iresult-executeasync(microsoft-aspnetcore-http-httpcontext) 'Microsoft\.AspNetCore\.Http\.IResult\.ExecuteAsync\(Microsoft\.AspNetCore\.Http\.HttpContext\)')

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A [System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task') that represents the asynchronous operate completion\.