#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models').[ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument')

## ToonDocument\.AsArray\(\) Method

Attempts to treat the document root as an array\.

```csharp
public ToonNet.Core.Models.ToonArray AsArray();
```

#### Returns
[ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')  
The root value cast to [ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')\.

#### Exceptions

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
Thrown when the root value is not an instance of [ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')\.

### Remarks
Use this method when you expect the root value to be an array\. If the root is not
of the expected type, an exception is thrown to indicate the mismatch\.