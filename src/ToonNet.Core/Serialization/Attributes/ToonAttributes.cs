namespace ToonNet.Core.Serialization.Attributes;

/// <summary>
/// Marks a property to be ignored during serialization/deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ToonIgnoreAttribute : Attribute;

/// <summary>
/// Specifies a custom name for a property in TOON format.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ToonPropertyAttribute(string name) : Attribute
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
}

/// <summary>
/// Specifies the order of properties during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ToonPropertyOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}

/// <summary>
/// Specifies a custom converter for a property or type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ToonConverterAttribute(Type converterType) : Attribute
{
    public Type ConverterType { get; } = converterType ?? throw new ArgumentNullException(nameof(converterType));
}

/// <summary>
/// Marks a constructor to be used for deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class ToonConstructorAttribute : Attribute;
