#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization\.Attributes](ToonNet.Core.Serialization.Attributes.md 'ToonNet\.Core\.Serialization\.Attributes').[ToonPropertyAttribute](ToonNet.Core.Serialization.Attributes.ToonPropertyAttribute.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonPropertyAttribute')

## ToonPropertyAttribute\(string\) Constructor

Specifies a custom name for a property in TOON format\.

```csharp
public ToonPropertyAttribute(string name);
```
#### Parameters

<a name='ToonNet.Core.Serialization.Attributes.ToonPropertyAttribute.ToonPropertyAttribute(string).name'></a>

`name` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The custom name to use for the property during serialization and deserialization\.
This value cannot be null\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when the provided [name](ToonNet.Core.Serialization.Attributes.ToonPropertyAttribute.ToonPropertyAttribute(string).md#ToonNet.Core.Serialization.Attributes.ToonPropertyAttribute.ToonPropertyAttribute(string).name 'ToonNet\.Core\.Serialization\.Attributes\.ToonPropertyAttribute\.ToonPropertyAttribute\(string\)\.name') is null\.

### Remarks
This attribute allows you to define a custom name for a property or field when serialized
into TOON format, overriding the default property name\.