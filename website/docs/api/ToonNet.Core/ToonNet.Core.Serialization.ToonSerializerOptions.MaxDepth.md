#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

## ToonSerializerOptions\.MaxDepth Property

Gets or sets the maximum depth for serialization \(prevents circular references\)\.
Must be between 1 and 200 \(or 1000 if [AllowExtendedLimits](ToonNet.Core.Serialization.ToonSerializerOptions.AllowExtendedLimits.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.AllowExtendedLimits') is true\)\.
The default value is 100\.

```csharp
public int MaxDepth { get; set; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

#### Exceptions

[System\.ArgumentOutOfRangeException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception 'System\.ArgumentOutOfRangeException')  
Thrown when the value is less than 1 or exceeds the allowed maximum
\(200 by default, or 1000 with [AllowExtendedLimits](ToonNet.Core.Serialization.ToonSerializerOptions.AllowExtendedLimits.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.AllowExtendedLimits')\)\.

### Remarks
This limit prevents infinite recursion from circular references and stack overflow\.
TOON specification §15 recommends 100 for security considerations\.
Standard limit is 200\. Enable [AllowExtendedLimits](ToonNet.Core.Serialization.ToonSerializerOptions.AllowExtendedLimits.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.AllowExtendedLimits') to allow up to 1000\.