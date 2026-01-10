namespace ToonNet.Core.Serialization.Attributes;

/// <summary>
/// Marks a property to be ignored during serialization/deserialization.
/// </summary>
/// <remarks>
/// This attribute can be applied to properties or fields to exclude them from the serialization
/// and deserialization process when working with TOON format.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ToonIgnoreAttribute : Attribute;

/// <summary>
/// Specifies a custom name for a property in TOON format.
/// </summary>
/// <param name="name">
/// The custom name to use for the property during serialization and deserialization.
/// This value cannot be null.
/// </param>
/// <exception cref="ArgumentNullException">
/// Thrown when the provided <paramref name="name"/> is null.
/// </exception>
/// <remarks>
/// This attribute allows you to define a custom name for a property or field when serialized
/// into TOON format, overriding the default property name.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ToonPropertyAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the custom name specified for the property.
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
}

/// <summary>
/// Specifies the order of properties during serialization.
/// </summary>
/// <param name="order">
/// The order value that determines the position of the property during serialization.
/// </param>
/// <remarks>
/// This attribute can be applied to properties or fields to control their order in the serialized output.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ToonPropertyOrderAttribute(int order) : Attribute
{
    /// <summary>
    /// Gets the order value specified for the property.
    /// </summary>
    public int Order { get; } = order;
}

/// <summary>
/// Specifies a custom converter for a property or type.
/// </summary>
/// <param name="converterType">
/// The type of the custom converter to use for serialization and deserialization.
/// This value cannot be null and must implement the required converter interface.
/// </param>
/// <exception cref="ArgumentNullException">
/// Thrown when the provided <paramref name="converterType"/> is null.
/// </exception>
/// <remarks>
/// This attribute allows you to define a custom converter for a property, field, class, or struct,
/// enabling custom serialization and deserialization logic.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ToonConverterAttribute(Type converterType) : Attribute
{
    /// <summary>
    /// Gets the type of the custom converter specified for the property or type.
    /// </summary>
    public Type ConverterType { get; } = converterType ?? throw new ArgumentNullException(nameof(converterType));
}

/// <summary>
/// Marks a constructor to be used for deserialization.
/// </summary>
/// <remarks>
/// This attribute can be applied to a constructor to indicate that it should be used during the deserialization process.
/// </remarks>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class ToonConstructorAttribute : Attribute;