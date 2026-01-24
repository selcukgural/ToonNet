#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core').[ToonOptions](ToonNet.Core.ToonOptions.md 'ToonNet\.Core\.ToonOptions')

## ToonOptions\.Delimiter Property

Gets or sets the delimiter character for array values\.
Cannot be whitespace, newline, tab, or control characters\.
The default value is comma \(','\)\.

```csharp
public char Delimiter { get; set; }
```

#### Property Value
[System\.Char](https://learn.microsoft.com/en-us/dotnet/api/system.char 'System\.Char')

#### Exceptions

[System\.ArgumentException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception 'System\.ArgumentException')  
Thrown when the value is a whitespace character, newline, tab, or control character\.

### Remarks
Per TOON specification §11, delimiters must be printable non\-whitespace characters\.