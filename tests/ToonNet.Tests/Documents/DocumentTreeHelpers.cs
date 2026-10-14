using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Tests;

/// <summary>
///     Writes a tree with ToonDocumentWriter in the forms ToonEncoder picks (tables with nested field groups, which the
///     writer has no method for, go through WriteValue).
/// </summary>
internal static class DocumentTreeWriter
{
    public static void WriteRoot(ToonDocumentWriter writer, ToonValue value)
    {
        switch (value)
        {
            case ToonObject obj when KeyedFields(obj) is { } fields:
                writer.WriteStartKeyedTable(obj.Properties.Count, fields);
                WriteKeyedRows(writer, obj, fields);
                writer.WriteEndArray();
                break;
            case ToonObject obj when HasGroupedKeyedColumns(obj):
                writer.WriteValue(obj);
                break;
            case ToonObject obj:
                writer.WriteStartObject();
                WriteFields(writer, obj);
                writer.WriteEndObject();
                break;
            case ToonArray array:
                WriteArray(writer, null, array, listItem: false);
                break;
            default:
                WritePrimitive(writer, value);
                break;
        }
    }

    private static void WriteFields(ToonDocumentWriter writer, ToonObject obj)
    {
        foreach (var (key, value) in obj.Properties)
        {
            switch (value)
            {
                case ToonObject inner when KeyedFields(inner) is { } fields:
                    writer.WriteStartKeyedTable(key, inner.Properties.Count, fields);
                    WriteKeyedRows(writer, inner, fields);
                    writer.WriteEndArray();
                    break;
                case ToonObject inner when HasGroupedKeyedColumns(inner):
                    writer.WriteValue(key, inner);
                    break;
                case ToonObject inner:
                    writer.WriteStartObject(key);
                    WriteFields(writer, inner);
                    writer.WriteEndObject();
                    break;
                case ToonArray array:
                    WriteArray(writer, key, array, listItem: false);
                    break;
                case ToonString s:
                    writer.WriteString(key, s.Value);
                    break;
                case ToonBoolean b:
                    writer.WriteBoolean(key, b.Value);
                    break;
                case ToonNull:
                    writer.WriteNull(key);
                    break;
                default:
                    writer.WriteValue(key, value); // numbers keep the exact form of their ToonNumber
                    break;
            }
        }
    }

    private static void WriteArray(ToonDocumentWriter writer, string? key, ToonArray array, bool listItem)
    {
        if (array.Items.All(i => i is not (ToonObject or ToonArray)))
        {
            if (key == null)
            {
                writer.WriteStartArray(array.Count);
            }
            else
            {
                writer.WriteStartArray(key, array.Count);
            }

            foreach (var item in array.Items)
            {
                WritePrimitive(writer, item);
            }

            writer.WriteEndArray();
            return;
        }

        var columns = listItem ? null : ToonColumns.TryGetColumns(array.Items);

        if (columns != null && columns.All(c => !c.IsGroupAt(Array.IndexOf(columns, c))))
        {
            var fields = columns.Select(c => c.Name).ToArray();

            if (key == null)
            {
                writer.WriteStartTable(array.Count, fields);
            }
            else
            {
                writer.WriteStartTable(key, array.Count, fields);
            }

            foreach (ToonObject row in array.Items)
            {
                foreach (var field in fields)
                {
                    WritePrimitive(writer, row.Properties[field]);
                }
            }

            writer.WriteEndArray();
            return;
        }

        if (columns != null)
        {
            // Nested field groups: no writer method; the tree writer writes the encoder's form
            if (key == null)
            {
                writer.WriteValue(array);
            }
            else
            {
                writer.WriteValue(key, array);
            }

            return;
        }

        if (key == null)
        {
            writer.WriteStartList(array.Count);
        }
        else
        {
            writer.WriteStartList(key, array.Count);
        }

        foreach (var item in array.Items)
        {
            switch (item)
            {
                case ToonObject obj:
                    writer.WriteStartObject();
                    WriteFields(writer, obj);
                    writer.WriteEndObject();
                    break;
                case ToonArray inner:
                    WriteArray(writer, null, inner, listItem: true);
                    break;
                default:
                    WritePrimitive(writer, item);
                    break;
            }
        }

        writer.WriteEndArray();
    }

    private static void WriteKeyedRows(ToonDocumentWriter writer, ToonObject obj, string[] fields)
    {
        foreach (var (key, value) in obj.Properties)
        {
            writer.WriteRowKey(key);

            foreach (var field in fields)
            {
                WritePrimitive(writer, ((ToonObject)value).Properties[field]);
            }
        }
    }

    private static void WritePrimitive(ToonDocumentWriter writer, ToonValue value)
    {
        switch (value)
        {
            case ToonString s:
                writer.WriteStringValue(s.Value);
                break;
            case ToonBoolean b:
                writer.WriteBooleanValue(b.Value);
                break;
            case ToonNull:
                writer.WriteNullValue();
                break;
            default:
                writer.WriteValue(value);
                break;
        }
    }

    private static ToonColumn[]? KeyedColumns(ToonObject obj) =>
        obj.Properties.Count >= 2 && obj.Properties.Values.All(v => v is ToonObject { Properties.Count: > 0 })
            ? ToonColumns.TryGetColumns(obj.Properties.Values.ToList())
            : null;

    private static string[]? KeyedFields(ToonObject obj) =>
        KeyedColumns(obj) is { } columns && columns.Select((c, i) => c.IsGroupAt(i)).All(group => !group) ? columns.Select(c => c.Name).ToArray() : null;

    private static bool HasGroupedKeyedColumns(ToonObject obj) => KeyedColumns(obj) != null;
}

/// <summary>Builds a tree from ToonDocumentReader tokens.</summary>
internal static class DocumentTreeReader
{
    /// <summary>Reads the value the reader is on (a start token or a primitive).</summary>
    public static ToonValue ReadValue(ToonDocumentReader reader)
    {
        switch (reader.TokenType)
        {
            case ToonTokenType.StartObject:
            {
                var obj = new ToonObject();

                while (reader.Read() && reader.TokenType != ToonTokenType.EndObject)
                {
                    Assert.Equal(ToonTokenType.PropertyName, reader.TokenType);
                    var key = reader.GetPropertyName();
                    Assert.True(reader.Read());
                    obj.Properties[key] = ReadValue(reader);
                }

                return obj;
            }
            case ToonTokenType.StartArray:
            {
                _ = reader.ArrayLength; // the declared length; non-strict mode accepts another number of items (§14.4)
                var array = new ToonArray();

                while (reader.Read() && reader.TokenType != ToonTokenType.EndArray)
                {
                    array.Items.Add(ReadValue(reader));
                }

                return array;
            }
            case ToonTokenType.String:
                return new ToonString(reader.GetString());
            case ToonTokenType.Number:
                return reader.TryGetDecimal(out var exact) ? new ToonNumber(exact, reader.GetDouble()) : new ToonNumber(reader.GetDouble());
            case ToonTokenType.True:
            case ToonTokenType.False:
                return new ToonBoolean(reader.GetBoolean());
            case ToonTokenType.Null:
                return ToonNull.Instance;
            default:
                throw new InvalidOperationException($"Unexpected {reader.TokenType}");
        }
    }
}
