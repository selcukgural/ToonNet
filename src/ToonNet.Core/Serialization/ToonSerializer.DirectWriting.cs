using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

namespace ToonNet.Core.Serialization;

// Serialization straight to TOON text. The result is the text the encoder produces for the ToonValue tree that
// SerializeValue builds, without building that tree for the common shapes: objects, primitives, collections and
// dictionaries with string or integer keys. Values the writer does not handle itself (converter output, ToonValue
// values, types with generated code or [ToonConverter], collections whose form it cannot decide) are converted with
// SerializeValue and written by ToonDomWriter. On any error the text is discarded and the tree-based path runs instead, so exceptions,
// messages and paths are exactly those of SerializeValue and the encoder.
public static partial class ToonSerializer
{
    /// <summary>The number of times direct writing fell back to the tree-based path on this thread (tests read it).</summary>
    [ThreadStatic]
    internal static int DirectWriteFallbacks;

    /// <summary>
    ///     Serializes a value and appends its TOON text to <paramref name="buffer"/>.
    /// </summary>
    private static void SerializeTo(ToonTextBuffer buffer, object? value, Type type, ToonSerializerOptions options)
    {
        var start = buffer.Length;

        try
        {
            new DirectWriter(buffer, options).WriteRoot(value, type);
        }
        catch
        {
            // Report the error (or, should direct writing fail where the tree-based path does not, write the text)
            // exactly as the tree-based path does
            DirectWriteFallbacks++;
            buffer.Truncate(start);

            var toonValue = SerializeValue(value, type, options, 0, new SerializerState());
            new ToonEncoder(options.ToonOptions).Write(new ToonDocument(toonValue ?? ToonNull.Instance), buffer);
        }
    }

    /// <summary>How a value is written.</summary>
    private enum DirectKind : byte
    {
        Null,
        Primitive,
        Object,
        Collection,
        Dictionary,

        /// <summary>An object of a type with generated code, written by its <see cref="ToonObjectWriter"/>.</summary>
        Generated,

        /// <summary>Converted with <see cref="SerializeValue"/> and written by <see cref="ToonDomWriter"/>.</summary>
        Tree
    }

    /// <summary>What direct writing needs to know about a runtime type.</summary>
    private sealed class DirectTypeInfo
    {
        public required DirectKind Kind { get; init; }

        public TypeMetadata? Metadata { get; init; }

        /// <summary>Reads the entries of a dictionary written directly.</summary>
        public DictionaryEntryReader? Entries { get; init; }

        /// <summary>
        ///     Whether the type has generated code, which <see cref="SerializeValue"/> uses (when the options allow it)
        ///     instead of reflection.
        /// </summary>
        public bool HasGeneratedCode { get; init; }

        /// <summary>The generated writer of a type with generated code.</summary>
        public ToonObjectWriter? Writer { get; init; }
    }

    private static readonly ConcurrentDictionary<Type, DirectTypeInfo> DirectTypeInfos = new();

    /// <summary>
    ///     Classifies a runtime type in the order <see cref="SerializeValue"/> checks it (converters from the options are
    ///     checked per value).
    /// </summary>
    private static DirectTypeInfo GetDirectTypeInfo(Type type)
    {
        return DirectTypeInfos.GetOrAdd(type, static t =>
        {
            if (typeof(ToonValue).IsAssignableFrom(t))
            {
                return new DirectTypeInfo { Kind = DirectKind.Tree };
            }

            if (IsPrimitiveType(t))
            {
                return new DirectTypeInfo { Kind = DirectKind.Primitive };
            }

            if (FindGeneratedSerializer(t) is { } generated)
            {
                // Code from a generator without writers builds the tree
                return new DirectTypeInfo
                {
                    Kind = generated.Writer != null ? DirectKind.Generated : DirectKind.Tree,
                    HasGeneratedCode = true,
                    Writer = generated.Writer
                };
            }

            var metadata = GetTypeMetadata(t);

            if (metadata.TypeConverter != null)
            {
                return new DirectTypeInfo { Kind = DirectKind.Tree };
            }

            var reader = GetDictionaryEntryReader(t);

            if (reader != null || typeof(IDictionary).IsAssignableFrom(t))
            {
                // Distinct keys always format to distinct names only for these key types; others could collide, which
                // the tree resolves by keeping the last value
                return reader != null && IsDistinctlyFormattedKey(reader.KeyType)
                    ? new DirectTypeInfo { Kind = DirectKind.Dictionary, Entries = reader }
                    : new DirectTypeInfo { Kind = DirectKind.Tree };
            }

            return typeof(IEnumerable).IsAssignableFrom(t)
                ? new DirectTypeInfo { Kind = DirectKind.Collection }
                : new DirectTypeInfo { Kind = DirectKind.Object, Metadata = metadata };
        });

        static bool IsDistinctlyFormattedKey(Type keyType) =>
            keyType == typeof(string) || keyType == typeof(int) || keyType == typeof(long) || keyType == typeof(short) ||
            keyType == typeof(byte) || keyType == typeof(sbyte) || keyType == typeof(ushort) || keyType == typeof(uint) ||
            keyType == typeof(ulong) || keyType == typeof(char) || keyType == typeof(Guid) || keyType.IsEnum;
    }

