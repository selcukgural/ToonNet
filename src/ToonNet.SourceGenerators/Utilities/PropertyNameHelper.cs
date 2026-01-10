using System.Text;
using ToonNet.Core.Serialization;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
/// Provides helper methods for transforming property names and applying naming policies
/// during code generation and serialization.
/// </summary>
internal static class PropertyNameHelper
{
    /// <summary>
    /// Converts a property name to camelCase.
    /// </summary>
    /// <param name="name">The property name to be converted.</param>
    /// <returns>The converted property name in camelCase.</returns>
    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>
    /// Converts a property name to snake_case.
    /// Example: FirstName → first_name
    /// </summary>
    /// <param name="name">The property name to be converted to snake_case.</param>
    /// <returns>The transformed property name in snake_case format.</returns>
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
    /// Applies a specified naming policy to a property name.
    /// </summary>
    /// <param name="name">The original property name to transform.</param>
    /// <param name="policy">The naming policy to apply.</param>
    /// <return>The transformed property name based on the specified naming policy.</return>
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
    /// Gets the serialized property name by applying a naming policy or using a custom name if provided.
    /// </summary>
    /// <param name="propertyName">
    /// The original name of the property to be serialized.
    /// </param>
    /// <param name="customName">
    /// The custom name to use for serialization, if specified. This overrides the naming policy.
    /// </param>
    /// <param name="policy">
    /// The naming policy to apply (e.g., CamelCase, SnakeCase, etc.) if no custom name is provided.
    /// </param>
    /// <returns>
    /// The serialized property name, transformed according to the custom name or the naming policy.
    /// </returns>
    public static string GetSerializedPropertyName(string propertyName, string? customName, PropertyNamingPolicy policy)
    {
        // Custom name takes priority
        return !string.IsNullOrEmpty(customName) ? customName :
                   // Apply naming policy
                   ApplyNamingPolicy(propertyName, policy);
    }
}