namespace ToonNet.Core.Serialization.Attributes;

/// <summary>
/// Marks a property to be ignored during serialization/deserialization.
/// </summary>
/// <remarks>
/// This attribute can be applied to properties to exclude them from the serialization
/// and deserialization process when working with TOON format.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ToonIgnoreAttribute : Attribute;

/// <summary>
/// Lets reading set a property through its non-public setter (<c>private set</c>, <c>protected set</c>,
/// <c>internal set</c>), as System.Text.Json's <c>[JsonInclude]</c> does.
/// </summary>
/// <remarks>
/// Without it, reading sets only public setters (<c>set</c> and <c>init</c>): a non-public setter usually means the
/// property is not for callers to set, and a document from a client must not set what the code keeps for itself
/// (<c>IsAdmin { get; private set; }</c>). A property with only a non-public setter is then read-only, as for
/// <see cref="ToonSerializerOptions.IncludeReadOnlyProperties"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ToonIncludeAttribute : Attribute;

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
/// This attribute allows you to define a custom name for a property when serialized
/// into TOON format, overriding the default property name.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
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
/// This attribute can be applied to properties to control their order in the serialized output.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
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
/// This attribute allows you to define a custom converter for a property, class, or struct,
/// enabling custom serialization and deserialization logic.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
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
/// The constructor may be non-public. Without the attribute, the public parameterless constructor is used, otherwise the
/// public constructor with the most parameters.
/// </remarks>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class ToonConstructorAttribute : Attribute;