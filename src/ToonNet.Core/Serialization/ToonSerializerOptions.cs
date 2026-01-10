using System.ComponentModel.DataAnnotations;

namespace ToonNet.Core.Serialization;

/// <summary>
///     Configuration options for TOON serialization.
/// </summary>
public sealed class ToonSerializerOptions : IValidatableObject
{
    private int _maxDepth = 100;
    private const int DefaultMaxDepth = 200;
    private const int ExtendedMaxDepth = 1000;

    private ToonOptions _toonOptions = ToonOptions.Default;

    /// <summary>
    ///     Gets or sets the options for parsing/encoding.
    ///     Cannot be null.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the value is null.
    /// </exception>
    [Required]
    public ToonOptions ToonOptions
    {
        get => _toonOptions;
        set => _toonOptions = value ?? throw new ArgumentNullException(nameof(value), "ToonOptions cannot be null");
    }

    /// <summary>
    ///     Gets or sets whether to ignore null values during serialization.
    ///     The default value is false.
    /// </summary>
    public bool IgnoreNullValues { get; set; }

    /// <summary>
    ///     Gets or sets the property naming policy to use during serialization.
    ///     The default value is PropertyNamingPolicy.Default.
    /// </summary>
    public PropertyNamingPolicy PropertyNamingPolicy { get; set; } = PropertyNamingPolicy.Default;

    /// <summary>
    ///     Gets or sets whether to include type information for polymorphic scenarios.
    ///     The default value is false.
    /// </summary>
    public bool IncludeTypeInformation { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether extended limits are allowed.
    ///     When false (default), MaxDepth is limited to 200.
    ///     When true, MaxDepth can be set up to 1000.
    /// </summary>
    /// <remarks>
    ///     Enable this only when you need to serialize deeply nested structures.
    ///     Extended limits may increase memory usage and stack depth.
    /// </remarks>
    public bool AllowExtendedLimits { get; set; }

    /// <summary>
    ///     Gets or sets the maximum depth for serialization (prevents circular references).
    ///     Must be between 1 and 200 (or 1000 if <see cref="AllowExtendedLimits"/> is true).
    ///     The default value is 100.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the value is less than 1 or exceeds the allowed maximum
    ///     (200 by default, or 1000 with <see cref="AllowExtendedLimits"/>).
    /// </exception>
    /// <remarks>
    ///     This limit prevents infinite recursion from circular references and stack overflow.
    ///     TOON specification §15 recommends 100 for security considerations.
    ///     Standard limit is 200. Enable <see cref="AllowExtendedLimits"/> to allow up to 1000.
    /// </remarks>
    [Range(1, 1000)]
    public int MaxDepth
    {
        get => _maxDepth;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "MaxDepth must be at least 1");
            }

            var maxAllowed = AllowExtendedLimits ? ExtendedMaxDepth : DefaultMaxDepth;

            if (value > maxAllowed)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                                                      AllowExtendedLimits
                                                          ? $"{nameof(MaxDepth)} cannot exceed {ExtendedMaxDepth} even with extended limits enabled"
                                                          : $"{nameof(MaxDepth)} cannot exceed {DefaultMaxDepth}. Set {nameof(AllowExtendedLimits)} = true to allow up to {ExtendedMaxDepth}");
            }

            _maxDepth = value;
        }
    }

    /// <summary>
    ///     Gets the collection of custom converters to use during serialization.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="AddConverter"/> to safely add converters with null checking.
    /// </remarks>
    public List<IToonConverter> Converters { get; } = [];

    /// <summary>
    ///     Gets or sets whether to serialize only public properties/fields.
    ///     Default value is true.
    /// </summary>
    public bool PublicOnly { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether to include read-only properties in serialization.
    ///     Default value is true.
    /// </summary>
    public bool IncludeReadOnlyProperties { get; set; } = true;

    /// <summary>
    ///     Gets the default instance with standard settings.
    /// </summary>
    public static ToonSerializerOptions Default => new();

    /// <summary>
    ///     Adds a custom converter to the collection.
    /// </summary>
    /// <param name="converter">The converter to add. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the converter is null.
    /// </exception>
    public void AddConverter(IToonConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        Converters.Add(converter);
    }

    /// <summary>
    ///     Gets a converter for the specified type.
    /// </summary>
    /// <param name="type">The type to find a converter for.</param>
    /// <returns>
    ///     The first converter that can handle the type, or <c>null</c> if none is found.
    /// </returns>
    public IToonConverter? GetConverter(Type type)
    {
        return Converters.FirstOrDefault(c => c.CanConvert(type));
    }

    /// <summary>
    ///     Validates the current instance using DataAnnotations rules.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A sequence of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var maxAllowed = AllowExtendedLimits ? ExtendedMaxDepth : DefaultMaxDepth;
        if (MaxDepth > maxAllowed)
        {
            yield return new ValidationResult(
                AllowExtendedLimits
                    ? $"{nameof(MaxDepth)} cannot exceed {ExtendedMaxDepth} even with extended limits enabled"
                    : $"{nameof(MaxDepth)} cannot exceed {DefaultMaxDepth}. Set {nameof(AllowExtendedLimits)} = true to allow up to {ExtendedMaxDepth}",
                [nameof(MaxDepth), nameof(AllowExtendedLimits)]);
        }
    }
}

/// <summary>
/// Property naming policies.
/// </summary>
public enum PropertyNamingPolicy
{
    /// <summary>
    /// Use property names as-is (PascalCase in C#).
    /// </summary>
    Default,

    /// <summary>
    /// Convert to camelCase (e.g., firstName).
    /// </summary>
    CamelCase,

    /// <summary>
    /// Convert to snake_case (e.g., first_name).
    /// </summary>
    SnakeCase,

    /// <summary>
    /// Convert to lowercase.
    /// </summary>
    LowerCase
}