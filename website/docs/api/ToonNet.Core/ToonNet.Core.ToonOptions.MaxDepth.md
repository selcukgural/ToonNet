#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core').[ToonOptions](ToonNet.Core.ToonOptions.md 'ToonNet\.Core\.ToonOptions')

## ToonOptions\.MaxDepth Property

Gets or sets the maximum nesting depth allowed during parsing\.
Must be between 1 and 200 \(or 1000 if [AllowExtendedLimits](ToonNet.Core.ToonOptions.AllowExtendedLimits.md 'ToonNet\.Core\.ToonOptions\.AllowExtendedLimits') is true\)\.
The default value is 100\.

```csharp
public int MaxDepth { get; set; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

#### Exceptions

[System\.ArgumentOutOfRangeException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception 'System\.ArgumentOutOfRangeException')  
Thrown when the value is less than 1 or exceeds the allowed maximum
\(200 by default, or 1000 with [AllowExtendedLimits](ToonNet.Core.ToonOptions.AllowExtendedLimits.md 'ToonNet\.Core\.ToonOptions\.AllowExtendedLimits')\)\.

### Remarks
TOON specification §15 recommends 100 as default for security\.
Standard limit is 200\. Enable [AllowExtendedLimits](ToonNet.Core.ToonOptions.AllowExtendedLimits.md 'ToonNet\.Core\.ToonOptions\.AllowExtendedLimits') to allow up to 1000\.
Very high values may cause stack overflow issues\.