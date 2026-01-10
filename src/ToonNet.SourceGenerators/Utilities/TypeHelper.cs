using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
///     Helper utilities for detecting primitive and simple types during code generation.
/// </summary>
internal static class TypeHelper
{
    /// <summary>
    ///     Checks if a type is a simple/primitive type that can be directly serialized.
    /// </summary>
    public static bool IsSimpleType(ITypeSymbol type)
    {
        var name = type.ToDisplayString();

        return name is "string" or "string?" or "int" or "long" or "double" or "decimal" or "bool" or "float" or "byte" or "short" or "uint"
                       or "ulong" or "System.String" or "System.Int32" or "System.Int64" or "System.Double" or "System.Decimal" or "System.Boolean"
                       or "System.Single" or "System.Byte" or "System.Int16" or "System.UInt32" or "System.UInt64";
    }

    /// <summary>
    ///     Checks if a type is a numeric type.
    /// </summary>
    public static bool IsNumericType(string typeName)
    {
        return typeName is "int" or "long" or "double" or "decimal" or "float" or "byte" or "short" or "uint" or "ulong" or "System.Int32"
                           or "System.Int64" or "System.Double" or "System.Decimal" or "System.Single" or "System.Byte" or "System.Int16"
                           or "System.UInt32" or "System.UInt64";
    }

    /// <summary>
    ///     Checks if a type is a numeric type.
    /// </summary>
    public static bool IsNumericType(ITypeSymbol type)
    {
        return IsNumericType(type.ToDisplayString());
    }

    /// <summary>
    ///     Checks if a type is nullable (T?).
    /// </summary>
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
    ///     Gets the underlying type of a nullable type.
    /// </summary>
    public static ITypeSymbol? GetUnderlyingNullableType(ITypeSymbol type)
    {
        if (IsNullableType(type) && type is INamedTypeSymbol { TypeArguments.Length: > 0 } namedType)
        {
            return namedType.TypeArguments[0];
        }

        return null;
    }

    /// <summary>
    ///     Checks if a type is a string.
    /// </summary>
    public static bool IsStringType(ITypeSymbol type)
    {
        var name = type.ToDisplayString();
        return name is "string" or "System.String";
    }

    /// <summary>
    ///     Checks if a type is a boolean.
    /// </summary>
    public static bool IsBooleanType(ITypeSymbol type)
    {
        var name = type.ToDisplayString();
        return name is "bool" or "System.Boolean";
    }

    /// <summary>
    ///     Gets the type name with proper escaping for code generation.
    /// </summary>
    public static string GetSafeTypeName(ITypeSymbol type)
    {
        var name = type.ToDisplayString();

        // Use global:: prefix to avoid namespace conflicts
        if (name.StartsWith("System.", StringComparison.Ordinal))
        {
            return $"global::{name}";
        }

        return $"global::ToonNet.Core.Models.{name}";
    }

    /// <summary>
    ///     Checks if a type is reference type.
    /// </summary>
    public static bool IsReferenceType(ITypeSymbol type)
    {
        return !type.IsValueType;
    }

    /// <summary>
    ///     Checks if a type is a ToonValue or derived class.
    /// </summary>
    public static bool IsToonValueType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            var baseType = namedType.BaseType;

            while (baseType != null)
            {
                if (baseType.Name == "ToonValue")
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }
        }

        return type.Name == "ToonValue";
    }
}
