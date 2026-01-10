using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
/// Provides helper methods for analyzing and manipulating collection types within the context of source generation.
/// </summary>
internal static class CollectionTypeHelper
{
    /// <summary>
    /// Checks if a type is a collection type (array, List, IEnumerable, ICollection).
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>True if the specified type is a collection type; otherwise, false.</returns>
    public static bool IsCollectionType(ITypeSymbol type)
    {
        // Check if it's an array
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        // Check by name
        var name = type.Name;

        if (name is "List" or "Array" or "IEnumerable" or "ICollection" or "IList" or "HashSet" or "Queue" or "Stack")
        {
            return true;
        }

        // Check interfaces
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        foreach (var namedTypeSymbol in namedType.AllInterfaces)
        {
            if (namedTypeSymbol.Name is "IEnumerable" or "ICollection" or "IList")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the element type of a collection.
    /// </summary>
    /// <param name="type">The type symbol representing the collection type.</param>
    /// <returns>
    /// The element type of the collection if the specified type is a collection
    /// (e.g., array or generic collection); otherwise, null.
    /// </returns>
    public static ITypeSymbol? GetElementType(ITypeSymbol type)
    {
        return type switch
        {
            // Array: get an element type
            IArrayTypeSymbol arrayType => arrayType.ElementType,
            // Generic type: get first type argument
            INamedTypeSymbol { TypeArguments.Length: > 0 } namedType => namedType.TypeArguments[0],
            _                                                        => null
        };
    }

    /// <summary>
    /// Determines if a type represents a dictionary or a key-value collection.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <return>
    /// True if the given type is a dictionary or a similar key-value collection; otherwise, false.
    /// </return>
    public static bool IsDictionaryType(ITypeSymbol type)
    {
        var name = type.Name;

        if (name is "Dictionary" or "IDictionary" or "IReadOnlyDictionary" or "ConcurrentDictionary")
        {
            return true;
        }

        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        foreach (var namedTypeSymbol in namedType.AllInterfaces)
        {
            if (namedTypeSymbol.Name is "IDictionary" or "IReadOnlyDictionary")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the key and value types of a dictionary.
    /// </summary>
    /// <param name="type">
    /// The type to inspect, which is expected to be a dictionary or a key-value collection type.
    /// </param>
    /// <returns>
    /// A tuple containing the key type and value type of the dictionary. If the type does not support key-value pairs,
    /// both elements of the tuple will be null.
    /// </returns>
    public static (ITypeSymbol? KeyType, ITypeSymbol? ValueType) GetDictionaryTypes(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { TypeArguments.Length: >= 2 } namedType)
        {
            return (namedType.TypeArguments[0], namedType.TypeArguments[1]);
        }

        return (null, null);
    }

    /// <summary>
    /// Gets the syntax string used to initialize a collection based on its type.
    /// </summary>
    /// <param name="type">
    /// The symbol representing the collection type for which the initialization syntax is required.
    /// </param>
    /// <returns>
    /// A string representing the initialization syntax for the specified collection type.
    /// </returns>
    public static string GetCollectionInitializationSyntax(ITypeSymbol type)
    {
        var name = type.Name;

        return name switch
        {
            "List"                 => "new()",
            "Array"                => "new",
            "HashSet"              => "new()",
            "Queue"                => "new()",
            "Stack"                => "new()",
            "Dictionary"           => "new()",
            "ConcurrentDictionary" => "new()",
            _                      => "new()"
        };
    }
}