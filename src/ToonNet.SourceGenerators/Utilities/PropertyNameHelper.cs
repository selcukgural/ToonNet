using System.Text;
using ToonNet.Core.Serialization;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
///     Helper utilities for property name transformations during code generation.
/// </summary>
internal static class PropertyNameHelper
{
    /// <summary>
    ///     Converts a property name to camelCase.
    ///     Example: FirstName → firstName
    /// </summary>
    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    ///     Converts a property name to snake_case.
    ///     Example: FirstName → first_name
    /// </summary>
    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
            {
                result.Append('_');
            }

            result.Append(char.ToLowerInvariant(name[i]));
        }

        return result.ToString();
    }

    /// <summary>
    ///     Applies a naming policy to a property name.
    /// </summary>
    public static string ApplyNamingPolicy(string name, PropertyNamingPolicy policy)
    {
        return policy switch
        {
            PropertyNamingPolicy.CamelCase => ToCamelCase(name),
            PropertyNamingPolicy.SnakeCase => ToSnakeCase(name),
            PropertyNamingPolicy.LowerCase => name.ToLowerInvariant(),
            _                              => name
        };
    }

    /// <summary>
    ///     Gets the serialized property name (respecting custom names and policies).
    /// </summary>
    public static string GetSerializedPropertyName(string propertyName, string? customName, PropertyNamingPolicy policy)
    {
        // Custom name takes priority
        if (!string.IsNullOrEmpty(customName))
        {
            return customName;
        }

        // Apply naming policy
        return ApplyNamingPolicy(propertyName, policy);
    }
}