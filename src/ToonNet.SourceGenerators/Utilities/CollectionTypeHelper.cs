using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
/// Helper utilities for detecting and handling collection types during code generation.
/// </summary>
internal static class CollectionTypeHelper
{
    /// <summary>
    /// Checks if a type is a collection type (array, List, IEnumerable, ICollection).
    /// </summary>
    public static bool IsCollectionType(ITypeSymbol type)
    {
        // Check if it's an array
        if (type is IArrayTypeSymbol)
            return true;

        // Check by name
        var name = type.Name;
        if (name is "List" or "Array" or "IEnumerable" or "ICollection" or "IList" or "HashSet" or "Queue" or "Stack")
            return true;

        // Check interfaces
        if (type is INamedTypeSymbol namedType)
        {
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.Name is "IEnumerable" or "ICollection" or "IList")
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the element type of a collection.
    /// </summary>
    public static ITypeSymbol? GetElementType(ITypeSymbol type)
    {
        // Array: get element type
        if (type is IArrayTypeSymbol arrayType)
            return arrayType.ElementType;

        // Generic type: get first type argument
        if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
            return namedType.TypeArguments[0];

        return null;
    }

    /// <summary>
    /// Checks if a type represents a dictionary or key-value collection.
    /// </summary>
    public static bool IsDictionaryType(ITypeSymbol type)
    {
        var name = type.Name;
        if (name is "Dictionary" or "IDictionary" or "IReadOnlyDictionary" or "ConcurrentDictionary")
            return true;

        if (type is INamedTypeSymbol namedType)
        {
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.Name is "IDictionary" or "IReadOnlyDictionary")
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the key and value types of a dictionary.
    /// </summary>
    public static (ITypeSymbol? KeyType, ITypeSymbol? ValueType) GetDictionaryTypes(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length >= 2)
            return (namedType.TypeArguments[0], namedType.TypeArguments[1]);

        return (null, null);
    }

    /// <summary>
    /// Gets the collection initialization syntax for a given type.
    /// </summary>
    public static string GetCollectionInitializationSyntax(ITypeSymbol type)
    {
        var name = type.Name;

        return name switch
        {
            "List" => "new()",
            "Array" => "new",
            "HashSet" => "new()",
            "Queue" => "new()",
            "Stack" => "new()",
            "Dictionary" => "new()",
            "ConcurrentDictionary" => "new()",
            _ => "new()"
        };
    }
}
