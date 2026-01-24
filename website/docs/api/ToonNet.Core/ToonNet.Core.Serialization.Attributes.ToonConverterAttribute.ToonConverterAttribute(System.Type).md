#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization\.Attributes](ToonNet.Core.Serialization.Attributes.md 'ToonNet\.Core\.Serialization\.Attributes').[ToonConverterAttribute](ToonNet.Core.Serialization.Attributes.ToonConverterAttribute.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonConverterAttribute')

## ToonConverterAttribute\(Type\) Constructor

Specifies a custom converter for a property or type\.

```csharp
public ToonConverterAttribute(System.Type converterType);
```
#### Parameters

<a name='ToonNet.Core.Serialization.Attributes.ToonConverterAttribute.ToonConverterAttribute(System.Type).converterType'></a>

`converterType` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The type of the custom converter to use for serialization and deserialization\.
This value cannot be null and must implement the required converter interface\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the provided [converterType](ToonNet.Core.Serialization.Attributes.ToonConverterAttribute.ToonConverterAttribute(System.Type).md#ToonNet.Core.Serialization.Attributes.ToonConverterAttribute.ToonConverterAttribute(System.Type).converterType 'ToonNet\.Core\.Serialization\.Attributes\.ToonConverterAttribute\.ToonConverterAttribute\(System\.Type\)\.converterType') is null\.

### Remarks
This attribute allows you to define a custom converter for a property, field, class, or struct,
enabling custom serialization and deserialization logic\.