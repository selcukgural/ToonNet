using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;
using ToonNet.Core.Models;

namespace ToonNet.Core.Serialization;

/// <summary>
///     Values that already are documents: a <see cref="ToonDocument"/> is written as its root, and System.Text.Json's
///     <see cref="JsonElement"/>, <see cref="JsonDocument"/> and <see cref="JsonNode"/> as the TOON values of their JSON
///     (numbers keep their digits); reading creates them from the TOON value. Without this they would be written as
///     objects of their properties (<c>Root:</c>, <c>ValueKind: Object</c>).
/// </summary>
internal static class ToonJsonInterop
{
    /// <summary>Whether values of the type are converted here.</summary>
    public static bool IsDocumentType(Type type) =>
        type == typeof(ToonDocument) || type == typeof(JsonElement) || type == typeof(JsonDocument) || typeof(JsonNode).IsAssignableFrom(type);

    /// <summary>Converts a document value to its TOON value.</summary>
    public static bool TryToToon(object value, out ToonValue result)
    {
        switch (value)
        {
            case ToonDocument document:
                result = document.Root;
                return true;
            case JsonElement element:
                result = FromElement(element);
                return true;
            case JsonDocument document:
                result = FromElement(document.RootElement);
                return true;
            case JsonNode node:
                result = FromNode(node);
                return true;
            default:
                result = null!;
                return false;
        }
    }

    /// <summary>Creates a document value of <paramref name="targetType"/> from a TOON value.</summary>
    /// <exception cref="ToonSerializationException">Thrown when the TOON value does not fit the target (an array for a JsonObject).</exception>
    public static bool TryFromToon(ToonValue value, Type targetType, out object? result)
    {
        if (targetType == typeof(ToonDocument))
        {
            result = new ToonDocument(value);
            return true;
        }

        if (targetType == typeof(JsonElement?) && value is ToonNull)
        {
            result = null;
            return true;
        }

        if (targetType == typeof(JsonElement) || targetType == typeof(JsonElement?))
        {
            using var document = JsonDocument.Parse(ToJson(value));
            result = document.RootElement.Clone();
            return true;
        }

        if (targetType == typeof(JsonDocument))
        {
            result = JsonDocument.Parse(ToJson(value));
            return true;
        }

        if (typeof(JsonNode).IsAssignableFrom(targetType))
        {
            var node = JsonNode.Parse(ToJson(value));

            result = node == null || targetType.IsInstanceOfType(node)
                ? node
                : throw new ToonSerializationException($"Cannot convert {value.ValueType} to {targetType.Name}") { TargetType = targetType, Value = value };

            return true;
        }

        result = null;
        return false;
    }

    private static ToonValue FromElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var obj = new ToonObject();

                foreach (var property in element.EnumerateObject())
                {
                    obj.Properties[property.Name] = FromElement(property.Value); // a repeated name keeps the last value
                }

                return obj;
            }
            case JsonValueKind.Array:
            {
                var array = new ToonArray(new List<ToonValue>(element.GetArrayLength()));

                foreach (var item in element.EnumerateArray())
                {
                    array.Items.Add(FromElement(item));
                }

                return array;
            }
            case JsonValueKind.String:
                return new ToonString(element.GetString()!);
            case JsonValueKind.Number:
                return Number(element.GetRawText());
            case JsonValueKind.True:
                return ToonBoolean.True;
            case JsonValueKind.False:
                return ToonBoolean.False;
            default:
                return ToonNull.Instance;
        }
    }

    private static ToonValue FromNode(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return ToonNull.Instance;
            case JsonObject obj:
            {
                var result = new ToonObject(obj.Count);

                foreach (var (name, value) in obj)
                {
                    result.Properties[name] = FromNode(value);
                }

                return result;
            }
            case JsonArray array:
            {
                var result = new ToonArray(new List<ToonValue>(array.Count));

                foreach (var item in array)
                {
                    result.Items.Add(FromNode(item));
                }

                return result;
            }
            default:
            {
                var value = node.AsValue();

                // A value parsed from JSON text holds a JsonElement: convert it as one (its number text decides)
                if (value.TryGetValue<JsonElement>(out var element))
                {
                    return FromElement(element);
                }

                // A value created from a CLR value can hold a NaN or an infinity, which has no JSON form (and no value
                // kind): TOON writes null (§3)
                if ((value.TryGetValue<double>(out var d) && !double.IsFinite(d)) || (value.TryGetValue<float>(out var f) && !float.IsFinite(f)))
                {
                    return ToonNull.Instance;
                }

                return node.GetValueKind() switch
                {
                    // A string, or a value written as one (a DateTime, Guid, char, ...): its JSON text decides
                    JsonValueKind.String => new ToonString(value.TryGetValue<string>(out var text) ? text : ReadJsonString(node.ToJsonString())),
                    JsonValueKind.Number => Number(node.ToJsonString()),
                    JsonValueKind.True => ToonBoolean.True,
                    JsonValueKind.False => ToonBoolean.False,
                    _ => ToonNull.Instance
                };
            }
        }
    }

    /// <summary>The string a JSON string literal stands for.</summary>
    private static string ReadJsonString(string json)
    {
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        return reader.GetString()!;
    }

    /// <summary>
    ///     A JSON number with its digits (as a decimal when it fits exactly, else the closest double); one that is not
    ///     finite as a double (<c>1e400</c>) is kept as a string, as <c>ToonConvert.FromJson</c> keeps it.
    /// </summary>
    private static ToonValue Number(string text) => ToonNumber.TryParse(text, out var number) ? number : new ToonString(text);

    /// <summary>The UTF-8 JSON text of a TOON value (numbers in their canonical form).</summary>
    private static byte[] ToJson(ToonValue value)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            Write(writer, value);
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void Write(Utf8JsonWriter writer, ToonValue value)
    {
        switch (value)
        {
            case ToonObject obj:
                writer.WriteStartObject();

                foreach (var (name, property) in obj.Properties)
                {
                    writer.WritePropertyName(name);
                    Write(writer, property);
                }

                writer.WriteEndObject();
                break;
            case ToonArray array:
                writer.WriteStartArray();

                foreach (var item in array.Items)
                {
                    Write(writer, item);
                }

                writer.WriteEndArray();
                break;
            case ToonString text:
                writer.WriteStringValue(text.Value);
                break;
            case ToonBoolean boolean:
                writer.WriteBooleanValue(boolean.Value);
                break;
            case ToonNumber number when double.IsFinite(number.Value):
                writer.WriteRawValue(number.ToString(), skipInputValidation: true);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }
}
