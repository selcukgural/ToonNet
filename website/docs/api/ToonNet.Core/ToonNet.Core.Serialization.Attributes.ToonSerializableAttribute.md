#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization\.Attributes](ToonNet.Core.Serialization.Attributes.md 'ToonNet\.Core\.Serialization\.Attributes')

## ToonSerializableAttribute Class

Marks a class for automatic TOON serialization code generation via source generator\.
The source generator will create Serialize and Deserialize methods at compile\-time,
eliminating reflection overhead and enabling AOT\-ready deployments\.

```csharp
public sealed class ToonSerializableAttribute : System.Attribute
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [System\.Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.attribute 'System\.Attribute') &#129106; ToonSerializableAttribute

### Remarks

The attributed class MUST be declared as `partial` to allow the source generator
to inject generated code.

Usage example:

```csharp
[ToonSerializable]
public partial class User
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// Generated methods become available:
var user = new User { Name = "Alice", Age = 30 };
var doc = User.Serialize(user);
var deserialized = User.Deserialize(doc);
```

Generated methods are static and follow this pattern:

```csharp
public static ToonDocument Serialize(T value, ToonSerializerOptions? options = null)
public static T Deserialize(ToonDocument doc, ToonSerializerOptions? options = null)
```

Performance benefits:
- 3-5x faster than reflection-based serialization
- Zero allocation in hot paths
- Full compile-time type safety
- Native AOT compatible

| Properties | |
| :--- | :--- |
| [GeneratePublicMethods](ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute.GeneratePublicMethods.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonSerializableAttribute\.GeneratePublicMethods') | Gets or sets whether to generate public static Serialize/Deserialize methods\. If false, methods will be internal \(useful for source generation testing\)\. |
| [IncludeDocumentation](ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute.IncludeDocumentation.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonSerializableAttribute\.IncludeDocumentation') | Gets or sets whether to generate methods with extensive XML documentation\. When enabled, generated methods include summary, parameter, and return tags\. |
| [IncludeNullChecks](ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute.IncludeNullChecks.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonSerializableAttribute\.IncludeNullChecks') | Gets or sets whether to include null\-check guards in generated code\. When enabled, the generator includes ArgumentNullException checks for non\-nullable properties\. |
| [NamingPolicy](ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute.NamingPolicy.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonSerializableAttribute\.NamingPolicy') | Gets or sets the property naming policy for generated serialization code\. This policy is applied to all properties but can be overridden per\-property using the \[ToonProperty\(name\)\] attribute\. |
