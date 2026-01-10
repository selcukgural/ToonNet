namespace ToonNet.Core.Serialization;

/// <summary>
///     Configuration options for TOON serialization.
/// </summary>
public sealed class ToonSerializerOptions
{
    /// <summary>
    ///     Options for parsing/encoding.
    /// </summary>
    public ToonOptions ToonOptions { get; set; } = ToonOptions.Default;

    /// <summary>
    ///     Whether to ignore null values during serialization.
    /// </summary>
    public bool IgnoreNullValues { get; set; } = false;

    /// <summary>
    ///     Whether to use property names as-is or convert to camelCase/snake_case.
    /// </summary>
    public PropertyNamingPolicy PropertyNamingPolicy { get; set; } = PropertyNamingPolicy.Default;

    /// <summary>
    ///     Whether to include type information for polymorphic scenarios.
    /// </summary>
    public bool IncludeTypeInformation { get; set; } = false;

    /// <summary>
    ///     Maximum depth for serialization (prevents circular references).
    /// </summary>
    public int MaxDepth { get; set; } = 64;

    /// <summary>
    ///     Custom converters to use.
    /// </summary>
    public List<IToonConverter> Converters { get; } = [];

    /// <summary>
    ///     Whether to serialize only public properties/fields.
    /// </summary>
    public bool PublicOnly { get; set; } = true;

    /// <summary>
    ///     Whether to include read-only properties in serialization.
    /// </summary>
    public bool IncludeReadOnlyProperties { get; set; } = true;

    /// <summary>
    ///     Default instance with standard settings.
    /// </summary>
    public static ToonSerializerOptions Default => new();

    /// <summary>
    ///     Gets a converter for the specified type.
    /// </summary>
    /// <param name="type">The type to find a converter for.</param>
    /// <returns>The first converter that can handle the type, or null if none found.</returns>
    public IToonConverter? GetConverter(Type type)
    {
        return Converters.FirstOrDefault(c => c.CanConvert(type));
    }
}

/// <summary>
///     Property naming policies.
/// </summary>
public enum PropertyNamingPolicy
{
    /// <summary>
    ///     Use property names as-is (PascalCase in C#).
    /// </summary>
    Default,

    /// <summary>
    ///     Convert to camelCase (firstName).
    /// </summary>
    CamelCase,

    /// <summary>
    ///     Convert to snake_case (first_name).
    /// </summary>
    SnakeCase,

    /// <summary>
    ///     Convert to lowercase.
    /// </summary>
    LowerCase
}