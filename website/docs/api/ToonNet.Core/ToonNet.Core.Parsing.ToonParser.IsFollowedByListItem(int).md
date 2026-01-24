#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Parsing](ToonNet.Core.Parsing.md 'ToonNet\.Core\.Parsing').[ToonParser](ToonNet.Core.Parsing.ToonParser.md 'ToonNet\.Core\.Parsing\.ToonParser')

## ToonParser\.IsFollowedByListItem\(int\) Method

Checks if the token stream is followed by a list item pattern \(Indent \+ ListItem\)\.

```csharp
private bool IsFollowedByListItem(int startPosition);
```
#### Parameters

<a name='ToonNet.Core.Parsing.ToonParser.IsFollowedByListItem(int).startPosition'></a>

`startPosition` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The position to start looking from\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True if followed by Indent, then ListItem; otherwise, false\.

### Remarks
Helper method to detect list arrays in lookahead scenarios\.
Skips newlines to find the next meaningful token\.