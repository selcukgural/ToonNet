using System.Buffers;
using System.Collections;
using System.Runtime.InteropServices;
using ToonNet.Core.Encoding;

namespace ToonNet.Core.Serialization;

// Nested field groups (spec §9.3) and keyed tabular objects (§9.5) of typed objects, written without building the
// tree: a plan of the columns is derived from the objects, the same columns the encoder finds in the tree. Shapes the
// plan cannot decide (converters, ToonValue values, dictionaries with custom comparers, objects of different types) go to
// the tree.
public static partial class ToonSerializer
{
    private sealed partial class DirectWriter
    {
        /// <summary>Whether objects qualify for a tabular form, as far as the plan can tell.</summary>
        private enum PlanResult
        {
            Tabular,
            NotTabular,

            /// <summary>Only the tree knows.</summary>
            Unknown,

            /// <summary>A column every object leaves out (null values with <see cref="ToonSerializerOptions.IgnoreNullValues"/>).</summary>
            Absent
        }

        /// <summary>
        ///     The columns of a tabular array or keyed object of typed objects, in pre-order like <see cref="ToonColumn"/>,
        ///     with where each column's values come from.
        /// </summary>
        private sealed class TablePlan
        {
            public readonly List<ToonColumn> Columns = [];
            public readonly List<PlanColumn> Sources = [];

            /// <summary>The number of nested field group levels.</summary>
            public int GroupDepth;

            /// <summary>The dictionary entries columns come from (by <see cref="PlanColumn.Property"/>), when some do.</summary>
            public List<(DictionaryEntryReader Reader, object Key)>? Entries;
        }

        /// <summary>
        ///     Where a column's values come from: a property of the objects at its level, or (with neither
        ///     <paramref name="Metadata"/> nor <paramref name="Writer"/>) the dictionary entry at <paramref name="Property"/>
        ///     in <see cref="TablePlan.Entries"/>.
        /// </summary>
        /// <param name="Property">The index of the property (or of the entry).</param>
        /// <param name="Metadata">The property, for objects with reflection metadata.</param>
        /// <param name="Writer">The generated writer of the objects, for objects with generated code.</param>
        /// <param name="ReadAsObject">For a cell: write the value read as an object rather than the typed property.</param>
        private readonly record struct PlanColumn(int Property, PropertyMetadata? Metadata, ToonObjectWriter? Writer, bool ReadAsObject);

        /// <summary>The value of a column in an object of its level.</summary>
        private static object? ValueOf(TablePlan plan, in PlanColumn source, object item)
        {
            if (source.Metadata is { } property)
            {
                return property.Getter(item);
            }

            if (source.Writer is { } writer)
            {
                return writer.GetValueOf(item, source.Property);
            }

            var (reader, key) = plan.Entries![source.Property];
            return reader.TryGetValue(item, key, out var value) ? value : null;
        }

        /// <summary>Plans the tabular form for collection items (all objects of one type).</summary>
        private PlanResult PlanRows(PooledItems items, DirectKind[] kinds, DirectTypeInfo?[] infos, out TablePlan plan)
        {
            plan = new TablePlan();
            var type = items[0]?.GetType();

            for (var i = 0; i < items.Count; i++)
            {
                if (kinds[i] != kinds[0] || kinds[i] is not (DirectKind.Object or DirectKind.Generated or DirectKind.Dictionary) || items[i]!.GetType() != type)
                {
                    return kinds[i] is DirectKind.Null or DirectKind.Primitive or DirectKind.Collection ? PlanResult.NotTabular : PlanResult.Unknown;
                }
            }

            return AddColumns(plan, items.Span, kinds[0], infos[0]!, 1);
        }

