#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core').[ToonOptions](ToonNet.Core.ToonOptions.md 'ToonNet\.Core\.ToonOptions')

## ToonOptions\.IndentSize Property

Gets or sets the number of spaces per indentation level\.
Must be an even number between 2 and 100 per TOON specification §12\.
The default value is 2\.

```csharp
public int IndentSize { get; set; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

#### Exceptions

[System\.ArgumentOutOfRangeException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception 'System\.ArgumentOutOfRangeException')  
Thrown when the value is less than 2, greater than 100, or not an even number\.

### Remarks
TOON specification §12 requires indentation to be a multiple of 2 spaces\.
The recommended value is 2 for consistency with the specification\.