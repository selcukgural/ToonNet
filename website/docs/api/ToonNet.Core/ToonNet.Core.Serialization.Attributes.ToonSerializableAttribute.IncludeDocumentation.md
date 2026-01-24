#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization\.Attributes](ToonNet.Core.Serialization.Attributes.md 'ToonNet\.Core\.Serialization\.Attributes').[ToonSerializableAttribute](ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonSerializableAttribute')

## ToonSerializableAttribute\.IncludeDocumentation Property

Gets or sets whether to generate methods with extensive XML documentation\.
When enabled, generated methods include summary, parameter, and return tags\.

```csharp
public bool IncludeDocumentation { get; init; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

### Remarks
Default: `true` \(include documentation\)
Set to `false` to reduce generated code size if documentation is not needed\.