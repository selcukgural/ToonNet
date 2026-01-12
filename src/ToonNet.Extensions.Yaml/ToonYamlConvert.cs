using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

namespace ToonNet.Extensions.Yaml;

/// <summary>
/// Provides utility methods for converting between YAML, TOON, and .NET objects.
/// </summary>
/// <remarks>
/// This class follows the industry-standard naming convention (like JsonConvert, XmlConvert)
/// and provides a clean, familiar API for format conversions.
/// Similar to ToonConvert for JSON conversions.
/// </remarks>
public static class ToonYamlConvert
{
    /// <summary>
    /// Converts a YAML string directly to TOON format string.
    /// </summary>
    /// <param name="yamlString">The YAML string to convert.</param>
    /// <param name="options">Optional TOON encoding options.</param>
    /// <returns>TOON format string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when yamlString is null.</exception>
    /// <exception cref="YamlDotNet.Core.YamlException">Thrown when YAML parsing fails.</exception>
    /// <remarks>
    /// This method provides a simple, developer-friendly API for YAML to TOON conversion:
    /// <code>
    /// string toonString = ToonYamlConvert.FromYaml(yamlString);
    /// </code>
    /// No need to deal with ToonDocument or ToonEncoder - just like System.Text.Json!
    /// </remarks>
    public static string FromYaml(string yamlString, ToonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(yamlString);

        var toonDocument = ToonYamlConverter.FromYaml(yamlString);
        var encoder = new ToonEncoder(options ?? ToonOptions.Default);
        return encoder.Encode(toonDocument);
    }

    /// <summary>
    /// Converts a TOON format string to YAML format string.
    /// </summary>
    /// <param name="toonString">The TOON string to convert.</param>
    /// <returns>YAML format string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toonString is null.</exception>
    /// <exception cref="ToonParseException">Thrown when TOON parsing fails.</exception>
    /// <remarks>
    /// This method provides a simple, developer-friendly API for TOON to YAML conversion:
    /// <code>
    /// string yamlString = ToonYamlConvert.ToYaml(toonString);
    /// </code>
    /// </remarks>
    public static string ToYaml(string toonString)
    {
        ArgumentNullException.ThrowIfNull(toonString);

        var parser = new Core.Parsing.ToonParser();
        var toonDocument = parser.Parse(toonString);
        return ToonYamlConverter.ToYaml(toonDocument);
    }
}
