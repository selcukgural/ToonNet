using ToonNet.Core.Models;

namespace ToonNet.Core.Encoding;

/// <summary>
///     Writes a <see cref="ToonValue"/> tree with a <see cref="ToonWriter"/> in the canonical encoding of TOON spec
///     v3.3.2: arrays of uniform primitive-only objects use the tabular form, primitive arrays are written inline, other
///     arrays as expanded lists and empty arrays as <c>[]</c>.
/// </summary>
/// <remarks>
///     The serializer also uses it for values it does not write directly (converter output, <see cref="ToonValue"/>
///     properties), so it exposes the field, array and list-item steps, not only the root.
/// </remarks>
internal sealed class ToonDomWriter(ToonWriter writer)
{
    private readonly ToonWriter _writer = writer;

    /// <summary>
    ///     Writes the root value: an object as top-level fields, an array with a key-less header, or a single primitive
    ///     (spec §5).
    /// </summary>
    public void WriteRoot(ToonValue root)
    {
        switch (root)
        {
            case ToonObject obj:
                // An empty root object yields an empty document (§8)
                WriteFields(obj, 0);
                break;
            case ToonArray array:
                _writer.StartLine(0);
                _writer.Nesting = -1; // the root array is at depth 0, like the root object
                WriteArray(null, array, 1, ToonArrayPosition.Root);
                break;
            default:
                _writer.StartLine(0);
                _writer.BeginRootPrimitive();
                _writer.WritePrimitive(root);
                break;
        }
    }

    /// <summary>
    ///     Writes the fields of an object, one line per field at the given depth.
    /// </summary>
    public void WriteFields(ToonObject obj, int depth)
    {
        foreach (var (key, value) in obj.Properties)
        {
            _writer.StartLine(depth);
            WriteField(key, value, depth + 1);
        }
    }

    /// <summary>
    ///     Writes a field on the current line (after indentation or a list marker) and its nested content at
    ///     <paramref name="childDepth"/>.
    /// </summary>
    public void WriteField(string key, ToonValue value, int childDepth)
    {
        switch (value)
        {
            case ToonObject obj:
                _writer.WriteKey(key);
                _writer.Write(':');
                _writer.EnterNesting();
                WriteFields(obj, childDepth);
                _writer.ExitNesting();
                break;
            case ToonArray array:
                WriteArray(key, array, childDepth, ToonArrayPosition.Field);
                break;
            default:
                _writer.WriteKey(key);
                _writer.Write(": ");
                _writer.WritePrimitive(value);
                break;
        }
    }

    /// <summary>
    ///     Writes an array header (prefixed by the key, if any) and its items (spec §9).
    /// </summary>
    public void WriteArray(string? key, ToonArray array, int childDepth, ToonArrayPosition position)
    {
        var writer = _writer;

        if (array.Count == 0)
        {
            WriteEmptyArray(writer, key, position);
            return;
        }

        writer.EnterNesting();
        writer.WriteArrayHeader(key, array.Count);

        if (IsPrimitiveArray(array))
        {
            writer.Write(": ");

            for (var i = 0; i < array.Count; i++)
            {
                if (i > 0)
                {
                    writer.WriteDelimiter();
                }

                writer.WritePrimitive(array[i]);
            }
        }
        else if (position != ToonArrayPosition.ListItem && TryGetTabularFields(array) is { } fields)
        {
            WriteTabularRows(array, fields, childDepth);
        }
        else
        {
            writer.Write(':');

            foreach (var item in array.Items)
            {
                writer.StartLine(childDepth);
                WriteListItem(item, childDepth);
            }
        }

        writer.ExitNesting();
    }

    /// <summary>
    ///     Writes an empty array: <c>key: []</c> as a field, <c>[]</c> at the root and <c>[0]:</c> in a list item, where
    ///     inner arrays keep the header form (§9.1, §9.2).
    /// </summary>
    public static void WriteEmptyArray(ToonWriter writer, string? key, ToonArrayPosition position)
    {
        writer.CheckEmptyNesting();

        switch (position)
        {
            case ToonArrayPosition.Field:
                writer.WriteKey(key!);
                writer.Write(": []");
                break;
            case ToonArrayPosition.Root:
                writer.Write("[]");
                break;
            default:
                writer.WriteArrayHeader(key, 0);
                writer.Write(':');
                break;
        }
    }

    /// <summary>
    ///     Writes the field list of a tabular array followed by one row per object (spec §9.3).
    /// </summary>
    private void WriteTabularRows(ToonArray array, string[] fields, int rowDepth)
    {
        var writer = _writer;
        writer.WriteFieldList(fields);

        // Rows are objects one level below the array
        writer.EnterNesting();

        foreach (var item in array.Items)
        {
            var row = (ToonObject)item;
            writer.StartLine(rowDepth);

            for (var i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                {
                    writer.WriteDelimiter();
                }

                // Rows usually hold the fields in header order, so the entry at the same position is checked first
                ref readonly var entry = ref row.Properties.EntryAt(i);
                writer.WritePrimitive(string.Equals(entry.Key, fields[i]) ? entry.Value : row.Properties[fields[i]]);
            }
        }

        writer.ExitNesting();
    }

    /// <summary>
    ///     Writes a list item whose line has been started at <paramref name="depth"/> (spec §9.4, §10).
    /// </summary>
    public void WriteListItem(ToonValue item, int depth)
    {
        var writer = _writer;

        switch (item)
        {
            case ToonObject { Properties.Count: 0 }:
                // Empty object list item is a bare hyphen (§10)
                writer.CheckEmptyNesting();
                writer.Write('-');
                break;
            case ToonObject obj:
                writer.Write("- ");
                writer.EnterNesting();

                var isFirst = true;

                foreach (var (key, value) in obj.Properties)
                {
                    if (!isFirst)
                    {
                        writer.StartLine(depth + 1);
                    }

                    // The first field sits on the hyphen line; its nested content goes two levels deeper (§10)
                    WriteField(key, value, depth + 2);
                    isFirst = false;
                }

                writer.ExitNesting();
                break;
            case ToonArray array:
                writer.Write("- ");
                WriteArray(null, array, depth + 1, ToonArrayPosition.ListItem);
                break;
            default:
                writer.Write("- ");
                writer.WritePrimitive(item);
                break;
        }
    }

    /// <summary>
    ///     Returns the tabular field names when the array qualifies for tabular form (spec §9.3), otherwise null.
    /// </summary>
    /// <remarks>
    ///     Every item must be a non-empty object with the same key set and only primitive values.
    ///     The field order is the first object's key order.
    /// </remarks>
    private static string[]? TryGetTabularFields(ToonArray array)
    {
        if (array[0] is not ToonObject { Properties.Count: > 0 } first)
        {
            return null;
        }

        var fieldCount = first.Properties.Count;

        foreach (var item in array.Items)
        {
            if (item is not ToonObject obj || obj.Properties.Count != fieldCount)
            {
                return null;
            }

            foreach (var (key, value) in obj.Properties)
            {
                if (!first.Properties.ContainsKey(key) || value is ToonObject or ToonArray)
                {
                    return null;
                }
            }
        }

        var fields = new string[fieldCount];

        for (var i = 0; i < fields.Length; i++)
        {
            fields[i] = first.Properties.EntryAt(i).Key;
        }

        return fields;
    }

    private static bool IsPrimitiveArray(ToonArray array)
    {
        foreach (var item in array.Items)
        {
            if (item is ToonObject or ToonArray)
            {
                return false;
            }
        }

        return true;
    }
}
