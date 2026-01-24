#### [ToonNet\.AspNetCore\.Mvc](index.md 'index')
### [ToonNet\.AspNetCore\.Mvc\.Http](ToonNet.AspNetCore.Mvc.Http.md 'ToonNet\.AspNetCore\.Mvc\.Http').[ToonHttpResultExtensions](ToonNet.AspNetCore.Mvc.Http.ToonHttpResultExtensions.md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonHttpResultExtensions')

## ToonHttpResultExtensions\.Toon\(this IResultExtensions, object, ToonSerializerOptions\) Method

Creates a [ToonResult](ToonNet.AspNetCore.Mvc.Http.ToonResult.md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult') that serializes the specified [value](ToonNet.AspNetCore.Mvc.Http.ToonHttpResultExtensions.Toon(thisMicrosoft.AspNetCore.Http.IResultExtensions,object,ToonNet.Core.Serialization.ToonSerializerOptions).md#ToonNet.AspNetCore.Mvc.Http.ToonHttpResultExtensions.Toon(thisMicrosoft.AspNetCore.Http.IResultExtensions,object,ToonNet.Core.Serialization.ToonSerializerOptions).value 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonHttpResultExtensions\.Toon\(this Microsoft\.AspNetCore\.Http\.IResultExtensions, object, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)\.value') to TOON format\.

```csharp
public static Microsoft.AspNetCore.Http.IResult Toon(this Microsoft.AspNetCore.Http.IResultExtensions resultExtensions, object? value, ToonNet.Core.Serialization.ToonSerializerOptions? options=null);
```
#### Parameters

<a name='ToonNet.AspNetCore.Mvc.Http.ToonHttpResultExtensions.Toon(thisMicrosoft.AspNetCore.Http.IResultExtensions,object,ToonNet.Core.Serialization.ToonSerializerOptions).resultExtensions'></a>

`resultExtensions` [Microsoft\.AspNetCore\.Http\.IResultExtensions](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.iresultextensions 'Microsoft\.AspNetCore\.Http\.IResultExtensions')

The result extensions\.

<a name='ToonNet.AspNetCore.Mvc.Http.ToonHttpResultExtensions.Toon(thisMicrosoft.AspNetCore.Http.IResultExtensions,object,ToonNet.Core.Serialization.ToonSerializerOptions).value'></a>

`value` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The value to serialize\.

<a name='ToonNet.AspNetCore.Mvc.Http.ToonHttpResultExtensions.Toon(thisMicrosoft.AspNetCore.Http.IResultExtensions,object,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonNet\.Core\.Serialization\.ToonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/api/toonnet.core.serialization.toonserializeroptions 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional serializer options\.

#### Returns
[Microsoft\.AspNetCore\.Http\.IResult](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.iresult 'Microsoft\.AspNetCore\.Http\.IResult')  
The created [ToonResult](ToonNet.AspNetCore.Mvc.Http.ToonResult.md 'ToonNet\.AspNetCore\.Mvc\.Http\.ToonResult')\.