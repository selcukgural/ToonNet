using System.Text.Json;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Extensions.Json;

/// <summary>
/// Provides utility methods for converting between JSON, TOON, and .NET objects.
/// </summary>
/// <remarks>
/// This class follows the industry-standard naming convention (like JsonConvert, XmlConvert)
/// and provides a clean, familiar API for format conversions.
/// Similar to Newtonsoft.Json's JsonConvert class.
/// </remarks>
public static class ToonConvert
{
    /// <summary>
    /// Deserializes a JSON string to a .NET object using TOON as the intermediate format.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
    /// <param name="jsonString">The JSON string to be deserialized.</param>
    /// <param name="options">Optional serialization options provided for JSON deserialization.</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonString"/> is null.</exception>
    /// <exception cref="JsonException">Thrown if the JSON string cannot be parsed properly.</exception>
    /// <exception cref="NotSupportedException">Thrown if an error occurs during TOON deserialization.</exception>
    public static T? DeserializeFromJson<T>(string jsonString, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(jsonString);

        var obj = JsonSerializer.Deserialize<T>(jsonString, options);
        return obj;
    }

    /// <summary>
    /// Serializes a .NET object to JSON string.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>JSON string representation.</returns>
    /// <remarks>
    /// This method provides a consistent API for format conversion:
    /// <code>
    /// string json = ToonConvert.SerializeToJson(person);
    /// </code>
    /// </remarks>
    public static string SerializeToJson<T>(T value, JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Converts a JSON string directly to TOON format string.
    /// </summary>
    /// <param name="jsonString">The JSON string to convert.</param>
    /// <param name="options">Optional TOON encoding options.</param>
    /// <returns>TOON format string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when jsonString is null.</exception>
    /// <exception cref="JsonException">Thrown when JSON parsing fails.</exception>
    /// <remarks>
    /// This method provides a simple, developer-friendly API for JSON to TOON conversion:
    /// <code>
    /// string toonString = ToonConvert.FromJson(jsonString);
    /// </code>
    /// No need to deal with ToonDocument or ToonEncoder - just like System.Text.Json!
    /// </remarks>
    public static string FromJson(string jsonString, ToonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(jsonString);

        var toonDocument = ToonJsonConverter.FromJson(jsonString);
        var encoder = new ToonEncoder(options ?? ToonOptions.Default);
        return encoder.Encode(toonDocument);
    }

    /// <summary>
    /// Converts a TOON format string to JSON format string.
    /// </summary>
    /// <param name="toonString">The TOON string to convert.</param>
    /// <param name="writerOptions">Optional JSON writer options to control formatting.</param>
    /// <returns>JSON format string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toonString is null.</exception>
    /// <exception cref="ToonParseException">Thrown when TOON parsing fails.</exception>
    /// <remarks>
    /// This method provides a simple, developer-friendly API for TOON to JSON conversion:
    /// <code>
    /// // Default (no indentation)
    /// string jsonString = ToonConvert.ToJson(toonString);
    /// 
    /// // With indentation
    /// string jsonString = ToonConvert.ToJson(toonString, new JsonWriterOptions { Indented = true });
    /// </code>
    /// </remarks>
    public static string ToJson(string toonString, JsonWriterOptions? writerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(toonString);

        var parser = new Core.Parsing.ToonParser();
        var toonDocument = parser.Parse(toonString);
        return ToonJsonConverter.ToJson(toonDocument, writerOptions);
    }

    /// <summary>
    /// Converts a JSON string to TOON and deserializes to a .NET object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="jsonString">The JSON string to convert and deserialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>The deserialized object.</returns>
    /// <remarks>
    /// This is a convenience method that combines JSON to TOON conversion with deserialization:
    /// <code>
    /// var person = ToonConvert.ParseJson&lt;Person&gt;(jsonString);
    /// </code>
    /// Equivalent to: Deserialize&lt;T&gt;(FromJson(jsonString))
    /// </remarks>
    public static T? ParseJson<T>(string jsonString, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(jsonString);

        var toonString = FromJson(jsonString);
        return ToonSerializer.Deserialize<T>(toonString, options);
    }
}
