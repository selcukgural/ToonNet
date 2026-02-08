namespace ToonNet.Core.Validation;

/// <summary>
/// Defines stable error codes for TOON validation results.
/// </summary>
public static class ToonValidationErrorCodes
{
    /// <summary>
    /// Indicates a general parsing failure for the provided input.
    /// </summary>
    public const string ParseError = "TOON_PARSE_ERROR";

    /// <summary>
    /// Indicates a strict-mode-only violation detected during validation.
    /// </summary>
    public const string StrictModeViolation = "TOON_STRICT_VIOLATION";

    /// <summary>
    /// Indicates invalid validation or parsing options.
    /// </summary>
    public const string OptionsInvalid = "TOON_OPTIONS_INVALID";

    /// <summary>
    /// Indicates the document exceeds configured depth limits.
    /// </summary>
    public const string MaxDepthExceeded = "TOON_MAX_DEPTH_EXCEEDED";
}