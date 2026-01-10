namespace ToonNet.Core;

/// <summary>
/// Configuration options for TOON parsing and encoding.
/// </summary>
public sealed class ToonOptions
{
    /// <summary>
    /// Gets or sets the number of spaces per indentation level.
    /// The default value is 2.
    /// </summary>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets the delimiter character for array values.
    /// The default value is a comma (',').
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// Gets or sets a value indicating whether strict parsing mode is enabled.
    /// Default value is true. In strict mode, invalid documents throw exceptions.
    /// </summary>
    public bool StrictMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum nesting depth allowed during parsing.
    /// The default value is 64.
    /// </summary>
    public int MaxDepth { get; set; } = 64;

    /// <summary>
    /// Gets the default instance of <see cref="ToonOptions"/> with standard settings.
    /// </summary>
    public static ToonOptions Default => new();
}