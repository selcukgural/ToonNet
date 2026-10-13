using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ToonNet.Core;
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
    ///     Compact output that writes non-ASCII text as is (<c>ü</c>, not <c>\u00FC</c>); HTML-sensitive characters
    ///     stay escaped.
    /// </summary>
    private static readonly JsonWriterOptions DefaultWriterOptions = new()
    {
        Indented = false,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

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
        return FromJson(json, null);
    }

    /// <summary>
    ///     Converts a JSON string to a TOON document, using <paramref name="options"/> for the nesting limit and for
    ///     duplicate keys.
    /// </summary>
    /// <param name="json">The JSON string to convert.</param>
    /// <param name="options">
    ///     Options whose <see cref="ToonOptions.MaxDepth"/> limits the nesting (the root is at depth 0, as in TOON) and
    ///     whose <see cref="ToonOptions.StrictMode"/> decides duplicate keys: an error in strict mode (the default),
    ///     otherwise the last value wins.
    /// </param>
    /// <returns>A <see cref="ToonDocument"/> representing the JSON data.</returns>
    /// <remarks>
    ///     Comments and trailing commas are accepted, as in <c>appsettings.json</c> files.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid, too deep, or has duplicate keys in strict mode.</exception>
    public static ToonDocument FromJson(string json, ToonOptions? options)
    {
        ArgumentNullException.ThrowIfNull(json);
        options ??= ToonOptions.Default;

        var documentOptions = new JsonDocumentOptions
        {
            // JsonDocument counts the root container as depth 1
            MaxDepth = options.MaxDepth + 1,
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        using var doc = JsonDocument.Parse(json, documentOptions);
        return FromJson(doc.RootElement, options);
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
        return FromJson(element, null);
    }

    /// <summary>
    ///     Converts a JsonElement to a TOON document; duplicate keys are an error when
    ///     <see cref="ToonOptions.StrictMode"/> is on (the default), otherwise the last value wins.
    /// </summary>
    /// <exception cref="JsonException">Thrown for duplicate keys in strict mode.</exception>
    public static ToonDocument FromJson(JsonElement element, ToonOptions? options)
    {
        var toonValue = ConvertJsonElementToToonValue(element, (options ?? ToonOptions.Default).StrictMode);
        return new ToonDocument(toonValue);
    }

    /// <summary>
    ///     Converts a TOON document to a JSON string.
    /// </summary>
    /// <param name="document">
    ///     The TOON document to convert. Must not be null.
    /// </param>
    /// <param name="writerOptions">
    ///     Optional JSON writer options to control formatting.
    ///     If null, default options with no indentation will be used.
    /// </param>
    /// <returns>
    ///     A JSON string representation of the TOON document.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="document"/> is null.
    /// </exception>
    /// <remarks>
    ///     This method serializes the root value of the provided TOON document into a JSON string.
    ///     The caller can specify JSON writer options to control formatting, indentation, encoding, and other serialization behaviors.
    /// </remarks>
    public static string ToJson(ToonDocument document, JsonWriterOptions? writerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        return ToJson(document.Root, writerOptions);
    }

    /// <summary>
    ///     Converts a TOON value to a JSON string.
    /// </summary>
    /// <param name="value">
    ///     The TOON value to convert. Must not be null.
    /// </param>
    /// <param name="writerOptions">
    ///     Optional JSON writer options to control formatting.
    ///     If null, default options with no indentation will be used.
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
    ///     JSON writer options to control formatting, indentation, encoding, and other serialization behaviors.
    /// </remarks>
    public static string ToJson(ToonValue value, JsonWriterOptions? writerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        using var stream = new MemoryStream();

        using var writer = new Utf8JsonWriter(stream, writerOptions ?? DefaultWriterOptions);

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
    /// <param name="strict">Whether duplicate keys are an error (<see cref="ToonOptions.StrictMode"/>).</param>
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
    private static ToonValue ConvertJsonElementToToonValue(JsonElement element, bool strict)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object                          => ConvertJsonObject(element, strict),
            JsonValueKind.Array                           => ConvertJsonArray(element, strict),
            JsonValueKind.String                          => new ToonString(element.GetString()!),
            JsonValueKind.Number                          => ConvertJsonNumber(element),
            JsonValueKind.True                            => new ToonBoolean(true),
            JsonValueKind.False                           => new ToonBoolean(false),
            JsonValueKind.Null or JsonValueKind.Undefined => ToonNull.Instance,
            _                                             => throw new JsonException($"Unsupported JSON value kind: {element.ValueKind}")
        };
    }

    /// <summary>
    ///     Converts a JSON number with the same rules as the TOON decoder: the exact value when it fits in a
    ///     <see cref="decimal"/> (so large integers and decimals are not rounded through <see cref="double"/>),
    ///     otherwise the nearest <see cref="double"/>. A number that is not finite as a double (e.g. <c>1e400</c>)
    ///     is kept as a string, so no value is lost silently.
    /// </summary>
    private static ToonValue ConvertJsonNumber(JsonElement element)
    {
        var text = element.GetRawText();
        return ToonNumber.TryParse(text, out var number) ? number : new ToonString(text);
    }

    /// <summary>
    ///     Converts a JSON object to a TOON object.
    /// </summary>
    /// <param name="element">
    ///     The <see cref="JsonElement"/> representing the JSON object to convert.
    ///     Must be of type <see cref="JsonValueKind.Object"/>.
    /// </param>
    /// <param name="strict">Whether duplicate keys are an error (<see cref="ToonOptions.StrictMode"/>).</param>
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
    private static ToonObject ConvertJsonObject(JsonElement element, bool strict)
    {
        var toonObject = new ToonObject();

        foreach (var property in element.EnumerateObject())
        {
            var value = ConvertJsonElementToToonValue(property.Value, strict);

            // Duplicate keys are an error in strict mode, as in TOON and YAML, instead of silently dropping a value
            if (!toonObject.Properties.TryAdd(property.Name, value))
            {
                if (strict)
                {
                    throw new JsonException($"Duplicate key '{property.Name}' in a JSON object");
                }

                toonObject.Properties[property.Name] = value;
            }
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
    /// <param name="strict">Whether duplicate keys are an error (<see cref="ToonOptions.StrictMode"/>).</param>
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
    private static ToonArray ConvertJsonArray(JsonElement element, bool strict)
    {
        var items = new List<ToonValue>();

        foreach (var item in element.EnumerateArray())
        {
            items.Add(ConvertJsonElementToToonValue(item, strict));
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
                // Canonical TOON number text is also a valid JSON number, and keeps exact decimal values.
                if (n.IsFinite)
                {
                    writer.WriteRawValue(n.ToString(), skipInputValidation: true);
                }
                else
                {
                    writer.WriteNullValue(); // NaN and infinities, as in TOON (§3)
                }

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