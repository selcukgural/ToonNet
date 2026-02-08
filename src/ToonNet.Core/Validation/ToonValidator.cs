using System.ComponentModel.DataAnnotations;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Validation;

/// <summary>
/// Provides validation helpers for TOON content and documents.
/// </summary>
public static class ToonValidator
{
    /// <summary>
    /// Validates a TOON string without deserializing it to a CLR type.
    /// </summary>
    /// <param name="toonContent">The TOON content to validate.</param>
    /// <param name="options">Optional parsing options.</param>
    /// <returns>The validation result containing errors and warnings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toonContent"/> is null.</exception>
    /// <remarks>
    /// In strict mode, validation fails fast on the first parsing error.
    /// In non-strict mode, strict-only violations are reported as warnings.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = ToonValidator.Validate("items[5]: 1,2,3", new ToonOptions { StrictMode = false });
    /// if (!result.IsValid)
    /// {
    ///     foreach (var error in result.Errors)
    ///     {
    ///         Console.WriteLine(error.Message);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static ValidationResult Validate(string toonContent, ToonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toonContent);

        options ??= ToonOptions.Default;

        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();

        var optionsError = TryValidateOptions(options);
        if (optionsError != null)
        {
            errors.Add(optionsError);
            return new ValidationResult(errors, warnings);
        }

        try
        {
            var parser = new ToonParser(options);
            _ = parser.Parse(toonContent);
        }
        catch (ToonParseException ex)
        {
            errors.Add(CreateParseError(ex, ToonValidationErrorCodes.ParseError, ValidationSeverity.Error));
            return new ValidationResult(errors, warnings);
        }

        if (options.StrictMode)
        {
            return new ValidationResult(errors, warnings);
        }
        
        var strictOptions = CreateStrictOptions(options);

        try
        {
            var strictParser = new ToonParser(strictOptions);
            _ = strictParser.Parse(toonContent);
        }
        catch (ToonParseException ex)
        {
            warnings.Add(CreateParseError(ex, ToonValidationErrorCodes.StrictModeViolation, ValidationSeverity.Warning));
        }
        
