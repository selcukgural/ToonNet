#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core').[ToonSerializationException](ToonNet.Core.ToonSerializationException.md 'ToonNet\.Core\.ToonSerializationException')

## ToonSerializationException\.Create\(string, Type, string, object, string\) Method

Creates a serialization exception with context\.

```csharp
public static ToonNet.Core.ToonSerializationException Create(string message, System.Type? targetType=null, string? propertyName=null, object? value=null, string? suggestion=null);
```
#### Parameters

<a name='ToonNet.Core.ToonSerializationException.Create(string,System.Type,string,object,string).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The error message\.

<a name='ToonNet.Core.ToonSerializationException.Create(string,System.Type,string,object,string).targetType'></a>

`targetType` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The target type being serialized/deserialized\.

<a name='ToonNet.Core.ToonSerializationException.Create(string,System.Type,string,object,string).propertyName'></a>

`propertyName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The property name, if applicable\.

<a name='ToonNet.Core.ToonSerializationException.Create(string,System.Type,string,object,string).value'></a>

`value` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The value that caused the error\.

<a name='ToonNet.Core.ToonSerializationException.Create(string,System.Type,string,object,string).suggestion'></a>

`suggestion` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Optional suggestion for fixing the error\.

#### Returns
[ToonSerializationException](ToonNet.Core.ToonSerializationException.md 'ToonNet\.Core\.ToonSerializationException')  
A new ToonSerializationException instance\.