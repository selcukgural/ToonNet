#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core')

## ToonOptions Class

Configuration options for TOON parsing and encoding\.

```csharp
public sealed class ToonOptions : System.ComponentModel.DataAnnotations.IValidatableObject
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonOptions

Implements [System\.ComponentModel\.DataAnnotations\.IValidatableObject](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject 'System\.ComponentModel\.DataAnnotations\.IValidatableObject')

| Properties | |
| :--- | :--- |
| [AllowExtendedLimits](ToonNet.Core.ToonOptions.AllowExtendedLimits.md 'ToonNet\.Core\.ToonOptions\.AllowExtendedLimits') | Gets or sets a value indicating whether extended limits are allowed\. When false \(default\), MaxDepth is limited to 200\. When true, MaxDepth can be set up to 1000\. |
| [Default](ToonNet.Core.ToonOptions.Default.md 'ToonNet\.Core\.ToonOptions\.Default') | Gets the default instance of [ToonOptions](ToonNet.Core.ToonOptions.md 'ToonNet\.Core\.ToonOptions') with standard settings\. |
| [Delimiter](ToonNet.Core.ToonOptions.Delimiter.md 'ToonNet\.Core\.ToonOptions\.Delimiter') | Gets or sets the delimiter character for array values\. Cannot be whitespace, newline, tab, or control characters\. The default value is comma \(','\)\. |
| [IndentSize](ToonNet.Core.ToonOptions.IndentSize.md 'ToonNet\.Core\.ToonOptions\.IndentSize') | Gets or sets the number of spaces per indentation level\. Must be an even number between 2 and 100 per TOON specification §12\. The default value is 2\. |
| [MaxDepth](ToonNet.Core.ToonOptions.MaxDepth.md 'ToonNet\.Core\.ToonOptions\.MaxDepth') | Gets or sets the maximum nesting depth allowed during parsing\. Must be between 1 and 200 \(or 1000 if [AllowExtendedLimits](ToonNet.Core.ToonOptions.AllowExtendedLimits.md 'ToonNet\.Core\.ToonOptions\.AllowExtendedLimits') is true\)\. The default value is 100\. |
| [StrictMode](ToonNet.Core.ToonOptions.StrictMode.md 'ToonNet\.Core\.ToonOptions\.StrictMode') | Gets or sets a value indicating whether strict parsing mode is enabled\. Default value is true\. In strict mode, invalid documents throw exceptions\. |

| Methods | |
| :--- | :--- |
| [Validate\(ValidationContext\)](ToonNet.Core.ToonOptions.Validate(System.ComponentModel.DataAnnotations.ValidationContext).md 'ToonNet\.Core\.ToonOptions\.Validate\(System\.ComponentModel\.DataAnnotations\.ValidationContext\)') | Validates the current instance using DataAnnotations rules\. |
