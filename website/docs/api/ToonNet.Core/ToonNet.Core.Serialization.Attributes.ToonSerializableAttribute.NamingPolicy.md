#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization\.Attributes](ToonNet.Core.Serialization.Attributes.md 'ToonNet\.Core\.Serialization\.Attributes').[ToonSerializableAttribute](ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonSerializableAttribute')

## ToonSerializableAttribute\.NamingPolicy Property

Gets or sets the property naming policy for generated serialization code\.
This policy is applied to all properties but can be overridden per\-property
using the \[ToonProperty\(name\)\] attribute\.

```csharp
public ToonNet.Core.Serialization.PropertyNamingPolicy NamingPolicy { get; init; }
```

#### Property Value
[PropertyNamingPolicy](ToonNet.Core.Serialization.PropertyNamingPolicy.md 'ToonNet\.Core\.Serialization\.PropertyNamingPolicy')

### Remarks
Default: [Default](ToonNet.Core.Serialization.PropertyNamingPolicy.md#ToonNet.Core.Serialization.PropertyNamingPolicy.Default 'ToonNet\.Core\.Serialization\.PropertyNamingPolicy\.Default') \(property names as\-is\)
Examples:

```csharp
[ToonSerializable(NamingPolicy = PropertyNamingPolicy.CamelCase)]
public partial class User
{
    public string FirstName { get; set; }  // Serializes as "firstName"
}
```