        /// <summary>
        ///     Plans the keyed tabular form for an object, dictionary or generated object whose written values may all be
        ///     objects (<see cref="MayBeKeyed"/>).
        /// </summary>
        private PlanResult PlanKeyed(object value, DirectKind kind, DirectTypeInfo info, out List<KeyValuePair<string, object>> entries,
                                     out TablePlan plan)
        {
            plan = new TablePlan();
            entries = new List<KeyValuePair<string, object>>(kind switch
            {
                DirectKind.Dictionary => (value as ICollection)?.Count ?? 0,
                DirectKind.Generated => info.Writer!.PropertyCount,
                _ => info.Metadata!.Properties.Length
            });

            switch (kind)
            {
                // A converter for an entry's declared type (which the tree consults) is left to the tree. The generated
                // writer does not tell its declared types, but the converter check covers what its code writes itself
                case DirectKind.Dictionary when _hasConverters && IsConvertedDeclared(info.Entries!.ValueType):
                case DirectKind.Generated when _hasConverters && !_converterCheck!.AllowsGeneratedCode(value.GetType()):
                    return PlanResult.Unknown;
                case DirectKind.Dictionary:
                    foreach (var (rawKey, entryValue) in info.Entries!.Read(value))
                    {
                        if (Narrows(entryValue, info.Entries.ValueType))
                        {
                            return PlanResult.Unknown; // written as its declared type, which the tree decides with
                        }

                        if (!AddEntry(entries, FormatDictionaryKey(rawKey), entryValue))
                        {
                            return PlanResult.NotTabular;
                        }
                    }

                    break;
                case DirectKind.Generated:
                {
                    var writer = info.Writer!;

                    for (var p = 0; p < writer.PropertyCount; p++)
                    {
                        if (!_includeReadOnly && writer.IsReadOnlyProperty(p))
                        {
                            continue;
                        }

                        if (writer.GetPropertyKind(p) == ToonPropertyKind.Dynamic)
                        {
                            return PlanResult.Unknown; // a converter or runtime-typed value
                        }

                        var propertyValue = writer.GetValueOf(value, p);

                        // Code from generators before 4.0 does not tell the declared type: any object that may be derived
                        // is left to the tree
                        if (writer.GetPropertyType(p) is { } declared ? Narrows(propertyValue, declared) : propertyValue is { } v && !v.GetType().IsSealed)
                        {
                            return PlanResult.Unknown;
                        }

                        if (!AddEntry(entries, writer.GetName(p, _options), propertyValue))
                        {
                            return PlanResult.NotTabular;
                        }
                    }

                    break;
                }
                default:
                {
                    var names = GetDistinctPropertyNames(info.Metadata!, _options)!;
                    var properties = info.Metadata!.Properties;

                    for (var p = 0; p < properties.Length; p++)
                    {
                        if (!_includeReadOnly && !properties[p].HasSetter)
                        {
                            continue;
                        }

                        if (properties[p].Converter != null || IsConvertedProperty(properties[p]))
                        {
                            return PlanResult.Unknown;
                        }

                        var propertyValue = properties[p].Getter(value);

                        if (Narrows(propertyValue, properties[p].Property.PropertyType))
                        {
                            return PlanResult.Unknown;
                        }

                        if (!AddEntry(entries, names[p], propertyValue))
                        {
                            return PlanResult.NotTabular;
                        }
                    }

                    break;
                }
            }

            if (entries.Count < 2)
            {
                return PlanResult.NotTabular;
            }

            // Entry keys must be distinct, as in the tree (generated objects can share names under a naming policy)
            if (kind == DirectKind.Generated && entries.Select(e => e.Key).Distinct(StringComparer.Ordinal).Count() != entries.Count)
            {
                return PlanResult.Unknown;
            }

            var values = ArrayPool<object?>.Shared.Rent(entries.Count);

            try
            {
                var valueKind = DirectKind.Null;
                DirectTypeInfo? valueInfo = null;
                Type? valueType = null;

                for (var i = 0; i < entries.Count; i++)
                {
                    var entryValue = values[i] = entries[i].Value;
                    var entryKind = Classify(entryValue, entryValue.GetType(), null, out var entryInfo);

                    if (entryKind is DirectKind.Primitive or DirectKind.Collection)
                    {
                        return PlanResult.NotTabular;
                    }

                    if (entryKind is not (DirectKind.Object or DirectKind.Generated or DirectKind.Dictionary) ||
                        (valueType != null && entryValue.GetType() != valueType))
                    {
                        return PlanResult.Unknown;
                    }

                    valueKind = entryKind;
                    valueInfo = entryInfo;
                    valueType = entryValue.GetType();
                }

                return AddColumns(plan, values.AsSpan(0, entries.Count), valueKind, valueInfo!, 1);
            }
            finally
            {
                ArrayPool<object?>.Shared.Return(values, clearArray: true);
            }
        }

