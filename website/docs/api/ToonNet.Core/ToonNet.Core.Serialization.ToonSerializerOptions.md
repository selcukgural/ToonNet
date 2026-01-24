#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization')

## ToonSerializerOptions Class

Configuration options for TOON serialization\.

```csharp
public sealed class ToonSerializerOptions : System.ComponentModel.DataAnnotations.IValidatableObject
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonSerializerOptions

Implements [System\.ComponentModel\.DataAnnotations\.IValidatableObject](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject 'System\.ComponentModel\.DataAnnotations\.IValidatableObject')

| Properties | |
| :--- | :--- |
| [AllowExtendedLimits](ToonNet.Core.Serialization.ToonSerializerOptions.AllowExtendedLimits.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.AllowExtendedLimits') | Gets or sets a value indicating whether extended limits are allowed\. When false \(default\), MaxDepth is limited to 200\. When true, MaxDepth can be set up to 1000\. |
| [Converters](ToonNet.Core.Serialization.ToonSerializerOptions.Converters.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.Converters') | Gets the collection of custom converters to use during serialization\. |
| [Default](ToonNet.Core.Serialization.ToonSerializerOptions.Default.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.Default') | Gets the default instance with standard settings\. |
| [IgnoreNullValues](ToonNet.Core.Serialization.ToonSerializerOptions.IgnoreNullValues.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.IgnoreNullValues') | Gets or sets whether to ignore null values during serialization\. The default value is false\. |
| [IncludeReadOnlyProperties](ToonNet.Core.Serialization.ToonSerializerOptions.IncludeReadOnlyProperties.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.IncludeReadOnlyProperties') | Gets or sets whether to include read\-only properties in serialization\. Default value is true\. |
| [IncludeTypeInformation](ToonNet.Core.Serialization.ToonSerializerOptions.IncludeTypeInformation.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.IncludeTypeInformation') | Gets or sets whether to include type information for polymorphic scenarios\. The default value is false\. |
| [MaxDepth](ToonNet.Core.Serialization.ToonSerializerOptions.MaxDepth.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.MaxDepth') | Gets or sets the maximum depth for serialization \(prevents circular references\)\. Must be between 1 and 200 \(or 1000 if [AllowExtendedLimits](ToonNet.Core.Serialization.ToonSerializerOptions.AllowExtendedLimits.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.AllowExtendedLimits') is true\)\. The default value is 100\. |
| [PropertyNamingPolicy](ToonNet.Core.Serialization.ToonSerializerOptions.PropertyNamingPolicy.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.PropertyNamingPolicy') | Gets or sets the property naming policy to use during serialization\. The default value is PropertyNamingPolicy\.Default\. |
| [PublicOnly](ToonNet.Core.Serialization.ToonSerializerOptions.PublicOnly.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.PublicOnly') | Gets or sets whether to serialize only public properties/fields\. Default value is true\. |
| [ToonOptions](ToonNet.Core.Serialization.ToonSerializerOptions.ToonOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.ToonOptions') | Gets or sets the options for parsing/encoding\. Cannot be null\. |

| Methods | |
| :--- | :--- |
| [AddConverter\(IToonConverter\)](ToonNet.Core.Serialization.ToonSerializerOptions.AddConverter(ToonNet.Core.Serialization.IToonConverter).md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.AddConverter\(ToonNet\.Core\.Serialization\.IToonConverter\)') | Adds a custom converter to the collection\. |
| [GetConverter\(Type\)](ToonNet.Core.Serialization.ToonSerializerOptions.GetConverter(System.Type).md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.GetConverter\(System\.Type\)') | Gets a converter for the specified type\. |
| [Validate\(ValidationContext\)](ToonNet.Core.Serialization.ToonSerializerOptions.Validate(System.ComponentModel.DataAnnotations.ValidationContext).md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions\.Validate\(System\.ComponentModel\.DataAnnotations\.ValidationContext\)') | Validates the current instance using DataAnnotations rules\. |
