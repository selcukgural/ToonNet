#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Encoding](ToonNet.Core.Encoding.md 'ToonNet\.Core\.Encoding').[ToonEncoder](ToonNet.Core.Encoding.ToonEncoder.md 'ToonNet\.Core\.Encoding\.ToonEncoder')

## ToonEncoder\.WriteIndent\(int\) Method

Writes indentation to the output string builder based on the specified indentation level\.

```csharp
private void WriteIndent(int indentLevel);
```
#### Parameters

<a name='ToonNet.Core.Encoding.ToonEncoder.WriteIndent(int).indentLevel'></a>

`indentLevel` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The indentation level, in spaces\. If the level is less than or equal to zero, no indentation is added\.

### Remarks
This method uses a cached array of precomputed indentation strings for common levels to minimize
allocations\. For uncommon levels, it generates the indentation dynamically\. If the level is odd,
an additional space is appended to the cached string\.