        /// <summary>Adds an entry with a written value; returns false for a null value that is written (a primitive).</summary>
        /// <summary>Whether a value is written as a type other than its runtime type (its declared type).</summary>
        private bool Narrows(object? value, Type declaredType) =>
            value != null && value.GetType() != declaredType && WrittenType(declaredType, value.GetType(), _options) != value.GetType();

        private bool AddEntry(List<KeyValuePair<string, object>> entries, string key, object? value)
        {
            if (value != null)
            {
                entries.Add(new KeyValuePair<string, object>(key, value));
                return true;
            }

            return _ignoreNulls;
        }

        /// <summary>
        ///     Adds the columns of <paramref name="objects"/> (all of one type, with <paramref name="info"/>, none null) at
        ///     nesting <paramref name="level"/>: a column per written property, a nested field group for properties whose
        ///     values are all objects of one type.
        /// </summary>
        private PlanResult AddColumns(TablePlan plan, ReadOnlySpan<object?> objects, DirectKind kind, DirectTypeInfo info, int level)
        {
            if (level > _options.ValidatedMaxDepth)
            {
                return PlanResult.Unknown; // the tree reports the error
            }

            // The values of one non-primitive property at a time, rented when the first one is read
            object?[]? rented = null;

            try
            {
                return AddColumns(plan, objects, kind, info, level, ref rented);
            }
            finally
            {
                if (rented != null)
                {
                    ArrayPool<object?>.Shared.Return(rented, clearArray: true);
                }
            }
        }

