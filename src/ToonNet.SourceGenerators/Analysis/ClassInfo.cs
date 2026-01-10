using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace ToonNet.SourceGenerators.Analysis;

/// <summary>
/// Metadata about a class marked with [ToonSerializable].
/// </summary>
internal sealed record ClassInfo
{
    /// <summary>
    /// The class name (without namespace).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The namespace containing the class.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// The Roslyn symbol for the class.
    /// </summary>
    public required INamedTypeSymbol Symbol { get; init; }

    /// <summary>
    /// All serializable properties in the class (in order).
    /// </summary>
    public required ImmutableArray<PropertyInfo> Properties { get; init; }

    /// <summary>
    /// The [ToonSerializable] attribute data.
    /// </summary>
    public required AttributeData? Attribute { get; init; }

    /// <summary>
    /// Gets the fully qualified class name.
    /// </summary>
    public string FullName => $"{Namespace}.{Name}";

    /// <summary>
    /// Gets a value indicating whether the class is declared as partial.
    /// </summary>
    public bool IsPartial { get; init; }
}
