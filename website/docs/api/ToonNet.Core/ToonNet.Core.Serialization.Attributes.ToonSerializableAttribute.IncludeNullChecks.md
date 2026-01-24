#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization\.Attributes](ToonNet.Core.Serialization.Attributes.md 'ToonNet\.Core\.Serialization\.Attributes').[ToonSerializableAttribute](ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonSerializableAttribute')

## ToonSerializableAttribute\.IncludeNullChecks Property

Gets or sets whether to include null\-check guards in generated code\.
When enabled, the generator includes ArgumentNullException checks for non\-nullable properties\.

```csharp
public bool IncludeNullChecks { get; init; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

### Remarks
Default: `true` \(include null checks\)
Set to `false` for performance\-critical scenarios where null checking
is handled elsewhere, or for maximum code size reduction\.