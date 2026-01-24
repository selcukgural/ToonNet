#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization\.Attributes](ToonNet.Core.Serialization.Attributes.md 'ToonNet\.Core\.Serialization\.Attributes')

## ToonPropertyAttribute Class

Specifies a custom name for a property in TOON format\.

```csharp
public sealed class ToonPropertyAttribute : System.Attribute
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [System\.Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.attribute 'System\.Attribute') &#129106; ToonPropertyAttribute

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the provided name is null\.

### Remarks
This attribute allows you to define a custom name for a property or field when serialized
into TOON format, overriding the default property name\.

| Constructors | |
| :--- | :--- |
| [ToonPropertyAttribute\(string\)](ToonNet.Core.Serialization.Attributes.ToonPropertyAttribute.ToonPropertyAttribute(string).md 'ToonNet\.Core\.Serialization\.Attributes\.ToonPropertyAttribute\.ToonPropertyAttribute\(string\)') | Specifies a custom name for a property in TOON format\. |

| Properties | |
| :--- | :--- |
| [Name](ToonNet.Core.Serialization.Attributes.ToonPropertyAttribute.Name.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonPropertyAttribute\.Name') | Gets the custom name specified for the property\. |