        return new ValidationResult(errors, warnings);
    }

    /// <summary>
    /// Validates a parsed TOON document against the provided options.
    /// </summary>
    /// <param name="document">The parsed TOON document to validate.</param>
    /// <param name="options">Optional validation options.</param>
    /// <returns>The validation result containing errors and warnings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
    /// <remarks>
    /// Validation fails fast when the document exceeds configured depth limits.
    /// </remarks>
    public static ValidationResult Validate(ToonDocument document, ToonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        options ??= ToonOptions.Default;

        var errors = new List<ValidationError>();
        var warnings = Array.Empty<ValidationError>();

        var optionsError = TryValidateOptions(options);
        if (optionsError != null)
        {
            errors.Add(optionsError);
            return new ValidationResult(errors, warnings);
        }

        var depthError = TryValidateDepth(document.Root, options.MaxDepth);

        if (depthError == null)
        {
            return new ValidationResult(errors, warnings);
        }

        errors.Add(depthError);
        return new ValidationResult(errors, warnings);
    }

    /// <summary>
    /// Attempts to validate the specified <see cref="ToonOptions"/> and returns a validation error if the options are invalid.
    /// </summary>
    /// <param name="options">The TOON options to validate.</param>
    /// <returns>
    /// A <see cref="ValidationError"/> object if validation fails, or <c>null</c> if the options are valid.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <remarks>
    /// This method is used internally to validate configuration options for TOON validation.
    /// Validation checks include required properties and range constraints defined in the <see cref="ToonOptions"/> type.
    /// If validation fails, only the first error encountered is returned.
    /// </remarks>
    private static ValidationError? TryValidateOptions(ToonOptions options)
    {
        var validationContext = new ValidationContext(options);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (Validator.TryValidateObject(options, validationContext, results, validateAllProperties: true))
        {
            return null;
        }

        var firstResult = results.Count > 0 ? results[0] : new System.ComponentModel.DataAnnotations.ValidationResult("ToonOptions validation failed.");
        var path = string.Join('.', firstResult.MemberNames);

        return new ValidationError(
            ToonValidationErrorCodes.OptionsInvalid,
            firstResult.ErrorMessage ?? "ToonOptions validation failed.",
            ValidationSeverity.Error,
            path: string.IsNullOrWhiteSpace(path) ? null : path);
    }

    /// <summary>
    /// Validates the depth of a TOON document against a specified maximum depth.
    /// </summary>
    /// <param name="root">The root <see cref="ToonValue"/> of the TOON document.</param>
    /// <param name="maxDepth">The maximum allowed depth for the TOON document. If exceeded, validation fails.</param>
    /// <returns>A <see cref="ValidationError"/> if the document exceeds the maximum depth, otherwise null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    /// <remarks>
    /// This method traverses the TOON document to ensure its depth does not exceed the specified limit.
    /// Depth is tracked recursively for nested objects and arrays.
    /// </remarks>
    private static ValidationError? TryValidateDepth(ToonValue root, int maxDepth)
    {
        ArgumentNullException.ThrowIfNull(root);
        
        var stack = new Stack<(ToonValue Value, int Depth, string Path)>();
        stack.Push((root, 0, "$"));

        while (stack.Count > 0)
        {
            var (value, depth, path) = stack.Pop();

            if (depth > maxDepth)
            {
                return new ValidationError(
                    ToonValidationErrorCodes.MaxDepthExceeded,
                    $"Maximum depth of {maxDepth} exceeded during validation.",
                    ValidationSeverity.Error,
                    path: path);
            }

            switch (value)
            {
                case ToonObject obj:
                {
                    foreach (var (key, child) in obj.Properties)
                    {
                        stack.Push((child, depth + 1, $"{path}.{key}"));
                    }

                    break;
                }
                case ToonArray array:
                {
                    for (var i = array.Items.Count - 1; i >= 0; i--)
                    {
                        stack.Push((array.Items[i], depth + 1, $"{path}[{i}]"));
                    }

                    break;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a strict version of the provided <see cref="ToonOptions"/> instance.
    /// </summary>
    /// <param name="options">The original <see cref="ToonOptions"/> object to base the strict configuration on.</param>
    /// <returns>A new <see cref="ToonOptions"/> instance with strict mode enabled and other properties copied from the original instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    private static ToonOptions CreateStrictOptions(ToonOptions options)
    {
        return new ToonOptions
        {
            IndentSize = options.IndentSize,
            Delimiter = options.Delimiter,
            AllowExtendedLimits = options.AllowExtendedLimits,
            MaxDepth = options.MaxDepth,
            StrictMode = true
        };
    }

    /// <summary>
    /// Creates a parse error based on the provided exception, error code, and severity.
    /// </summary>
    /// <param name="exception">The exception containing parsing details such as message, line, and column.</param>
    /// <param name="code">The error code representing the type of parsing error.</param>
    /// <param name="severity">The severity level of the validation error (e.g., Error or Warning).</param>
    /// <returns>A <see cref="ValidationError"/> containing details of the parsing error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    /// <remarks>
    /// This method converts parsing-related information from a <see cref="ToonParseException"/> into a standardized
    /// validation error object that can be consumed in validation workflows.
    /// </remarks>
    private static ValidationError CreateParseError(ToonParseException exception, string code, ValidationSeverity severity)
    {
        return new ValidationError(
            code,
            exception.Message,
            severity,
            GetZeroBasedOrNull(exception.Line),
            GetZeroBasedOrNull(exception.Column));
    }

    /// <summary>
    /// Converts a one-based integer value to a zero-based integer or null if the input is less than or equal to zero.
    /// </summary>
    /// <param name="value">The one-based integer value to convert.</param>
    /// <returns>A zero-based integer if the input is greater than zero; otherwise, null.</returns>
    private static int? GetZeroBasedOrNull(int value)
    {
        return value > 0 ? value - 1 : null;
    }
}
