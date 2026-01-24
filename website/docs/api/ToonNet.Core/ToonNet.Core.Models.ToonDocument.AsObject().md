#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models').[ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument')

## ToonDocument\.AsObject\(\) Method

Attempts to treat the document root as an object\.

```csharp
public ToonNet.Core.Models.ToonObject AsObject();
```

#### Returns
[ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject')  
The root value cast to [ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject')\.

#### Exceptions

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
Thrown when the root value is not an instance of [ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject')\.

### Remarks
Use this method when you expect the root value to be an object\. If the root is not
of the expected type, an exception is thrown to indicate the mismatch\.