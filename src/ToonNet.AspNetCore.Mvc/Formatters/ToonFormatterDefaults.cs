namespace ToonNet.AspNetCore.Mvc.Formatters;

/// <summary>
/// Contains default values and media type constants used in TOON content format processing.
/// </summary>
public static class ToonFormatterDefaults
{
    /// <summary>
    /// Represents the default media type for TOON formatting.
    /// </summary>
    public const string MediaType = "application/toon";

    /// <summary>
    /// Alternative content type for TOON format (text/toon).
    /// </summary>
    public const string TextMediaType = "text/toon";

    /// <summary>
    /// The default maximum size, in bytes, of a TOON request body accepted by <see cref="ToonInputFormatter"/> (4 MB).
    /// </summary>
    public const long MaxRequestBodySize = 4 * 1024 * 1024;
}
