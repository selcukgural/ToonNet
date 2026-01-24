#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

## ToonSerializerOptions\.Validate\(ValidationContext\) Method

Validates the current instance using DataAnnotations rules\.

```csharp
public System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializerOptions.Validate(System.ComponentModel.DataAnnotations.ValidationContext).validationContext'></a>

`validationContext` [System\.ComponentModel\.DataAnnotations\.ValidationContext](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationcontext 'System\.ComponentModel\.DataAnnotations\.ValidationContext')

The validation context\.

Implements [Validate\(ValidationContext\)](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject.validate#system-componentmodel-dataannotations-ivalidatableobject-validate(system-componentmodel-dataannotations-validationcontext) 'System\.ComponentModel\.DataAnnotations\.IValidatableObject\.Validate\(System\.ComponentModel\.DataAnnotations\.ValidationContext\)')

#### Returns
[System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[System\.ComponentModel\.DataAnnotations\.ValidationResult](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationresult 'System\.ComponentModel\.DataAnnotations\.ValidationResult')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')  
A sequence of validation results\.