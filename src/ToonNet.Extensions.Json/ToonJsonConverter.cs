using System.Text.Json;
using ToonNet.Core.Models;

namespace ToonNet.Extensions.Json;

/// <summary>
///     Provides bidirectional conversion between JSON and TOON formats.
///     Supports System.Text.Json integration for seamless interoperability.
/// </summary>
/// <remarks>
///     This static class acts as a utility for converting data between JSON and TOON formats.
///     It includes methods for serializing and deserializing TOON documents and values,
///     ensuring compatibility with System.Text.Json.
/// </remarks>
public static class ToonJsonConverter
{
    /// <summary>
    ///     Converts a JSON string to a TOON document.
    /// </summary>
    /// <param name="json">
    ///     The JSON string to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A <see cref="ToonDocument"/> representing the JSON data.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="json"/> parameter is null.
    /// </exception>
    /// <exception cref="JsonException">
    ///     Thrown when the JSON parsing fails due to invalid JSON format.
    /// </exception>
    /// <remarks>
    ///     This method parses the provided JSON string into a <see cref="JsonDocument"/> and
    ///     converts its root element into a <see cref="ToonDocument"/> using the <see cref="FromJson(JsonElement)"/> method.
    /// </remarks>
    public static ToonDocument FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var doc = JsonDocument.Parse(json);
        return FromJson(doc.RootElement);
    }

    /// <summary>
    ///     Converts a JsonElement to a TOON document.
    /// </summary>
    /// <param name="element">
    ///     The <see cref="JsonElement"/> to convert. This element represents the JSON data to be transformed
    ///     into a TOON document structure.
    /// </param>
    /// <returns>
    ///     A <see cref="ToonDocument"/> that encapsulates the converted TOON representation of the JSON data.
    /// </returns>
    /// <remarks>
    ///     This method utilizes <see cref="ConvertJsonElementToToonValue"/> to transform the provided
    ///     <paramref name="element"/> into a TOON value, which is then wrapped in a <see cref="ToonDocument"/>.
    ///     The method assumes that the input JSON element is valid and does not perform additional validation.
    /// </remarks>
    public static ToonDocument FromJson(JsonElement element)
    {
        var toonValue = ConvertJsonElementToToonValue(element);
        return new ToonDocument(toonValue);
    }

    /// <summary>
    ///     Converts a TOON document to a JSON string.
    /// </summary>
    /// <param name="document">
    ///     The TOON document to convert. Must not be null.
    /// </param>
    /// <param name="writeIndented">
    ///     A boolean indicating whether to format the JSON with indentation.
    ///     Defaults to false.
    /// </param>
    /// <returns>
    ///     A JSON string representation of the TOON document.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="document"/> is null.
    /// </exception>
    /// <remarks>
    ///     This method serializes the root value of the provided TOON document into a JSON string.
    ///     The caller can specify whether the resulting JSON should be indented for readability.
    /// </remarks>
    public static string ToJson(ToonDocument document, bool writeIndented = false)
    {
        ArgumentNullException.ThrowIfNull(document);

        return ToJson(document.Root, writeIndented);
    }

    /// <summary>
    ///     Converts a TOON value to a JSON string.
    /// </summary>
    /// <param name="value">
    ///     The TOON value to convert. Must not be null.
    /// </param>
    /// <param name="writeIndented">
    ///     A boolean indicating whether to format the JSON with indentation.
    ///     Defaults to false.
    /// </param>
    /// <returns>
    ///     A JSON string representation of the TOON value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="value"/> is null.
    /// </exception>
    /// <remarks>
    ///     This method serializes the provided TOON value into a JSON string. The serialization
    ///     process uses a <see cref="Utf8JsonWriter"/> to write the JSON output. Complex TOON
    ///     types such as objects and arrays are serialized recursively. The caller can specify
    ///     whether the resulting JSON should be indented for readability.
    /// </remarks>
    public static string ToJson(ToonValue value, bool writeIndented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

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

    /// <summary>
    ///     Converts a JSON element to a TOON value.
    /// </summary>
    /// <param name="element">
    ///     The <see cref="JsonElement"/> to convert. The element's <see cref="JsonValueKind"/> determines
    ///     the type of TOON value created.
    /// </param>
    /// <returns>
    ///     A <see cref="ToonValue"/> representing the JSON element. The returned value can be one of the following:
    ///     <list type="bullet">
    ///         <item><description><see cref="ToonObject"/> for JSON objects.</description></item>
    ///         <item><description><see cref="ToonArray"/> for JSON arrays.</description></item>
    ///         <item><description><see cref="ToonString"/> for JSON strings.</description></item>
    ///         <item><description><see cref="ToonNumber"/> for JSON numbers.</description></item>
    ///         <item><description><see cref="ToonBoolean"/> for JSON boolean values.</description></item>
    ///         <item><description><see cref="ToonNull"/> for JSON null or undefined values.</description></item>
    ///     </list>
    /// </returns>
    /// <exception cref="JsonException">
    ///     Thrown if the <paramref name="element"/> has an unsupported <see cref="JsonValueKind"/>.
    /// </exception>
    /// <remarks>
    ///     This method uses a switch expression to map the <see cref="JsonValueKind"/> of the input
    ///     <paramref name="element"/> to the corresponding TOON value type. Unsupported kinds will result
    ///     in a <see cref="JsonException"/> being thrown.
    /// </remarks>
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

    /// <summary>
    ///     Converts a JSON object to a TOON object.
    /// </summary>
    /// <param name="element">
    ///     The <see cref="JsonElement"/> representing the JSON object to convert.
    ///     Must be of type <see cref="JsonValueKind.Object"/>.
    /// </param>
    /// <returns>
    ///     A <see cref="ToonObject"/> containing the converted TOON key-value pairs from the JSON object.
    /// </returns>
    /// <exception cref="JsonException">
    ///     Thrown if the <paramref name="element"/> is not of type <see cref="JsonValueKind.Object"/>.
    /// </exception>
    /// <remarks>
    ///     This method iterates over the properties of the JSON object, converts each property's value
    ///     to a corresponding TOON value using <see cref="ConvertJsonElementToToonValue"/>, and adds it
    ///     to the resulting <see cref="ToonObject"/>.
    /// </remarks>
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

    /// <summary>
    ///     Converts a JSON array to a TOON array.
    /// </summary>
    /// <param name="element">
    ///     The <see cref="JsonElement"/> representing the JSON array to convert.
    ///     Must be of type <see cref="JsonValueKind.Array"/>.
    /// </param>
    /// <returns>
    ///     A <see cref="ToonArray"/> containing the converted TOON values from the JSON array.
    /// </returns>
    /// <exception cref="JsonException">
    ///     Thrown if the <paramref name="element"/> is not of type <see cref="JsonValueKind.Array"/>.
    /// </exception>
    /// <remarks>
    ///     This method iterates over the elements of the JSON array, converts each element
    ///     to a corresponding TOON value using <see cref="ConvertJsonElementToToonValue"/>, and
    ///     adds it to the resulting <see cref="ToonArray"/>.
    /// </remarks>
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

    /// <summary>
    ///     Serializes a TOON value into JSON using the provided Utf8JsonWriter.
    /// </summary>
    /// <param name="writer">
    ///     The Utf8JsonWriter instance used to write the JSON output.
    ///     Must not be null.
    /// </param>
    /// <param name="value">
    ///     The TOON value to serialize. This can be of various types such as
    ///     ToonNull, ToonBoolean, ToonNumber, ToonString, ToonObject, or ToonArray.
    ///     Must not be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="writer"/> or <paramref name="value"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the type of <paramref name="value"/> is unsupported for serialization.
    /// </exception>
    /// <remarks>
    ///     This method determines the type of the provided TOON value and writes
    ///     the corresponding JSON representation. Complex types such as ToonObject
    ///     and ToonArray are serialized recursively.
    /// </remarks>
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

    /// <summary>
    /// Writes a TOON object as a JSON object using the provided Utf8JsonWriter.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter instance used to write the JSON output.</param>
    /// <param name="obj">The TOON object to be serialized into JSON.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="writer"/> or <paramref name="obj"/> is null.
    /// </exception>
    /// <remarks>
    /// This method iterates over the properties of the TOON object and writes each key-value pair
    /// as a JSON property. The value is recursively serialized using <see cref="WriteToonValueAsJson"/>.
    /// </remarks>
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

    /// <summary>
    /// Writes a TOON array as a JSON array using the provided Utf8JsonWriter.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter instance used to write the JSON output.</param>
    /// <param name="array">The TOON array to be serialized into JSON.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="writer"/> or <paramref name="array"/> is null.
    /// </exception>
    /// <remarks>
    /// This method iterates over the items in the TOON array and writes each item
    /// as a JSON element. Each item is recursively serialized using <see cref="WriteToonValueAsJson"/>.
    /// </remarks>
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