    private static readonly ConcurrentDictionary<Type, DirectTypeInfo> ReflectionTypeInfos = new();

    /// <summary>
    ///     Classifies a type that has generated code as <see cref="SerializeValue"/> does when the generated code is not
    ///     used (options with converters, or generated code turned off).
    /// </summary>
    private static DirectTypeInfo GetReflectionTypeInfo(Type type)
    {
        return ReflectionTypeInfos.GetOrAdd(type, static t =>
        {
            var metadata = GetTypeMetadata(t);

            if (metadata.TypeConverter != null)
            {
                return new DirectTypeInfo { Kind = DirectKind.Tree };
            }

            if (GetDictionaryEntryReader(t) != null || typeof(IDictionary).IsAssignableFrom(t) || typeof(IEnumerable).IsAssignableFrom(t))
            {
                return new DirectTypeInfo { Kind = DirectKind.Tree }; // rare for generated types; the tree handles them
            }

            return new DirectTypeInfo { Kind = DirectKind.Object, Metadata = metadata };
        });
    }

    /// <summary>The types <see cref="TrySerializePrimitive"/> converts.</summary>
    private static bool IsPrimitiveType(Type type)
    {
        return type == typeof(string) || type == typeof(char) || type == typeof(bool) ||
               type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) || type == typeof(decimal) || type == typeof(Half) ||
               type == typeof(BigInteger) || type == typeof(Int128) || type == typeof(UInt128) ||
               type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) ||
               type == typeof(TimeSpan) || type == typeof(Guid) || typeof(Uri).IsAssignableFrom(type) || type.IsEnum;
    }

    /// <summary>
    ///     The serialized names of the properties of <paramref name="metadata"/> (aligned with
    ///     <see cref="TypeMetadata.Properties"/>), or null when two properties share a name, which the tree resolves by
    ///     keeping the last value.
    /// </summary>
    private static string[]? GetDistinctPropertyNames(TypeMetadata metadata, ToonSerializerOptions options)
    {
        var policy = metadata.FixedNamingPolicy ?? options.PropertyNamingPolicy;

        if ((uint)policy < NamingPolicyCount && metadata.DistinctNames[(int)policy] is { } cached)
        {
            return cached.Length == 0 && metadata.Properties.Length != 0 ? null : cached;
        }

        var names = new string[metadata.Properties.Length];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var distinct = true;

        for (var i = 0; i < names.Length; i++)
        {
            names[i] = GetPropertyName(metadata.Properties[i], metadata, options);
            distinct &= seen.Add(names[i]);
        }

        if ((uint)policy < NamingPolicyCount)
        {
            // An empty array marks duplicates for a type with properties
            metadata.DistinctNames[(int)policy] = distinct ? names : [];
        }

        return distinct ? names : null;
    }

    /// <summary>
    ///     Writes one document. An instance is used once and is not thread-safe.
    /// </summary>
    private sealed class DirectWriter : IToonSerializationHost
    {
        private readonly ToonSerializerOptions _options;
        private readonly ToonWriter _writer;
        private readonly ToonDomWriter _tree;
        private readonly bool _hasConverters;
        private readonly bool _ignoreNulls;
        private readonly bool _includeReadOnly;

        public DirectWriter(ToonTextBuffer buffer, ToonSerializerOptions options)
        {
            _options = options;
            _writer = new ToonWriter(buffer, options.ToonOptions);
            _tree = new ToonDomWriter(_writer);
            _hasConverters = options.Converters.Count != 0;
            _ignoreNulls = options.IgnoreNullValues;
            _includeReadOnly = options.IncludeReadOnlyProperties;
            _writer.Host = this;
        }

        public ToonSerializerOptions Options => _options;

        /// <summary>
        ///     Writes the root value like the encoder writes the root of the tree: an object as top-level fields, a
        ///     collection with a key-less header, anything else on one line (spec §5).
        /// </summary>
        public void WriteRoot(object? value, Type type)
        {
            _ = _options.ToonOptions.ValidatedMaxDepth;

            switch (Classify(value, type, null, out var info))
            {
                case DirectKind.Null:
                    _writer.StartLine(0);
                    _writer.WriteNull();
                    break;
                case DirectKind.Primitive:
                    _writer.StartLine(0);
                    _writer.BeginRootPrimitive();
                    WritePrimitive(value!);
                    break;
                case DirectKind.Object:
                    EnterValue(0);
                    WriteObjectFields(value!, info!.Metadata!, 0, 1, firstOnCurrentLine: false, 0);
                    break;
                case DirectKind.Dictionary:
                    EnterValue(0);
                    WriteDictionaryEntries(value!, info!, 0, 1, firstOnCurrentLine: false, 0);
                    break;
                case DirectKind.Generated:
                    EnterValue(0);
                    info!.Writer!.WriteFieldsOf(value!, _writer, 0, 1, firstOnCurrentLine: false, 0);
                    break;
                case DirectKind.Collection:
                    _writer.StartLine(0);
                    _writer.Nesting = -1; // the root array is at depth 0, like the root object
                    WriteCollection(null, (IEnumerable)value!, 1, ToonArrayPosition.Root, 0);
                    break;
                default:
                    _tree.WriteRoot(ToTree(value, type, null, 0) ?? ToonNull.Instance);
                    break;
            }
        }

        /// <summary>
        ///     Decides how <paramref name="value"/> is written, in the order of <see cref="SerializeValue"/>: null,
        ///     converters, <see cref="ToonValue"/>, primitives, <c>[ToonConverter]</c> types, dictionaries, collections,
        ///     objects.
        /// </summary>
        private DirectKind Classify(object? value, Type declaredType, IToonConverter? propertyConverter, out DirectTypeInfo? info)
        {
            info = null;

            if (value == null)
            {
                return DirectKind.Null;
            }

            var runtimeType = value.GetType();

            if (propertyConverter != null ||
                (_hasConverters && (_options.GetConverter(runtimeType) ?? _options.GetConverter(declaredType)) != null))
            {
                return DirectKind.Tree;
            }

            info = GetDirectTypeInfo(runtimeType);

            if (info.HasGeneratedCode && (_hasConverters || !_options.UseGeneratedCode))
            {
                // Without generated code the type is converted with reflection, which direct writing does as well
                info = GetReflectionTypeInfo(runtimeType);
            }

            return info.Kind == DirectKind.Object && GetDistinctPropertyNames(info.Metadata!, _options) == null ? DirectKind.Tree : info.Kind;
        }

        /// <summary>
        ///     The depth and stack checks <see cref="SerializeValue"/> makes for an object, collection or dictionary;
        ///     cycles end here too (the error is reported by the tree-based path).
        /// </summary>
        public void EnterValue(int depth)
        {
            if (depth > _options.ValidatedMaxDepth || !RuntimeHelpers.TryEnsureSufficientExecutionStack())
            {
                throw new InsufficientExecutionStackException();
            }
        }

        private ToonValue? ToTree(object? value, Type declaredType, IToonConverter? propertyConverter, int depth)
        {
            return SerializeValue(value, declaredType, _options, depth, new SerializerState(), propertyConverter);
        }

        #region Objects and dictionaries

        /// <summary>
        ///     Writes the properties of an object as fields: the first on the current line when
        ///     <paramref name="firstOnCurrentLine"/> (a list item), the others on new lines at <paramref name="lineDepth"/>;
        ///     nested content goes to <paramref name="childDepth"/>.
        /// </summary>
        /// <returns>The number of fields written.</returns>
        private int WriteObjectFields(object instance, TypeMetadata metadata, int lineDepth, int childDepth, bool firstOnCurrentLine,
                                      int depth)
        {
            var names = GetDistinctPropertyNames(metadata, _options)!;
            var properties = metadata.Properties;
            var count = 0;

            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];

                if (!_includeReadOnly && !property.HasSetter)
                {
                    continue;
                }

                var startLine = count > 0 || !firstOnCurrentLine;

                if (property.Primitive is { } primitive && property.Converter == null && !_hasConverters)
                {
                    if (_ignoreNulls && primitive.IsNull(instance))
                    {
                        continue;
                    }

                    if (startLine)
                    {
                        _writer.StartLine(lineDepth);
                    }

                    _writer.WriteKey(names[i]);
                    _writer.Write(": ");
                    primitive.WriteText(_writer, instance);
                    count++;
                }
                else if (WriteField(names[i], property.Getter(instance), property.Property.PropertyType, property.Converter, lineDepth,
                                    childDepth, startLine, depth + 1))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Writes the entries of a dictionary as fields, like <see cref="WriteObjectFields"/>.</summary>
        private int WriteDictionaryEntries(object dictionary, DirectTypeInfo info, int lineDepth, int childDepth, bool firstOnCurrentLine,
                                           int depth)
        {
            var count = 0;

            foreach (var (rawKey, entryValue) in info.Entries!.Read(dictionary))
            {
                if (WriteField(FormatDictionaryKey(rawKey), entryValue, entryValue?.GetType() ?? typeof(object), null, lineDepth, childDepth,
                               count > 0 || !firstOnCurrentLine, depth + 1))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        ///     Writes <c>key: value</c> (or a nested object, dictionary or array) like the encoder writes a field.
        /// </summary>
        /// <returns><c>false</c> when the field is skipped (a null value with <see cref="ToonSerializerOptions.IgnoreNullValues"/>,
        ///     or a converter that returned null).</returns>
        public bool WriteField(string key, object? value, Type declaredType, IToonConverter? propertyConverter, int lineDepth,
                               int childDepth, bool startLine, int depth)
        {
            var writer = _writer;
            var kind = Classify(value, declaredType, propertyConverter, out var info);
            ToonValue? tree = null;

            if (kind == DirectKind.Tree && (tree = ToTree(value, declaredType, propertyConverter, depth)) == null)
            {
                return false;
            }

            if (kind == DirectKind.Null && _ignoreNulls)
            {
                return false;
            }

            if (startLine)
            {
                writer.StartLine(lineDepth);
            }

            switch (kind)
            {
                case DirectKind.Null:
                    writer.WriteKey(key);
                    writer.Write(": ");
                    writer.WriteNull();
                    break;
                case DirectKind.Primitive:
                    writer.WriteKey(key);
                    writer.Write(": ");
                    WritePrimitive(value!);
                    break;
                case DirectKind.Object:
                    EnterValue(depth);
                    writer.WriteKey(key);
                    writer.Write(':');
                    writer.EnterNesting();
                    WriteObjectFields(value!, info!.Metadata!, childDepth, childDepth + 1, firstOnCurrentLine: false, depth);
                    writer.ExitNesting();
                    break;
                case DirectKind.Dictionary:
                    EnterValue(depth);
                    writer.WriteKey(key);
                    writer.Write(':');
                    writer.EnterNesting();
                    WriteDictionaryEntries(value!, info!, childDepth, childDepth + 1, firstOnCurrentLine: false, depth);
                    writer.ExitNesting();
                    break;
                case DirectKind.Generated:
                    EnterValue(depth);
                    writer.WriteKey(key);
                    writer.Write(':');
                    writer.EnterNesting();
                    info!.Writer!.WriteFieldsOf(value!, writer, childDepth, childDepth + 1, firstOnCurrentLine: false, depth);
                    writer.ExitNesting();
                    break;
                case DirectKind.Collection:
                    WriteCollection(key, (IEnumerable)value!, childDepth, ToonArrayPosition.Field, depth);
                    break;
                default:
                    _tree.WriteField(key, tree!, childDepth);
                    break;
            }

            return true;
        }

        /// <summary>Writes the properties of a converted object as fields, like <see cref="WriteObjectFields"/>.</summary>
        public int WriteTreeFields(ToonObject value, int lineDepth, int childDepth, bool firstOnCurrentLine)
        {
            var count = 0;

            foreach (var (key, fieldValue) in value.Properties)
            {
                if (count > 0 || !firstOnCurrentLine)
                {
                    _writer.StartLine(lineDepth);
                }

                _tree.WriteField(key, fieldValue, childDepth);
                count++;
            }

            return count;
        }

        #endregion

        #region Collections

        /// <summary>
        ///     Writes a collection like the encoder writes an array: empty, inline (all items primitive), tabular (uniform
        ///     objects with primitive values, not inside a list item) or as list items (spec §9).
        /// </summary>
        private void WriteCollection(string? key, IEnumerable enumerable, int childDepth, ToonArrayPosition position, int depth)
        {
            EnterValue(depth);

            var items = new PooledItems(enumerable);

            try
            {
                var count = items.Count;

                if (count == 0)
                {
                    ToonDomWriter.WriteEmptyArray(_writer, key, position);
                    return;
                }

                var kinds = ArrayPool<DirectKind>.Shared.Rent(count);
                var infos = ArrayPool<DirectTypeInfo?>.Shared.Rent(count);

                try
                {
                    var allPrimitive = true;

                    for (var i = 0; i < count; i++)
                    {
                        var item = items[i];
                        kinds[i] = Classify(item, item?.GetType() ?? typeof(object), null, out infos[i]);

                        if (kinds[i] == DirectKind.Tree)
                        {
                            WriteCollectionAsTree(key, items, childDepth, position, depth);
                            return;
                        }

                        allPrimitive &= kinds[i] is DirectKind.Null or DirectKind.Primitive;
                    }

                    if (allPrimitive)
                    {
                        WriteInlineItems(key, items);
                        return;
                    }

                    if (position != ToonArrayPosition.ListItem)
                    {
                        switch (GetTabularShape(items, kinds, infos, out var metadata, out var included))
                        {
                            case TabularShape.Tabular when metadata == null:
                                WriteGeneratedRows(key, items, infos[0]!.Writer!, included!, childDepth, depth);
                                return;
                            case TabularShape.Tabular:
                                WriteTabularRows(key, items, metadata, included!, childDepth, depth);
                                return;
                            case TabularShape.Unknown:
                                WriteCollectionAsTree(key, items, childDepth, position, depth);
                                return;
                        }
                    }

                    _writer.EnterNesting();
                    _writer.WriteArrayHeader(key, count);
                    _writer.Write(':');

                    for (var i = 0; i < count; i++)
                    {
                        _writer.StartLine(childDepth);
                        WriteListItem(items[i], kinds[i], infos[i], childDepth, depth + 1);
                    }

                    _writer.ExitNesting();
                }
                finally
                {
                    ArrayPool<DirectTypeInfo?>.Shared.Return(infos, clearArray: true);
                    ArrayPool<DirectKind>.Shared.Return(kinds);
                }
            }
            finally
            {
                items.Dispose();
            }
        }

        private void WriteInlineItems(string? key, PooledItems items)
        {
            var writer = _writer;
            writer.EnterNesting();
            writer.WriteArrayHeader(key, items.Count);
            writer.Write(": ");

            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    writer.WriteDelimiter();
                }

                if (items[i] is { } item)
                {
                    WritePrimitive(item);
                }
                else
                {
                    writer.WriteNull();
                }
            }

            writer.ExitNesting();
        }

        /// <summary>Converts the items with <see cref="SerializeValue"/> and lets the encoder decide the form.</summary>
        private void WriteCollectionAsTree(string? key, PooledItems items, int childDepth, ToonArrayPosition position, int depth)
        {
            var array = new ToonArray(new List<ToonValue>(items.Count));

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                array.Items.Add(ToTree(item, item?.GetType() ?? typeof(object), null, depth + 1) ?? ToonNull.Instance);
            }

            _tree.WriteArray(key, array, childDepth, position);
        }

        private enum TabularShape
        {
            /// <summary>Not every item is an object with the same keys and primitive values.</summary>
            NotTabular,
            Tabular,

            /// <summary>The form depends on values only the tree shows (converters, objects of different types).</summary>
            Unknown
        }

        /// <summary>
        ///     Applies the encoder's tabular rule (spec §9.3) to the items: every item a non-empty object with the same
        ///     key set and only primitive values. For a tabular shape, <c>included</c> tells per property of
        ///     <c>metadata</c> whether it is a column.
        /// </summary>
        private TabularShape GetTabularShape(PooledItems items, DirectKind[] kinds, DirectTypeInfo?[] infos, out TypeMetadata? metadata,
                                             out bool[]? included)
        {
            metadata = null;
            included = null;

            for (var i = 0; i < items.Count; i++)
            {
                if (kinds[i] is not (DirectKind.Object or DirectKind.Dictionary or DirectKind.Generated))
                {
                    return TabularShape.NotTabular; // a primitive, null or array item is never a row
                }
            }

            var type = items[0]!.GetType();

            if (kinds[0] == DirectKind.Generated)
            {
                return GetGeneratedTabularShape(items, kinds, infos[0]!.Writer!, type, out included);
            }

            for (var i = 0; i < items.Count; i++)
            {
                // Dictionaries and objects of different types can still have the same keys
                if (kinds[i] != DirectKind.Object || items[i]!.GetType() != type)
                {
                    return TabularShape.Unknown;
                }
            }

            metadata = infos[0]!.Metadata!;
            var properties = metadata.Properties;
            included = new bool[properties.Length];
            var columns = 0;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i]!;
                var itemColumns = 0;

                for (var p = 0; p < properties.Length; p++)
                {
                    var property = properties[p];

                    if (!_includeReadOnly && !property.HasSetter)
                    {
                        continue;
                    }

                    if (property.Converter != null)
                    {
                        return TabularShape.Unknown;
                    }

                    bool isNull;

                    if (property.Primitive is { } primitive && !_hasConverters)
                    {
                        isNull = primitive.IsNull(item);
                    }
                    else
                    {
                        var value = property.Getter(item);

                        switch (Classify(value, property.Property.PropertyType, null, out _))
                        {
                            case DirectKind.Null:
                                isNull = true;
                                break;
                            case DirectKind.Primitive:
                                isNull = false;
                                break;
                            case DirectKind.Tree:
                                return TabularShape.Unknown;
                            default:
                                return TabularShape.NotTabular; // an object or array value
                        }
                    }

                    var isColumn = !(isNull && _ignoreNulls);

                    if (i == 0)
                    {
                        included[p] = isColumn;
                    }
                    else if (included[p] != isColumn)
                    {
                        return TabularShape.NotTabular; // the key sets differ
                    }

                    if (isColumn)
                    {
                        itemColumns++;
                    }
                }

                columns = itemColumns;
            }

            return columns > 0 ? TabularShape.Tabular : TabularShape.NotTabular;
        }

        /// <summary>The tabular rule for objects of a type with a generated writer, which reports each object's shape.</summary>
        private TabularShape GetGeneratedTabularShape(PooledItems items, DirectKind[] kinds, ToonObjectWriter objectWriter, Type type,
                                                      out bool[]? included)
        {
            var count = objectWriter.PropertyCount;
            var columns = new bool[count];
            var current = count <= 64 ? stackalloc bool[count] : new bool[count];
            var unknown = false;
            included = null;

            for (var i = 0; i < items.Count; i++)
            {
                if (kinds[i] != DirectKind.Generated || items[i]!.GetType() != type)
                {
                    return TabularShape.Unknown;
                }

                var target = i == 0 ? columns.AsSpan() : current;

                switch (objectWriter.GetRowShapeOf(items[i]!, _writer, target))
                {
                    case ToonRowShape.NotRow:
                        return TabularShape.NotTabular;
                    case ToonRowShape.Unknown:
                        unknown = true; // a later object may still decide it is not tabular
                        continue;
                }

                if (i > 0 && !current.SequenceEqual(columns))
                {
                    return TabularShape.NotTabular; // the key sets differ
                }
            }

            if (unknown)
            {
                return TabularShape.Unknown;
            }

            included = columns;
            return columns.AsSpan().Contains(true) ? TabularShape.Tabular : TabularShape.NotTabular;
        }

        private void WriteGeneratedRows(string? key, PooledItems items, ToonObjectWriter objectWriter, bool[] included, int rowDepth, int depth)
        {
            var writer = _writer;
            var fields = new List<string>(included.Length);

            for (var p = 0; p < included.Length; p++)
            {
                if (included[p])
                {
                    fields.Add(objectWriter.GetName(p, _options));
                }
            }

            // The rows are objects one level below the collection
            EnterValue(depth + 1);

            writer.EnterNesting();
            writer.WriteArrayHeader(key, items.Count);
            writer.WriteFieldList(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(fields));
            writer.EnterNesting();

            for (var i = 0; i < items.Count; i++)
            {
                writer.StartLine(rowDepth);
                objectWriter.WriteRowOf(items[i]!, writer, included);
            }

            writer.ExitNesting();
            writer.ExitNesting();
        }

        private void WriteTabularRows(string? key, PooledItems items, TypeMetadata metadata, bool[] included, int rowDepth, int depth)
        {
            var writer = _writer;
            var names = GetDistinctPropertyNames(metadata, _options)!;
            var properties = metadata.Properties;
            var fields = new List<string>(properties.Length);

            for (var p = 0; p < properties.Length; p++)
            {
                if (included[p])
                {
                    fields.Add(names[p]);
                }
            }

            // The rows are objects one level below the collection
            EnterValue(depth + 1);

            writer.EnterNesting();
            writer.WriteArrayHeader(key, items.Count);
            writer.WriteFieldList(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(fields));
            writer.EnterNesting();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i]!;
                var first = true;
                writer.StartLine(rowDepth);

                for (var p = 0; p < properties.Length; p++)
                {
                    if (!included[p])
                    {
                        continue;
                    }

                    if (!first)
                    {
                        writer.WriteDelimiter();
                    }

                    first = false;
                    var property = properties[p];

                    if (property.Primitive is { } primitive && !_hasConverters)
                    {
                        primitive.WriteText(writer, item);
                    }
                    else if (property.Getter(item) is { } value)
                    {
                        WritePrimitive(value);
                    }
                    else
                    {
                        writer.WriteNull();
                    }
                }
            }

            writer.ExitNesting();
            writer.ExitNesting();
        }

        /// <summary>
        ///     Writes a list item whose line has been started at <paramref name="depth"/> (spec §9.4, §10).
        /// </summary>
        private void WriteListItem(object? item, DirectKind kind, DirectTypeInfo? info, int depth, int valueDepth)
        {
            var writer = _writer;

            switch (kind)
            {
                case DirectKind.Null:
                    writer.Write("- ");
                    writer.WriteNull();
                    break;
                case DirectKind.Primitive:
                    writer.Write("- ");
                    WritePrimitive(item!);
                    break;
                case DirectKind.Object or DirectKind.Dictionary or DirectKind.Generated:
                {
                    EnterValue(valueDepth);

                    // The first field sits on the hyphen line and its nested content goes two levels deeper (§10); an
                    // object without fields is a bare hyphen
                    var start = writer.Position;
                    writer.Write("- ");
                    writer.EnterNesting();

                    var fields = kind switch
                    {
                        DirectKind.Object => WriteObjectFields(item!, info!.Metadata!, depth + 1, depth + 2, firstOnCurrentLine: true, valueDepth),
                        DirectKind.Generated => info!.Writer!.WriteFieldsOf(item!, writer, depth + 1, depth + 2, firstOnCurrentLine: true, valueDepth),
                        _ => WriteDictionaryEntries(item!, info!, depth + 1, depth + 2, firstOnCurrentLine: true, valueDepth)
                    };

                    writer.ExitNesting();

                    if (fields == 0)
                    {
                        writer.Truncate(start);
                        writer.Write('-');
                    }

                    break;
                }
                default:
                    writer.Write("- ");
                    WriteCollection(null, (IEnumerable)item!, depth + 1, ToonArrayPosition.ListItem, valueDepth);
                    break;
            }
        }

        /// <summary>
        ///     The items of a collection in an array rented from <see cref="ArrayPool{T}.Shared"/>, so the count is known
        ///     before the header is written and the items can be checked before they are written.
        /// </summary>
        private struct PooledItems : IDisposable
        {
            private object?[] _items;

            public PooledItems(IEnumerable enumerable)
            {
                _items = ArrayPool<object?>.Shared.Rent(enumerable is ICollection collection ? Math.Max(collection.Count, 1) : 16);
                Count = 0;

                foreach (var item in enumerable)
                {
                    if (Count == _items.Length)
                    {
                        var larger = ArrayPool<object?>.Shared.Rent(_items.Length * 2);
                        Array.Copy(_items, larger, Count);
                        ArrayPool<object?>.Shared.Return(_items, clearArray: true);
                        _items = larger;
                    }

                    _items[Count++] = item;
                }
            }

            public int Count { get; private set; }

            public readonly object? this[int index] => _items[index];

            public void Dispose()
            {
                if (_items.Length > 0)
                {
                    ArrayPool<object?>.Shared.Return(_items, clearArray: true);
                    _items = [];
                }
            }
        }

        #endregion

        #region Primitives

        /// <summary>
        ///     Writes a primitive exactly as <see cref="TrySerializePrimitive"/> followed by the encoder would, without
        ///     creating a <see cref="ToonValue"/> for the common types.
        /// </summary>
        private void WritePrimitive(object value)
        {
            var writer = _writer;

            switch (value)
            {
                case string s:
                    writer.WriteString(s);
                    break;
                case int n:
                    writer.WriteInteger(n);
                    break;
                case long n:
                    writer.WriteInteger(n);
                    break;
                case bool b:
                    writer.WriteBoolean(b);
                    break;
                case double n:
                    writer.WriteDouble(n);
                    break;
                case decimal n:
                    writer.WriteDecimal(n);
                    break;
                case float n:
                    writer.WriteDouble(ToonNumber.SingleToDouble(n));
                    break;
                case short n:
                    writer.WriteInteger(n);
                    break;
                case byte n:
                    writer.WriteInteger(n);
                    break;
                case sbyte n:
                    writer.WriteInteger(n);
                    break;
                case ushort n:
                    writer.WriteInteger(n);
                    break;
                case uint n:
                    writer.WriteInteger(n);
                    break;
                case ulong n:
                    writer.WriteDecimal(n);
                    break;
                case char c:
                    writer.WriteString(new ReadOnlySpan<char>(in c));
                    break;
                case DateTime dt:
                {
                    Span<char> text = stackalloc char[64];
                    dt.TryFormat(text, out var length, "O", CultureInfo.InvariantCulture);
                    writer.WriteString(text[..length]);
                    break;
                }
                case DateTimeOffset dt:
                {
                    Span<char> text = stackalloc char[64];
                    dt.TryFormat(text, out var length, "O", CultureInfo.InvariantCulture);
                    writer.WriteString(text[..length]);
                    break;
                }
                case Guid guid:
                {
                    Span<char> text = stackalloc char[36];
                    guid.TryFormat(text, out var length, "D");
                    writer.WriteString(text[..length]);
                    break;
                }
                case Enum e:
                    writer.WriteString(e.ToString());
                    break;
                default:
                    TrySerializePrimitive(value, out var primitive);
                    writer.WritePrimitive(primitive);
                    break;
            }
        }

        #endregion
    }
}
