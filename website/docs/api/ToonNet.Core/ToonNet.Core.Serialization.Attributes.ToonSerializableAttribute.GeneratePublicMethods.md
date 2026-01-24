#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization\.Attributes](ToonNet.Core.Serialization.Attributes.md 'ToonNet\.Core\.Serialization\.Attributes').[ToonSerializableAttribute](ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonSerializableAttribute')

## ToonSerializableAttribute\.GeneratePublicMethods Property

Gets or sets whether to generate public static Serialize/Deserialize methods\.
If false, methods will be internal \(useful for source generation testing\)\.

```csharp
public bool GeneratePublicMethods { get; init; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

### Remarks
Default: `true` \(public methods\)