        private PlanResult AddColumns(TablePlan plan, ReadOnlySpan<object?> objects, DirectKind kind, DirectTypeInfo info, int level,
                                      ref object?[]? rented)
        {
            var first = plan.Columns.Count;

            if (kind == DirectKind.Dictionary)
            {
                var result = AddEntryColumns(plan, objects, info.Entries!, level, ref rented);

                if (result != PlanResult.Tabular)
                {
                    return result;
                }
            }
            else if (kind == DirectKind.Object)
            {
                var metadata = info.Metadata!;
                var names = GetDistinctPropertyNames(metadata, _options);
                var properties = metadata.Properties;

                if (names == null)
                {
                    return PlanResult.Unknown;
                }

                for (var p = 0; p < properties.Length; p++)
                {
                    var property = properties[p];

                    if (!_includeReadOnly && !property.HasSetter)
                    {
                        continue;
                    }

                    if (property.Converter != null)
                    {
                        return PlanResult.Unknown;
                    }

                    PlanResult result;

                    if (property.Primitive is { } primitive && !IsConvertedProperty(property))
                    {
                        var absent = 0;

                        if (_ignoreNulls)
                        {
                            foreach (var item in objects)
                            {
                                absent += primitive.IsNull(item!) ? 1 : 0;
                            }
                        }

                        result = absent == 0 ? AddCell(plan, names[p], new PlanColumn(p, property, null, false))
                            : absent == objects.Length ? PlanResult.Absent : PlanResult.NotTabular;
                    }
                    else
                    {
                        var values = (rented ??= ArrayPool<object?>.Shared.Rent(objects.Length)).AsSpan(0, objects.Length);

                        for (var i = 0; i < objects.Length; i++)
                        {
                            values[i] = property.Getter(objects[i]!);
                        }

                        result = AddColumn(plan, names[p], values, property.Property.PropertyType, new PlanColumn(p, property, null, true), level);
                    }

                    if (result is PlanResult.NotTabular or PlanResult.Unknown)
                    {
                        return result;
                    }
                }
            }
            else
            {
                var writer = info.Writer!;
                var count = writer.PropertyCount;
                var seen = new HashSet<string>(StringComparer.Ordinal);

                // The row shapes tell which primitive properties are written (null ones are left out with IgnoreNullValues)
                var shapeArray = ArrayPool<bool>.Shared.Rent(objects.Length * count);

                try
                {
                    var shapes = shapeArray.AsSpan(0, objects.Length * count);
                    shapes.Clear();

                    for (var i = 0; i < objects.Length; i++)
                    {
                        if (writer.GetRowShapeOf(objects[i]!, _writer, shapes.Slice(i * count, count)) == ToonRowShape.NotRow)
                        {
                            return PlanResult.NotTabular;
                        }
                    }

                    for (var p = 0; p < count; p++)
                    {
                        if (!_includeReadOnly && writer.IsReadOnlyProperty(p))
                        {
                            continue;
                        }

                        var name = writer.GetName(p, _options);

                        if (!seen.Add(name))
                        {
                            return PlanResult.Unknown; // properties sharing a name: the tree keeps the last value
                        }

                        PlanResult result;
                        var propertyKind = writer.GetPropertyKind(p);

                        if (propertyKind == ToonPropertyKind.Dynamic)
                        {
                            return PlanResult.Unknown; // a converter or runtime-typed value
                        }

                        if (propertyKind == ToonPropertyKind.Primitive)
                        {
                            var absent = 0;

                            for (var i = 0; i < objects.Length; i++)
                            {
                                absent += shapes[i * count + p] ? 0 : 1;
                            }

                            result = absent == 0 ? AddCell(plan, name, new PlanColumn(p, null, writer, false))
                                : absent == objects.Length ? PlanResult.Absent : PlanResult.NotTabular;
                        }
                        else
                        {
                            // The declared type, which a converter may match, is not known here; no converter applies to the
                            // declared types of what the generated code writes itself when the converter check allows it
                            if (_hasConverters && !_converterCheck!.AllowsGeneratedCode(objects[0]!.GetType()))
                            {
                                return PlanResult.Unknown;
                            }

                            var values = (rented ??= ArrayPool<object?>.Shared.Rent(objects.Length)).AsSpan(0, objects.Length);

                            for (var i = 0; i < objects.Length; i++)
                            {
                                values[i] = writer.GetValueOf(objects[i]!, p);
                            }

                            result = AddColumn(plan, name, values, typeof(object), new PlanColumn(p, null, writer, true), level);
                        }

                        if (result is PlanResult.NotTabular or PlanResult.Unknown)
                        {
                            return result;
                        }
                    }
                }
                finally
                {
                    ArrayPool<bool>.Shared.Return(shapeArray);
                }
            }

            // Objects without written properties are empty objects, which rule the tabular form out
            return plan.Columns.Count > first ? PlanResult.Tabular : PlanResult.NotTabular;
        }

