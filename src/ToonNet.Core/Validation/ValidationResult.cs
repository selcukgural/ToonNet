namespace ToonNet.Core.Validation;

/// <summary>
/// Represents the outcome of validating a TOON input or document.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="errors">The list of blocking validation errors.</param>
    /// <param name="warnings">The list of non-blocking validation warnings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errors"/> or <paramref name="warnings"/> is null.
    /// </exception>
    public ValidationResult(IEnumerable<ValidationError> errors, IEnumerable<ValidationError> warnings)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(warnings);

        Errors = errors.ToArray();
        Warnings = warnings.ToArray();
    }

    /// <summary>
    /// Gets a value indicating whether validation produced no blocking errors.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the list of blocking validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Gets the list of non-blocking validation warnings.
    /// </summary>
    public IReadOnlyList<ValidationError> Warnings { get; }
}

