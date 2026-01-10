using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
/// Helper utilities for detecting and categorizing types during code generation.
/// </summary>
internal static class TypeHelper
{
    /// <summary>
    /// Checks if a type is a simple/primitive type that can be directly serialized.
    /// </summary>
    /// <param name="type">
    /// The <see cref="ITypeSymbol"/> representing the type to check.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the type is considered a simple/primitive type; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsSimpleType(ITypeSymbol type)
    {
        var name = type.ToDisplayString();

        return name is "string" or "string?" or "int" or "long" or "double" or "decimal" or "bool" or "float" or "byte" or "short" or "uint"
                       or "ulong" or "System.String" or "System.Int32" or "System.Int64" or "System.Double" or "System.Decimal" or "System.Boolean"
                       or "System.Single" or "System.Byte" or "System.Int16" or "System.UInt32" or "System.UInt64";
    }

    /// <summary>
    /// Checks if a type name represents a numeric type.
    /// </summary>
    /// <param name="typeName">
    /// The fully qualified or simple name of the type to check.
    /// </param>
    /// <returns>
    /// True if the type name corresponds to a numeric type, otherwise false.
    /// </returns>
    public static bool IsNumericType(string typeName)
    {
        return typeName is "int" or "long" or "double" or "decimal" or "float" or "byte" or "short" or "uint" or "ulong" or "System.Int32"
                           or "System.Int64" or "System.Double" or "System.Decimal" or "System.Single" or "System.Byte" or "System.Int16"
                           or "System.UInt32" or "System.UInt64";
    }

    /// <summary>
    /// Checks if a type is a numeric type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    /// <c>true</c> if the specified type is a numeric type; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNumericType(ITypeSymbol type)
    {
        return IsNumericType(type.ToDisplayString());
    }

    /// <summary>
    /// Checks if a type is nullable (T?).
    /// </summary>
    /// <param name="type">
    /// The type symbol to check for nullability.
    /// </param>
    /// <returns>
    /// True if the type is nullable, otherwise false.
    /// </returns>
    public static bool IsNullableType(ITypeSymbol type)
    {
        // Check if it's System.Nullable<T>
        if (type is INamedTypeSymbol namedType && namedType.OriginalDefinition.ToString() == "System.Nullable<T>")
        {
            return true;
        }

        // Check if it's a value type with nullable annotation (C# 8+)
        return type is { IsValueType: true, NullableAnnotation: NullableAnnotation.Annotated };
    }

    /// <summary>
    /// Gets the underlying type of nullable type.
    /// </summary>
    /// <param name="type">
    /// The type to check for its underlying non-nullable type.
    /// </param>
    /// <returns>
    /// The underlying non-nullable type if the specified type is nullable; otherwise, null.
    /// </returns>
    public static ITypeSymbol? GetUnderlyingNullableType(ITypeSymbol type)
    {
        if (IsNullableType(type) && type is INamedTypeSymbol { TypeArguments.Length: > 0 } namedType)
        {
            return namedType.TypeArguments[0];
        }

        return null;
    }

    /// <summary>
    /// Checks if a type is a string.
    /// </summary>
    /// <param name="type">
    /// The type symbol representing the type to check.
    /// </param>
    /// <returns>
    /// True if the type represents a string; otherwise, false.
    /// </returns>
    public static bool IsStringType(ITypeSymbol type)
    {
        var name = type.ToDisplayString();
        return name is "string" or "System.String";
    }

    /// <summary>
    /// Checks if a type is a boolean.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a boolean; otherwise, false.</returns>
    public static bool IsBooleanType(ITypeSymbol type)
    {
        var name = type.ToDisplayString();
        return name is "bool" or "System.Boolean";
    }

    /// <summary>
    /// Gets the type name with proper escaping for code generation.
    /// </summary>
    /// <param name="type">
    /// The type symbol representing the type whose name is to be retrieved and escaped.
    /// </param>
    /// <returns>
    /// The fully qualified type name prefixed with "global::" to ensure proper namespace usage in generated code.
    /// </returns>
    public static string GetSafeTypeName(ITypeSymbol type)
    {
        var name = type.ToDisplayString();

        // Use global:: prefix to avoid namespace conflicts
        return name.StartsWith("System.", StringComparison.Ordinal) ? $"global::{name}" : $"global::ToonNet.Core.Models.{name}";
    }

    /// <summary>
    /// Checks if a type is a reference type.
    /// </summary>
    /// <param name="type">The type symbol to be checked.</param>
    /// <returns>True if the type is a reference type; otherwise, false.</returns>
    public static bool IsReferenceType(ITypeSymbol type)
    {
        return !type.IsValueType;
    }

    /// <summary>
    /// Checks if a type is a ToonValue or derived class.
    /// </summary>
    /// <param name="type">
    /// The type to be checked.
    /// </param>
    /// <returns>
    /// true if the type is a ToonValue or derives from ToonValue; otherwise, false.
    /// </returns>
    public static bool IsToonValueType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return type.Name == "ToonValue";
        }

        var baseType = namedType.BaseType;

        while (baseType != null)
        {
            if (baseType.Name == "ToonValue")
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return type.Name == "ToonValue";
    }
}