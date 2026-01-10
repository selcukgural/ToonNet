using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Analysis;

/// <summary>
///     Metadata about a class marked with [ToonSerializable].
/// </summary>
internal sealed record ClassInfo
{
    /// <summary>
    ///     The class name (without namespace).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     The namespace containing the class.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    ///     The Roslyn symbol for the class.
    /// </summary>
    public required INamedTypeSymbol Symbol { get; init; }

    /// <summary>
    ///     All serializable properties in the class (in order).
    /// </summary>
    public required ImmutableArray<PropertyInfo> Properties { get; init; }

    /// <summary>
    ///     The [ToonSerializable] attribute data.
    /// </summary>
    public required AttributeData? Attribute { get; init; }

    /// <summary>
    ///     Custom constructor marked with [ToonConstructor] (if any).
    ///     If specified, this constructor is called during deserialization.
    /// </summary>
    public IMethodSymbol? CustomConstructor { get; init; }

    /// <summary>
    ///     Gets the fully qualified class name.
    /// </summary>
    public string FullName => $"{Namespace}.{Name}";

    /// <summary>
    ///     Gets a value indicating whether the class is declared as partial.
    /// </summary>
    public bool IsPartial { get; init; }

    /// <summary>
    ///     Gets whether to generate public methods (vs internal).
    /// </summary>
    public bool GeneratePublicMethods => GetBooleanAttribute("GeneratePublicMethods", true);

    /// <summary>
    ///     Gets whether to include null-check guards in generated code.
    /// </summary>
    public bool IncludeNullChecks => GetBooleanAttribute("IncludeNullChecks", true);

    /// <summary>
    ///     Gets whether to include XML documentation in generated code.
    /// </summary>
    public bool IncludeDocumentation => GetBooleanAttribute("IncludeDocumentation", true);

    /// <summary>
    ///     Gets a value indicating whether a custom constructor is available.
    /// </summary>
    public bool HasCustomConstructor => CustomConstructor is not null;

    /// <summary>
    ///     Extracts a boolean attribute value from [ToonSerializable].
    /// </summary>
    private bool GetBooleanAttribute(string attributeName, bool defaultValue)
    {
        if (Attribute is null)
        {
            return defaultValue;
        }

        foreach (var namedArg in Attribute.NamedArguments)
        {
            if (namedArg.Key == attributeName && namedArg.Value.Value is bool value)
            {
                return value;
            }
        }

        return defaultValue;
    }
}