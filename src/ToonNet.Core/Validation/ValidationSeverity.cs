namespace ToonNet.Core.Validation;

/// <summary>
/// Defines the severity of a validation finding.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Indicates a validation failure that blocks parsing or processing.
    /// </summary>
    Error,

    /// <summary>
    /// Indicates a non-blocking validation finding.
    /// </summary>
    Warning
}