        /// <summary>
        ///     Adds a column per entry of <paramref name="dictionaries"/> (all of one type): they qualify when they write
        ///     the same key names, in the order of the first one, as the encoder finds the key sets equal in the tree.
        /// </summary>
        /// <remarks>
        ///     Dictionary types here format distinct keys to distinct names, so equal name sets mean equal keys, and each
        ///     dictionary finds the entry of a key of the first one, whatever its comparer.
        /// </remarks>
        private PlanResult AddEntryColumns(TablePlan plan, ReadOnlySpan<object?> dictionaries, DictionaryEntryReader entries, int level,
                                           ref object?[]? rented)
        {
            // A converter may leave entries out of the tree
            if (_hasConverters)
            {
                return PlanResult.Unknown;
            }

            var keys = new List<KeyValuePair<object, string>>();
            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (var (rawKey, value) in entries.Read(dictionaries[0]!))
            {
                if (value != null || !_ignoreNulls)
                {
                    var name = FormatDictionaryKey(rawKey);
                    keys.Add(new KeyValuePair<object, string>(rawKey, name));
                    names.Add(name);
                }
            }

            // An empty object rules the tabular form out, and so do key sets that differ
            if (keys.Count == 0)
            {
                return PlanResult.NotTabular;
            }

            foreach (var dictionary in dictionaries)
            {
                if (!entries.CanLookUp(dictionary!))
                {
                    return PlanResult.Unknown;
                }

                var count = 0;

                foreach (var (rawKey, value) in entries.Read(dictionary!))
                {
                    if (value == null && _ignoreNulls)
                    {
                        continue;
                    }

                    if (!names.Contains(FormatDictionaryKey(rawKey)))
                    {
                        return PlanResult.NotTabular;
                    }

                    count++;
                }

                if (count != keys.Count)
                {
                    return PlanResult.NotTabular;
                }
            }

            foreach (var (rawKey, name) in keys)
            {
                var values = (rented ??= ArrayPool<object?>.Shared.Rent(dictionaries.Length)).AsSpan(0, dictionaries.Length);

                for (var i = 0; i < dictionaries.Length; i++)
                {
                    if (!entries.TryGetValue(dictionaries[i]!, rawKey, out values[i]) || (values[i] is null && _ignoreNulls))
                    {
                        return PlanResult.NotTabular; // the key sets differ
                    }
                }

                // Entry values are written by their runtime type
                (plan.Entries ??= []).Add((entries, rawKey));
                var result = AddColumn(plan, name, values, typeof(object), new PlanColumn(plan.Entries.Count - 1, null, null, true), level);

                if (result is PlanResult.NotTabular or PlanResult.Unknown)
                {
                    return result;
                }
            }

            return PlanResult.Tabular;
        }

        private static PlanResult AddCell(TablePlan plan, string name, PlanColumn source)
        {
            plan.Columns.Add(new ToonColumn(name, plan.Columns.Count + 1));
            plan.Sources.Add(source);
            return PlanResult.Tabular;
        }

        /// <summary>
        ///     Adds the column of one property from its values: primitives (or nulls) are cells, objects of one type a
        ///     nested field group; anything else rules the form out or is left to the tree.
        /// </summary>
        private PlanResult AddColumn(TablePlan plan, string name, ReadOnlySpan<object?> values, Type declaredType, PlanColumn source, int level)
        {
            int absent = 0, primitives = 0;
            DirectKind childKind = default;
            DirectTypeInfo? childInfo = null;
            Type? childType = null;

            foreach (var value in values)
            {
                // The declared type too: the tree uses a converter for it
                var kind = Classify(value, declaredType, null, out var info);

                switch (kind)
                {
                    case DirectKind.Null:
                        if (_ignoreNulls)
                        {
                            absent++;
                        }
                        else
                        {
                            primitives++;
                        }

                        break;
                    case DirectKind.Primitive:
                        primitives++;
                        break;
                    case DirectKind.Collection:
                        return PlanResult.NotTabular;
                    case DirectKind.Object or DirectKind.Generated or DirectKind.Dictionary when childType == null || value!.GetType() == childType:
                        childType = value!.GetType();
                        childKind = kind;
                        childInfo = info;
                        break;
                    default:
                        return PlanResult.Unknown; // converters, ToonValue values, objects of different types
                }
            }

            if (absent == values.Length)
            {
                return PlanResult.Absent;
            }

            if (absent > 0 || (childType != null && primitives > 0))
            {
                return PlanResult.NotTabular; // the key sets differ, or a column mixes objects and primitives
            }

            if (childType == null)
            {
                return AddCell(plan, name, source);
            }

            var index = plan.Columns.Count;
            plan.Columns.Add(default);
            plan.Sources.Add(source);

            // The values are the objects of the group (none is null); their own columns are planned with other buffers
            var result = AddColumns(plan, values, childKind, childInfo!, level + 1);

            if (result != PlanResult.Tabular)
            {
                return result;
            }

            plan.Columns[index] = new ToonColumn(name, plan.Columns.Count);
            plan.GroupDepth = Math.Max(plan.GroupDepth, level);
            return PlanResult.Tabular;
        }

