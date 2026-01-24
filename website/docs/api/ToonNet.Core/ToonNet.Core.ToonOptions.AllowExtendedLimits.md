#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core').[ToonOptions](ToonNet.Core.ToonOptions.md 'ToonNet\.Core\.ToonOptions')

## ToonOptions\.AllowExtendedLimits Property

Gets or sets a value indicating whether extended limits are allowed\.
When false \(default\), MaxDepth is limited to 200\.
When true, MaxDepth can be set up to 1000\.

```csharp
public bool AllowExtendedLimits { get; set; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

### Remarks
Enable this only when you need to process deeply nested structures\.
Extended limits may increase memory usage and stack depth\.