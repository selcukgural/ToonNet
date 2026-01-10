using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Analysis;

/// <summary>
/// Metadata about a property that will be serialized.
/// </summary>
internal sealed record PropertyInfo
{
    /// <summary>
    /// The property name as declared in the C# class.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The Roslyn symbol for the property.
    /// </summary>
    public required IPropertySymbol Symbol { get; init; }

    /// <summary>
    /// The type of the property.
    /// </summary>
    public required ITypeSymbol Type { get; init; }

    /// <summary>
    /// Custom name from [ToonProperty("name")] attribute, if specified.
    /// If null, use <see cref="Name"/> or apply naming policy.
    /// </summary>
    public string? CustomName { get; init; }

    /// <summary>
    /// Order from [ToonPropertyOrder(order)] attribute.
    /// Higher values are serialized later.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Custom converter type from [ToonConverter(typeof(...))] attribute.
    /// If specified, this converter is used instead of default serialization.
    /// </summary>
    public ITypeSymbol? CustomConverter { get; init; }

    /// <summary>
    /// Gets the name to use when serializing (applies custom name if set).
    /// </summary>
    public string SerializedName => CustomName ?? Name;

    /// <summary>
    /// Gets a value indicating whether this property should be ignored.
    /// </summary>
    public bool IsIgnored { get; init; }

    /// <summary>
    /// Gets a value indicating whether the property has a getter.
    /// </summary>
    public bool HasGetter => Symbol.GetMethod is not null;

    /// <summary>
    /// Gets a value indicating whether the property has a setter.
    /// </summary>
    public bool HasSetter => Symbol.SetMethod is not null;

    /// <summary>
    /// Gets a value indicating whether a custom converter is available.
    /// </summary>
    public bool HasCustomConverter => CustomConverter is not null;
}
