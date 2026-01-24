#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models').[ToonTokenTypeExtensions](ToonNet.Core.Models.ToonTokenTypeExtensions.md 'ToonNet\.Core\.Models\.ToonTokenTypeExtensions')

## ToonTokenTypeExtensions\.Is\(this ToonTokenType, ToonTokenCategory\) Method

Checks if a token type belongs to the specified category using bitmask operations\.

```csharp
private static bool Is(this ToonNet.Core.Models.ToonTokenType type, ToonNet.Core.Models.ToonTokenCategory category);
```
#### Parameters

<a name='ToonNet.Core.Models.ToonTokenTypeExtensions.Is(thisToonNet.Core.Models.ToonTokenType,ToonNet.Core.Models.ToonTokenCategory).type'></a>

`type` [ToonTokenType](ToonNet.Core.Models.ToonTokenType.md 'ToonNet\.Core\.Models\.ToonTokenType')

The token type to check\.

<a name='ToonNet.Core.Models.ToonTokenTypeExtensions.Is(thisToonNet.Core.Models.ToonTokenType,ToonNet.Core.Models.ToonTokenCategory).category'></a>

`category` [ToonTokenCategory](ToonNet.Core.Models.ToonTokenCategory.md 'ToonNet\.Core\.Models\.ToonTokenCategory')

The category to test for\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if the token belongs to the category; otherwise, false\.