        /// <summary>Writes the header and rows of a planned tabular array (spec §9.3).</summary>
        private void WritePlannedRows(string? key, PooledItems items, TablePlan plan, int rowDepth, int depth)
        {
            var writer = _writer;

            // The rows are objects one level below the collection, nested field groups further below
            EnterValue(depth + 1 + plan.GroupDepth);

            writer.EnterNesting();
            writer.WriteArrayHeader(key, items.Count);
            writer.WriteFieldList(CollectionsMarshal.AsSpan(plan.Columns));
            EnterLevels(plan);

            for (var i = 0; i < items.Count; i++)
            {
                writer.StartLine(rowDepth);
                WriteCells(items[i]!, plan, 0, plan.Columns.Count, true);
            }

            writer.Nesting -= 2 + plan.GroupDepth;
        }

        /// <summary>Writes a planned keyed tabular object (spec §9.5): the header, then one <c>key: cells</c> row per entry.</summary>
        private void WriteKeyed(string? key, List<KeyValuePair<string, object>> entries, TablePlan plan, int rowDepth, int depth)
        {
            var writer = _writer;
            EnterValue(depth + 1 + plan.GroupDepth);

            writer.EnterNesting();
            writer.WriteKeyedHeader(key, entries.Count);
            writer.WriteFieldList(CollectionsMarshal.AsSpan(plan.Columns));
            EnterLevels(plan);

            foreach (var (entryKey, value) in entries)
            {
                writer.StartLine(rowDepth);
                writer.WriteKey(entryKey);
                writer.Write(": ");
                WriteCells(value, plan, 0, plan.Columns.Count, true);
            }

            writer.Nesting -= 2 + plan.GroupDepth;
        }

        /// <summary>Enters the levels of the rows and their nested field groups, as the encoder does.</summary>
        private void EnterLevels(TablePlan plan)
        {
            for (var level = 0; level <= plan.GroupDepth; level++)
            {
                _writer.EnterNesting();
            }
        }

        /// <summary>Writes the cells of the columns from <paramref name="start"/> to <paramref name="end"/> of an object.</summary>
        /// <returns>Whether no cell was written yet.</returns>
        private bool WriteCells(object item, TablePlan plan, int start, int end, bool first)
        {
            var writer = _writer;
            var columns = CollectionsMarshal.AsSpan(plan.Columns);
            var sources = CollectionsMarshal.AsSpan(plan.Sources);

            for (var i = start; i < end; i = columns[i].End)
            {
                ref readonly var source = ref sources[i];

                if (columns[i].IsGroupAt(i))
                {
                    first = WriteCells(ValueOf(plan, in source, item)!, plan, i + 1, columns[i].End, first);
                    continue;
                }

                if (!first)
                {
                    writer.WriteDelimiter();
                }

                first = false;

                if (!source.ReadAsObject)
                {
                    if (source.Metadata is { } property)
                    {
                        property.Primitive!.WriteText(writer, item);
                    }
                    else
                    {
                        source.Writer!.WriteValueOf(item, source.Property, writer);
                    }

                    continue;
                }

                var value = ValueOf(plan, in source, item);

                if (value is null)
                {
                    writer.WriteNull();
                }
                else
                {
                    WritePrimitive(value);
                }
            }

            return first;
        }
    }
}
