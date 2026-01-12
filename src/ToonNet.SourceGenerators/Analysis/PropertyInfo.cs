using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Analysis;

/// <summary>
/// Encapsulates metadata about a property for serialization purposes.
/// </summary>
internal sealed record PropertyInfo
{
    /// <summary>
    /// The name of the property in the context of serialization metadata.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The symbol representation of the property in the Roslyn compilation model.
    /// </summary>
    public required IPropertySymbol Symbol { get; init; }

    /// <summary>
    /// Represents the type of the property as described by its symbol.
    /// </summary>
    public required ITypeSymbol Type { get; init; }

    /// <summary>
    /// A custom name for the property as it will appear in serialized output.
    /// If set, this value will override the default property name during serialization.
    /// </summary>
    public string? CustomName { get; init; }

    /// <summary>
    /// The order in which the property should be serialized or processed.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Specifies a custom converter to handle the serialization and deserialization
    /// of the property. If this property is set, the specified converter will be
    /// used instead of the default serialization logic.
    /// </summary>
    public ITypeSymbol? CustomConverter { get; init; }

    /// <summary>
    /// Indicates whether the property is associated with a nested type marked with
    /// the ToonSerializable attribute, implying that the type of the property
    /// should also undergo serialization and deserialization as a nested object.
    /// </summary>
    public bool IsNestedSerializable { get; init; }

    /// <summary>
    /// The serialized name of the property, which is either the custom name
    /// specified by the user or, if not provided, the property name as declared
    /// in the C# class.
    /// </summary>
    public string SerializedName => CustomName ?? Name;

    /// <summary>
    /// Indicates whether the property is marked as ignored for serialization purposes.
    /// </summary>
    public bool IsIgnored { get; init; }

    /// <summary>
    /// Indicates whether the property has a getter defined.
    /// </summary>
    public bool HasGetter => Symbol.GetMethod is not null;

    /// <summary>
    /// Indicates whether the property has a setter method defined.
    /// </summary>
    public bool HasSetter => Symbol.SetMethod is not null;

    /// <summary>
    /// Indicates whether the property has a custom converter defined for serialization and deserialization.
    /// </summary>
    public bool HasCustomConverter => CustomConverter is not null;
}