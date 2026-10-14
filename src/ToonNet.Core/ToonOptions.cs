using System.ComponentModel.DataAnnotations;

namespace ToonNet.Core;

/// <summary>
///     Configuration options for TOON parsing and encoding.
/// </summary>
public sealed class ToonOptions : IValidatableObject
{
    private int _indentSize = 2;
    private int _maxDepth = 100;
    private char _delimiter = ',';
    private const int DefaultMaxDepth = 200;
    private const int ExtendedMaxDepth = 1000;

    /// <summary>
    ///     Gets or sets the number of spaces per indentation level (the spec's <c>indentSize</c>, a fixed positive
    ///     number, §1.3 and §12), between 1 and 100. The default value is 2.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1 or greater than 100.</exception>
    [Range(1, 100)]
    public int IndentSize
    {
        get => _indentSize;
        set
        {
            switch (value)
            {
                // Check range first (more specific error messages)
                case < 1:
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "IndentSize must be a positive number of spaces per TOON specification §1.3");
                case > 100:
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "IndentSize cannot exceed 100 for readability and performance");
            }

            _indentSize = value;
        }
    }

    /// <summary>
    ///     Gets or sets the document delimiter for inline arrays and tabular rows: comma (','), tab ('\t') or pipe ('|').
    ///     The default value is comma (',').
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     Thrown when the value is not comma, tab or pipe.
    /// </exception>
    /// <remarks>
    ///     Per TOON specification §11, tab and pipe are declared in array headers (<c>[N\t]</c>, <c>[N|]</c>);
    ///     decoders always use the delimiter declared by each header, so this option only affects encoding.
    /// </remarks>
    public char Delimiter
    {
        get => _delimiter;
        set
        {
            if (value is not (',' or '\t' or '|'))
            {
                throw new ArgumentException(
                    $"Delimiter must be comma (','), tab ('\\t') or pipe ('|') per TOON specification §11, but was U+{(int)value:X4}",
                    nameof(value));
            }

            _delimiter = value;
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether strict parsing mode is enabled.
    ///     Default value is true. In strict mode, invalid documents throw exceptions.
    /// </summary>
    public bool StrictMode { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether extended limits are allowed.
    ///     When false (default), MaxDepth is limited to 200.
    ///     When true, MaxDepth can be set up to 1000.
    /// </summary>
    /// <remarks>
    ///     Enable this only when you need to process deeply nested structures.
    ///     Extended limits may increase memory usage and stack depth.
    /// </remarks>
    public bool AllowExtendedLimits { get; set; }

    /// <summary>
    ///     Gets or sets the maximum nesting depth of objects and arrays when parsing, encoding and validating.
    ///     The root value is at depth 0 and each object or array inside another is one level deeper; primitive values
    ///     do not count. Must be between 1 and 200 (or 1000 if <see cref="AllowExtendedLimits"/> is true).
    ///     The default value is 100.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1 or greater than 1000.</exception>
    /// <remarks>
    ///     A value above 200 without <see cref="AllowExtendedLimits"/> (which can be set in any order) makes parsing and
    ///     encoding throw an <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <remarks>
    ///     The TOON specification leaves the depth limit to implementations; it protects against stack exhaustion on
    ///     untrusted input. Standard limit is 200. Enable <see cref="AllowExtendedLimits"/> to allow up to 1000.
    ///     Very high values may cause stack overflow issues.
    /// </remarks>
    [Range(1, 1000)]
    public int MaxDepth
    {
        get => _maxDepth;
        set
        {
            if (value is < 1 or > ExtendedMaxDepth)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(MaxDepth)} must be between 1 and {ExtendedMaxDepth}");
            }

            // Values above 200 also need AllowExtendedLimits, which may be set before or after this property; the
            // combination is checked when the options are used (ValidatedMaxDepth) and by Validate.
            _maxDepth = value;
        }
    }

    /// <summary>
    ///     <see cref="MaxDepth"/>, after checking that a value above 200 comes with <see cref="AllowExtendedLimits"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="MaxDepth"/> exceeds 200 without extended limits.</exception>
    internal int ValidatedMaxDepth => _maxDepth <= DefaultMaxDepth || AllowExtendedLimits
        ? _maxDepth
        : throw new InvalidOperationException(
            $"{nameof(MaxDepth)} cannot exceed {DefaultMaxDepth}. Set {nameof(AllowExtendedLimits)} = true to allow up to {ExtendedMaxDepth}");

    /// <summary>
    ///     Validates the current instance using DataAnnotations rules.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A sequence of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var maxAllowed = AllowExtendedLimits ? 1000 : 200;
        if (MaxDepth > maxAllowed)
        {
            yield return new ValidationResult(
                AllowExtendedLimits
                    ? "MaxDepth cannot exceed 1000 even with extended limits enabled"
                    : "MaxDepth cannot exceed 200. Set AllowExtendedLimits = true to allow up to 1000",
                [nameof(MaxDepth), nameof(AllowExtendedLimits)]);
        }

        if (Delimiter is not (',' or '\t' or '|'))
        {
            yield return new ValidationResult(
                $"Delimiter must be comma (','), tab ('\\t') or pipe ('|') per TOON specification §11, but was U+{(int)Delimiter:X4}",
                [nameof(Delimiter)]);
        }
    }

    /// <summary>
    ///     Gets a new instance with the default settings. Each call returns a new instance, so changing one does not change
    ///     the defaults: create and keep your own instance (<c>new ToonOptions { ... }</c>) to use other settings.
    /// </summary>
    public static ToonOptions Default => new();

    /// <summary>A copy of these options in non-strict mode (§14.4).</summary>
    internal ToonOptions NonStrict() => new()
    {
        IndentSize = IndentSize,
        Delimiter = Delimiter,
        StrictMode = false,
        AllowExtendedLimits = AllowExtendedLimits,
        MaxDepth = MaxDepth
    };
}