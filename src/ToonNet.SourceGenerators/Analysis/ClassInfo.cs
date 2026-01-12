using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Analysis;

/// <summary>
/// Encapsulates metadata about a specific class annotated with the [ToonSerializable] attribute.
/// </summary>
internal sealed record ClassInfo
{
    /// <summary>
    /// The name of the class, used to represent its identifier without the namespace.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The namespace in which the class is defined.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// The symbol representing the class marked with [ToonSerializable].
    /// </summary>
    public required INamedTypeSymbol Symbol { get; init; }

    /// <summary>
    /// A collection of properties associated with the class.
    /// </summary>
    public required ImmutableArray<PropertyInfo> Properties { get; init; }

    /// <summary>
    /// Represents metadata about a specific attribute applied to a class.
    /// </summary>
    public required AttributeData? Attribute { get; init; }

    /// <summary>
    /// Represents a user-defined constructor for the class.
    /// </summary>
    public IMethodSymbol? CustomConstructor { get; init; }

    /// <summary>
    /// The fully qualified name of the class, combining the namespace and class name.
    /// </summary>
    public string FullName => $"{Namespace}.{Name}";

    /// <summary>
    /// Indicates whether the class is declared as a partial class.
    /// </summary>
    public bool IsPartial { get; init; }

    /// <summary>
    /// Indicates whether public methods should be generated for the class.
    /// Determines the accessibility of the generated methods, toggling between
    /// public and internal based on its value.
    /// </summary>
    public bool GeneratePublicMethods => GetBooleanAttribute("GeneratePublicMethods", true);

    /// <summary>
    /// Indicates whether null checks should be performed during serialization
    /// and deserialization processes.
    /// </summary>
    public bool IncludeNullChecks => GetBooleanAttribute("IncludeNullChecks", true);

    /// <summary>
    /// Specifies whether XML documentation should be generated for methods and properties
    /// in the associated class during code generation.
    /// </summary>
    public bool IncludeDocumentation => GetBooleanAttribute("IncludeDocumentation", true);

    /// <summary>
    /// Indicates whether the class has a custom constructor defined.
    /// </summary>
    public bool HasCustomConstructor => CustomConstructor is not null;

    /// <summary>
    /// Retrieves a boolean attribute value based on the provided attribute name within [ToonSerializable].
    /// </summary>
    /// <param name="attributeName">
    /// The name of the attribute for which the boolean value should be retrieved.
    /// </param>
    /// <param name="defaultValue">
    /// The default value that will be returned if the attribute is not found or cannot be resolved.
    /// </param>
    /// <returns>
    /// The boolean value of the specified attribute, or the default value if the attribute is not present.
    /// </returns>
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