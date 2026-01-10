using System.Text.Json;
using ToonNet.Core.Models;

namespace ToonNet.Core.Interop;

/// <summary>
///     Provides bidirectional conversion between JSON and TOON formats.
///     Supports System.Text.Json integration for seamless interoperability.
/// </summary>
public static class ToonJsonConverter
{
    /// <summary>
    ///     Converts a JSON string to a TOON document.
    /// </summary>
    /// <param name="json">The JSON string to convert.</param>
    /// <returns>A ToonDocument representing the JSON data.</returns>
    /// <exception cref="JsonException">Thrown when JSON parsing fails.</exception>
    /// <exception cref="ArgumentNullException">Thrown when json is null.</exception>
    public static ToonDocument FromJson(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        using var doc = JsonDocument.Parse(json);
        return FromJson(doc.RootElement);
    }

    /// <summary>
    ///     Converts a JsonElement to a TOON document.
    /// </summary>
    /// <param name="element">The JsonElement to convert.</param>
    /// <returns>A ToonDocument representing the JSON data.</returns>
    public static ToonDocument FromJson(JsonElement element)
    {
        var toonValue = ConvertJsonElementToToonValue(element);
        return new ToonDocument(toonValue);
    }

    /// <summary>
    ///     Converts a TOON document to a JSON string.
    /// </summary>
    /// <param name="document">The TOON document to convert.</param>
    /// <param name="writeIndented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the TOON document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when document is null.</exception>
    public static string ToJson(ToonDocument document, bool writeIndented = false)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return ToJson(document.Root, writeIndented);
    }

    /// <summary>
    ///     Converts a TOON value to a JSON string.
    /// </summary>
    /// <param name="value">The TOON value to convert.</param>
    /// <param name="writeIndented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the TOON value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static string ToJson(ToonValue value, bool writeIndented = false)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        using var stream = new MemoryStream();

        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = writeIndented
        });

        WriteToonValueAsJson(writer, value);
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    #region JSON to TOON Conversion

    private static ToonValue ConvertJsonElementToToonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object    => ConvertJsonObject(element),
            JsonValueKind.Array     => ConvertJsonArray(element),
            JsonValueKind.String    => new ToonString(element.GetString()!),
            JsonValueKind.Number    => new ToonNumber(element.GetDouble()),
            JsonValueKind.True      => new ToonBoolean(true),
            JsonValueKind.False     => new ToonBoolean(false),
            JsonValueKind.Null      => ToonNull.Instance,
            JsonValueKind.Undefined => ToonNull.Instance,
            _                       => throw new JsonException($"Unsupported JSON value kind: {element.ValueKind}")
        };
    }

    private static ToonObject ConvertJsonObject(JsonElement element)
    {
        var toonObject = new ToonObject();

        foreach (var property in element.EnumerateObject())
        {
            var value = ConvertJsonElementToToonValue(property.Value);
            toonObject[property.Name] = value;
        }

        return toonObject;
    }

    private static ToonArray ConvertJsonArray(JsonElement element)
    {
        var items = new List<ToonValue>();

        foreach (var item in element.EnumerateArray())
        {
            items.Add(ConvertJsonElementToToonValue(item));
        }

        return new ToonArray(items);
    }

    #endregion

    #region TOON to JSON Conversion

    private static void WriteToonValueAsJson(Utf8JsonWriter writer, ToonValue value)
    {
        switch (value)
        {
            case ToonNull:
                writer.WriteNullValue();
                break;

            case ToonBoolean b:
                writer.WriteBooleanValue(b.Value);
                break;

            case ToonNumber n:
                writer.WriteNumberValue(n.Value);
                break;

            case ToonString s:
                writer.WriteStringValue(s.Value);
                break;

            case ToonObject o:
                WriteObjectAsJson(writer, o);
                break;

            case ToonArray a:
                WriteArrayAsJson(writer, a);
                break;

            default:
                throw new InvalidOperationException($"Unsupported TOON value type: {value.GetType().Name}");
        }
    }

    private static void WriteObjectAsJson(Utf8JsonWriter writer, ToonObject obj)
    {
        writer.WriteStartObject();

        foreach (var (key, value) in obj.Properties)
        {
            writer.WritePropertyName(key);
            WriteToonValueAsJson(writer, value);
        }

        writer.WriteEndObject();
    }

    private static void WriteArrayAsJson(Utf8JsonWriter writer, ToonArray array)
    {
        writer.WriteStartArray();

        foreach (var item in array.Items)
        {
            WriteToonValueAsJson(writer, item);
        }

        writer.WriteEndArray();
    }

    #endregion
}