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

    /// <summary>
    ///     Gets or sets the number of spaces per indentation level.
    ///     Must be an even number between 2 and 100 per TOON specification §12.
    ///     The default value is 2.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the value is less than 2, greater than 100, or not an even number.
    /// </exception>
    /// <remarks>
    ///     TOON specification §12 requires indentation to be a multiple of 2 spaces.
    ///     The recommended value is 2 for consistency with the specification.
    /// </remarks>
    [Range(2, 100)]
    public int IndentSize
    {
        get => _indentSize;
        set
        {
            switch (value)
            {
                // Check range first (more specific error messages)
                case < 2:
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "IndentSize must be at least 2 per TOON specification §12");
                case > 100:
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "IndentSize cannot exceed 100 for readability and performance");
            }

            // Then check if even (spec requirement)
            if (value % 2 != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "IndentSize must be an even number (2, 4, 6, ...) per TOON specification §12");
            }

            _indentSize = value;
        }
    }

    /// <summary>
    ///     Gets or sets the delimiter character for array values.
    ///     Cannot be whitespace, newline, tab, or control characters.
    ///     The default value is comma (',').
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     Thrown when the value is a whitespace character, newline, tab, or control character.
    /// </exception>
    /// <remarks>
    ///     Per TOON specification §11, delimiters must be printable non-whitespace characters.
    /// </remarks>
    public char Delimiter
    {
        get => _delimiter;
        set
        {
            // Check for the newline/tab first (more specific message)
            if (value is '\n' or '\r' or '\t')
            {
                throw new ArgumentException(
                    $"Delimiter cannot be a newline or tab character (U+{(int)value:X4})",
                    nameof(value));
            }

            if (char.IsWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"Delimiter cannot be a whitespace character (U+{(int)value:X4})",
                    nameof(value));
            }

            if (char.IsControl(value))
            {
                throw new ArgumentException(
                    $"Delimiter cannot be a control character (U+{(int)value:X4})",
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
    ///     Gets or sets the maximum nesting depth allowed during parsing.
    ///     Must be between 1 and 200 (or 1000 if <see cref="AllowExtendedLimits"/> is true).
    ///     The default value is 100.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the value is less than 1 or exceeds the allowed maximum
    ///     (200 by default, or 1000 with <see cref="AllowExtendedLimits"/>).
    /// </exception>
    /// <remarks>
    ///     TOON specification §15 recommends 100 as default for security.
    ///     Standard limit is 200. Enable <see cref="AllowExtendedLimits"/> to allow up to 1000.
    ///     Very high values may cause stack overflow issues.
    /// </remarks>
    [Range(1, 1000)]
    public int MaxDepth
    {
        get => _maxDepth;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "MaxDepth must be at least 1");
            }

            int maxAllowed = AllowExtendedLimits ? 1000 : 200;
            if (value > maxAllowed)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    AllowExtendedLimits
                        ? "MaxDepth cannot exceed 1000 even with extended limits enabled"
                        : "MaxDepth cannot exceed 200. Set AllowExtendedLimits = true to allow up to 1000");
            }

            _maxDepth = value;
        }
    }

    /// <summary>
    ///     Validates the current instance using DataAnnotations rules.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A sequence of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IndentSize % 2 != 0)
        {
            yield return new ValidationResult(
                "IndentSize must be an even number (2, 4, 6, ...) per TOON specification §12",
                [nameof(IndentSize)]);
        }

        var maxAllowed = AllowExtendedLimits ? 1000 : 200;
        if (MaxDepth > maxAllowed)
        {
            yield return new ValidationResult(
                AllowExtendedLimits
                    ? "MaxDepth cannot exceed 1000 even with extended limits enabled"
                    : "MaxDepth cannot exceed 200. Set AllowExtendedLimits = true to allow up to 1000",
                [nameof(MaxDepth), nameof(AllowExtendedLimits)]);
        }

        if (Delimiter is '\n' or '\r' or '\t')
        {
            yield return new ValidationResult(
                $"Delimiter cannot be a newline or tab character (U+{(int)Delimiter:X4})",
                [nameof(Delimiter)]);
        }
        else if (char.IsWhiteSpace(Delimiter))
        {
            yield return new ValidationResult(
                $"Delimiter cannot be a whitespace character (U+{(int)Delimiter:X4})",
                [nameof(Delimiter)]);
        }
        else if (char.IsControl(Delimiter))
        {
            yield return new ValidationResult(
                $"Delimiter cannot be a control character (U+{(int)Delimiter:X4})",
                [nameof(Delimiter)]);
        }
    }

    /// <summary>
    ///     Gets the default instance of <see cref="ToonOptions" /> with standard settings.
    /// </summary>
    public static ToonOptions Default => new();
}