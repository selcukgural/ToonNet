#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models').[ToonToken](ToonNet.Core.Models.ToonToken.md 'ToonNet\.Core\.Models\.ToonToken')

## ToonToken\(ToonTokenType, ReadOnlyMemory\<char\>, int, int\) Constructor

Represents a token in TOON format with its type, value, and position\.

```csharp
public ToonToken(ToonNet.Core.Models.ToonTokenType type, System.ReadOnlyMemory<char> value, int line, int column);
```
#### Parameters

<a name='ToonNet.Core.Models.ToonToken.ToonToken(ToonNet.Core.Models.ToonTokenType,System.ReadOnlyMemory_char_,int,int).type'></a>

`type` [ToonTokenType](ToonNet.Core.Models.ToonTokenType.md 'ToonNet\.Core\.Models\.ToonTokenType')

<a name='ToonNet.Core.Models.ToonToken.ToonToken(ToonNet.Core.Models.ToonTokenType,System.ReadOnlyMemory_char_,int,int).value'></a>

`value` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Char](https://learn.microsoft.com/en-us/dotnet/api/system.char 'System\.Char')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='ToonNet.Core.Models.ToonToken.ToonToken(ToonNet.Core.Models.ToonTokenType,System.ReadOnlyMemory_char_,int,int).line'></a>

`line` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='ToonNet.Core.Models.ToonToken.ToonToken(ToonNet.Core.Models.ToonTokenType,System.ReadOnlyMemory_char_,int,int).column'></a>

`column` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

### Remarks
This is an internal implementation detail used by the lexer and parser\.
Users should use [ToonDocument](ToonNet.Core.Models.ToonDocument.md 'ToonNet\.Core\.Models\.ToonDocument') and [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue') instead\.