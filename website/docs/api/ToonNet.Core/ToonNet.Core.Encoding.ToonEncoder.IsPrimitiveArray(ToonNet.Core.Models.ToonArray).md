#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.IsPrimitiveArray\(ToonArray\) Method

Checks if an array contains only primitive values\.

```csharp
private static bool IsPrimitiveArray(ToonNet.Core.Models.ToonArray array);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.IsPrimitiveArray(ToonNet.Core.Models.ToonArray).array'></a>

`array` [ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')

The array to check\. This parameter must not be null\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if all items in the array are primitive values \(null, boolean, number, or string\);
otherwise, false\.

### Remarks
This method is used to determine if an array can be encoded as a single line
of comma\-separated values\. Non\-primitive items \(e\.g\., objects or arrays\) will
cause the method to return false\.