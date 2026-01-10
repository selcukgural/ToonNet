namespace ToonNet.Core;

/// <summary>
///     Configuration options for TOON parsing and encoding.
/// </summary>
public sealed class ToonOptions
{
    /// <summary>
    ///     Number of spaces per indentation level. Default is 2.
    /// </summary>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    ///     Delimiter character for array values. Default is comma (,).
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    ///     Whether to enable strict parsing mode. Default is true.
    ///     In strict mode, invalid documents throw exceptions.
    /// </summary>
    public bool StrictMode { get; set; } = true;

    /// <summary>
    ///     Maximum nesting depth allowed. Default is 64.
    /// </summary>
    public int MaxDepth { get; set; } = 64;

    /// <summary>
    ///     Gets the default TOON options instance.
    /// </summary>
    public static ToonOptions Default => new();
}