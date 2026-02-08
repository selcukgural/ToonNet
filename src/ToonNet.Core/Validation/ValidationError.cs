namespace ToonNet.Core.Validation;

/// <summary>
/// Represents a single validation finding for a TOON input or document.
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> class.
    /// </summary>
    /// <param name="code">The stable, machine-readable error code.</param>
    /// <param name="message">The human-readable error message.</param>
    /// <param name="severity">The severity of the validation finding.</param>
    /// <param name="lineNumber">
    /// The zero-based line number where the issue occurred, if available.
    /// </param>
    /// <param name="columnNumber">
    /// The zero-based character position in the line where the issue occurred, if available.
    /// </param>
    /// <param name="path">The logical path to the affected value, if known.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="code"/> or <paramref name="message"/> is empty.
    /// </exception>
    public ValidationError(string code, string message, ValidationSeverity severity, int? lineNumber = null,
                           int? columnNumber = null, string? path = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Error code cannot be null or whitespace.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message cannot be null or whitespace.", nameof(message));
        }

        Code = code;
        Message = message;
        Severity = severity;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        Path = path;
    }

    /// <summary>
    /// Gets the stable, machine-readable error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the severity of the validation finding.
    /// </summary>
    public ValidationSeverity Severity { get; }

    /// <summary>
    /// Gets the zero-based line number where the issue occurred, if available.
    /// </summary>
    public int? LineNumber { get; }

    /// <summary>
    /// Gets the zero-based character position in the line where the issue occurred, if available.
    /// </summary>
    public int? ColumnNumber { get; }

    /// <summary>
    /// Gets the logical path to the affected value, if known.
    /// </summary>
    public string? Path { get; }
}

