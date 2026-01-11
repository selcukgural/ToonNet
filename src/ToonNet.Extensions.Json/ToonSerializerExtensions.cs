using System.Text.Json;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Extensions.Json;

/// <summary>
/// Extension methods for ToonSerializer to provide System.Text.Json-like API for format conversions.
/// </summary>
/// <remarks>
/// These extensions make ToonNet API familiar to developers who use System.Text.Json,
/// providing seamless conversion between JSON and TOON formats.
/// </remarks>
public static class ToonSerializerExtensions
{
    /// <summary>
    /// Deserializes a JSON string to a .NET object using TOON as intermediate format.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="jsonString">The JSON string to deserialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when jsonString is null.</exception>
    /// <exception cref="JsonException">Thrown when JSON parsing fails.</exception>
    /// <exception cref="ToonDeserializationException">Thrown when TOON deserialization fails.</exception>
    /// <remarks>
    /// This method provides a System.Text.Json-like API:
    /// <code>
    /// var person = ToonSerializer.DeserializeFromJson&lt;Person&gt;(jsonString);
    /// </code>
    /// </remarks>
    public static T? DeserializeFromJson<T>(string jsonString, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(jsonString);

        var obj = JsonSerializer.Deserialize<T>(jsonString);
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
    /// string json = ToonSerializer.SerializeToJson(person);
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
    /// string toonString = ToonSerializer.FromJson(jsonString);
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
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>JSON format string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toonString is null.</exception>
    /// <exception cref="ToonParseException">Thrown when TOON parsing fails.</exception>
    /// <remarks>
    /// This method provides a simple, developer-friendly API for TOON to JSON conversion:
    /// <code>
    /// string jsonString = ToonSerializer.ToJson(toonString);
    /// </code>
    /// </remarks>
    public static string ToJson(string toonString, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toonString);

        var parser = new ToonNet.Core.Parsing.ToonParser();
        var toonDocument = parser.Parse(toonString);
        return ToonJsonConverter.ToJson(toonDocument, options?.WriteIndented ?? true);
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
    /// var person = ToonSerializer.ParseJson&lt;Person&gt;(jsonString);
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
