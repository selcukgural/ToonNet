#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization\.Attributes](ToonNet.Core.Serialization.Attributes.md 'ToonNet\.Core\.Serialization\.Attributes')

## ToonConverterAttribute Class

Specifies a custom converter for a property or type\.

```csharp
public sealed class ToonConverterAttribute : System.Attribute
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [System\.Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.attribute 'System\.Attribute') &#129106; ToonConverterAttribute

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the provided converterType is null\.

### Remarks
This attribute allows you to define a custom converter for a property, field, class, or struct,
enabling custom serialization and deserialization logic\.

| Constructors | |
| :--- | :--- |
| [ToonConverterAttribute\(Type\)](ToonNet.Core.Serialization.Attributes.ToonConverterAttribute.ToonConverterAttribute(System.Type).md 'ToonNet\.Core\.Serialization\.Attributes\.ToonConverterAttribute\.ToonConverterAttribute\(System\.Type\)') | Specifies a custom converter for a property or type\. |

| Properties | |
| :--- | :--- |
| [ConverterType](ToonNet.Core.Serialization.Attributes.ToonConverterAttribute.ConverterType.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonConverterAttribute\.ConverterType') | Gets the type of the custom converter specified for the property or type\. |
