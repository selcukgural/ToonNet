using ToonNet.Core.Models;

namespace ToonNet.Core.Encoding;

/// <summary>
///     A column of a tabular array or keyed tabular object (spec §9.3, §9.5). Columns are stored in pre-order: a
///     nested-uniform column (a nested field group) is followed by its subcolumns, and <see cref="End"/> is the index
///     after them; a column whose <see cref="End"/> is the next index holds primitives.
/// </summary>
/// <param name="Name">The field name.</param>
/// <param name="End">The index after the column and its subcolumns.</param>
internal readonly record struct ToonColumn(string Name, int End)
{
    /// <summary>Whether the column at <paramref name="index"/> is a nested field group.</summary>
    public bool IsGroupAt(int index) => End > index + 1;
}

/// <summary>Detects the columns of <see cref="ToonObject"/> values for the tabular forms.</summary>
internal static class ToonColumns
{
    /// <summary>
    ///     Returns the columns shared by <paramref name="objects"/> when they qualify for a tabular form: every value is
    ///     a non-empty object, all have the same key set, and every column is uniform-primitive or nested-uniform (its
    ///     values are objects that qualify in turn). Field order is the first object's key order.
    /// </summary>
    public static ToonColumn[]? TryGetColumns(IReadOnlyList<ToonValue> objects)
    {
        var columns = new List<ToonColumn>();
        return AddColumns(columns, objects) ? columns.ToArray() : null;
    }

    /// <summary>The number of nested field group levels below the top-level columns.</summary>
    public static int GroupDepth(ReadOnlySpan<ToonColumn> columns)
    {
        var depth = 0;
        Measure(columns, 0, columns.Length, 0, ref depth);
        return depth;

        static void Measure(ReadOnlySpan<ToonColumn> columns, int start, int end, int level, ref int depth)
        {
            for (var i = start; i < end; i = columns[i].End)
            {
                if (columns[i].IsGroupAt(i))
                {
                    depth = Math.Max(depth, level + 1);
                    Measure(columns, i + 1, columns[i].End, level + 1, ref depth);
                }
            }
        }
    }

    /// <summary>
    ///     The value of <paramref name="key"/> in <paramref name="obj"/>, checking the entry at
    ///     <paramref name="position"/> first (objects usually hold their keys in the same order).
    /// </summary>
    public static ToonValue? ValueOf(ToonObject obj, string key, int position)
    {
        var properties = obj.Properties;

        if (position < properties.Count)
        {
            ref readonly var entry = ref properties.EntryAt(position);

            if (string.Equals(entry.Key, key))
            {
                return entry.Value;
            }
        }

        return properties.TryGetValue(key, out var value) ? value : null;
    }

    private static bool AddColumns(List<ToonColumn> columns, IReadOnlyList<ToonValue> objects)
    {
        if (objects[0] is not ToonObject { Properties.Count: > 0 } first)
        {
            return false;
        }

        var keyCount = first.Properties.Count;

        foreach (var item in objects)
        {
            if (item is not ToonObject obj || obj.Properties.Count != keyCount)
            {
                return false;
            }
        }

        for (var k = 0; k < keyCount; k++)
        {
            var key = first.Properties.EntryAt(k).Key;
            var allObjects = true;
            var allPrimitives = true;

            // The objects have as many keys as the first one, so finding all of its keys means the key sets are equal
            foreach (var item in objects)
            {
                switch (ValueOf((ToonObject)item, key, k))
                {
                    case null or ToonArray or ToonObject { Properties.Count: 0 }:
                        return false;
                    case ToonObject:
                        allPrimitives = false;
                        break;
                    default:
                        allObjects = false;
                        break;
                }

                if (!allObjects && !allPrimitives)
                {
                    return false; // a column mixing objects and primitives
                }
            }

            var index = columns.Count;
            columns.Add(default);

            if (allObjects)
            {
                var groupValues = new ToonValue[objects.Count];

                for (var i = 0; i < groupValues.Length; i++)
                {
                    groupValues[i] = ValueOf((ToonObject)objects[i], key, k)!;
                }

                if (!AddColumns(columns, groupValues))
                {
                    return false;
                }
            }

            columns[index] = new ToonColumn(key, columns.Count);
        }

        return true;
    }
}
