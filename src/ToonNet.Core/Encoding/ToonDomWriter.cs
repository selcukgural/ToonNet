using ToonNet.Core.Models;

namespace ToonNet.Core.Encoding;

/// <summary>
///     Writes a <see cref="ToonValue"/> tree with a <see cref="ToonWriter"/> in the canonical encoding of TOON spec
///     v4.4: arrays of uniform objects use the tabular form (with nested field groups for nested uniform objects),
///     objects of uniform objects the keyed tabular form, primitive arrays are written inline, other arrays as expanded
///     lists and empty arrays as <c>[]</c>.
/// </summary>
/// <remarks>
///     The serializer also uses it for values it does not write directly (converter output, <see cref="ToonValue"/>
///     properties), so it exposes the field, array and list-item steps, not only the root.
/// </remarks>
internal sealed class ToonDomWriter(ToonWriter writer)
{
    private readonly ToonWriter _writer = writer;

    /// <summary>
    ///     Arrays shortened for a prompt, with the number of items left out, which a comment line after the array tells
    ///     (<c># 380 more items</c>).
    /// </summary>
    public IReadOnlyDictionary<ToonArray, int>? OmittedItems { get; init; }

    /// <summary>
    ///     Writes the root value: an object as top-level fields, an array with a key-less header, or a single primitive
    ///     (spec §5).
    /// </summary>
    public void WriteRoot(ToonValue root)
    {
        switch (root)
        {
            case ToonObject obj when TryGetKeyedColumns(obj) is { } columns:
                _writer.StartLine(0);
                _writer.Nesting = -1; // the root object is at depth 0
                WriteKeyed(null, obj, columns, 1);
                break;
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
            case ToonObject obj when TryGetKeyedColumns(obj) is { } columns:
                WriteKeyed(key, obj, columns, childDepth);
                break;
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
        else if (position != ToonArrayPosition.ListItem && ToonColumns.TryGetColumns(array.Items) is { } columns)
        {
            WriteTabularRows(array, columns, childDepth);
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

        if (OmittedItems != null && OmittedItems.TryGetValue(array, out var omitted))
        {
            // A comment line, which parsers drop (§5.1), at the depth of the items
            writer.StartLine(childDepth);
            writer.Write(omitted == 1 ? "# 1 more item" : $"# {omitted.ToString(System.Globalization.CultureInfo.InvariantCulture)} more items");
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
    private void WriteTabularRows(ToonArray array, ToonColumn[] columns, int rowDepth)
    {
        var writer = _writer;
        writer.WriteFieldList(columns);

        // Rows are objects one level below the array, nested field groups further below
        var levels = 1 + ToonColumns.GroupDepth(columns);

        for (var i = 0; i < levels; i++)
        {
            writer.EnterNesting();
        }

        foreach (var item in array.Items)
        {
            writer.StartLine(rowDepth);
            WriteCells((ToonObject)item, columns, 0, columns.Length, true);
        }

        writer.Nesting -= levels;
    }

    /// <summary>
    ///     Writes an object whose values are uniform objects as a keyed tabular object (spec §9.5): the header, then one
    ///     <c>entrykey: cells</c> row per entry.
    /// </summary>
    private void WriteKeyed(string? key, ToonObject obj, ToonColumn[] columns, int rowDepth)
    {
        var writer = _writer;
        writer.EnterNesting();
        writer.WriteKeyedHeader(key, obj.Properties.Count);
        writer.WriteFieldList(columns);

        var levels = 1 + ToonColumns.GroupDepth(columns);

        for (var i = 0; i < levels; i++)
        {
            writer.EnterNesting();
        }

        foreach (var (entryKey, value) in obj.Properties)
        {
            writer.StartLine(rowDepth);
            writer.WriteKey(entryKey);
            writer.Write(": ");
            WriteCells((ToonObject)value, columns, 0, columns.Length, true);
        }

        writer.Nesting -= levels + 1;
    }

    /// <summary>
    ///     Writes the cells of the columns from <paramref name="start"/> to <paramref name="end"/>, joined by the
    ///     delimiter, in depth-first order (nested field groups expanded in place).
    /// </summary>
    /// <returns>Whether no cell was written (the next cell needs no delimiter).</returns>
    private bool WriteCells(ToonObject row, ToonColumn[] columns, int start, int end, bool first)
    {
        var writer = _writer;
        var position = 0;

        for (var i = start; i < end; i = columns[i].End, position++)
        {
            var value = ToonColumns.ValueOf(row, columns[i].Name, position);

            if (columns[i].IsGroupAt(i))
            {
                first = WriteCells((ToonObject)value!, columns, i + 1, columns[i].End, first);
                continue;
            }

            if (!first)
            {
                writer.WriteDelimiter();
            }

            first = false;
            writer.WritePrimitive(value);
        }

        return first;
    }

    /// <summary>
    ///     Returns the columns when an object qualifies for the keyed tabular form (spec §9.5): at least two entries,
    ///     whose values are uniform objects.
    /// </summary>
    private static ToonColumn[]? TryGetKeyedColumns(ToonObject obj)
    {
        var properties = obj.Properties;

        if (properties.Count < 2)
        {
            return null;
        }

        // Most objects have a primitive or array value, which rules the form out before collecting anything
        foreach (var (_, value) in properties)
        {
            if (value is not ToonObject { Properties.Count: > 0 })
            {
                return null;
            }
        }

        var values = new ToonValue[properties.Count];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = properties.EntryAt(i).Value;
        }

        return ToonColumns.TryGetColumns(values);
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
