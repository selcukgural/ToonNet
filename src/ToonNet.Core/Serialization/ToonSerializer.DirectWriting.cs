using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    ///     The number of times direct writing left a possible nested field group or keyed tabular object to the tree on
    ///     this thread (tests read it).
    /// </summary>
    [ThreadStatic]
    internal static int DirectWriteTreeShapes;

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
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Report the error (or, should direct writing fail where the tree-based path does not, write the text)
            // exactly as the tree-based path does
            DirectWriteFallbacks++;
            buffer.Truncate(start);

            var toonValue = SerializeValue(value, type, options, 0, new SerializerState());
            new ToonEncoder(options.ToonOptions).Write(new ToonDocument(toonValue ?? ToonNull.Instance), buffer);
        }
    }

    /// <summary>
    ///     Thrown when writing or reading with a <see cref="ToonTypeInfo"/> meets a value that only reflection would handle
    ///     directly; the generated tree-based methods then convert the whole document.
    /// </summary>
    private sealed class GeneratedOnlyFallbackException : Exception;

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
            if (typeof(ToonValue).IsAssignableFrom(t) || ToonJsonInterop.IsDocumentType(t))
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

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                return new DirectTypeInfo { Kind = DirectKind.Collection };
            }

            ThrowIfAsyncEnumerable(t);
            return new DirectTypeInfo { Kind = DirectKind.Object, Metadata = metadata };
        });
    }

    private static readonly ConcurrentDictionary<Type, DirectTypeInfo> GeneratedOnlyTypeInfos = new();

    /// <summary>
    ///     Classifies a runtime type like <see cref="GetDirectTypeInfo"/> without reflection metadata, for writing with a
    ///     <see cref="ToonTypeInfo"/>: everything that would need it (objects without generated code, types with
    ///     <c>[ToonConverter]</c>, dictionaries without <see cref="IDictionary"/>) is <see cref="DirectKind.Tree"/>, which
    ///     ends direct writing in favor of the generated tree.
    /// </summary>
    private static DirectTypeInfo GetGeneratedOnlyTypeInfo(Type type)
    {
        return GeneratedOnlyTypeInfos.GetOrAdd(type, static t =>
        {
            if (typeof(ToonValue).IsAssignableFrom(t) || ToonJsonInterop.IsDocumentType(t))
            {
                return new DirectTypeInfo { Kind = DirectKind.Tree };
            }

            if (IsPrimitiveType(t))
            {
                return new DirectTypeInfo { Kind = DirectKind.Primitive };
            }

            if (FindGeneratedSerializer(t) is { } generated)
            {
                return new DirectTypeInfo
                {
                    Kind = generated.Writer != null ? DirectKind.Generated : DirectKind.Tree,
                    HasGeneratedCode = true,
                    Writer = generated.Writer
                };
            }

            if (t.IsDefined(typeof(Attributes.ToonConverterAttribute), inherit: true))
            {
                return new DirectTypeInfo { Kind = DirectKind.Tree };
            }

            var dictionaryInterface = FindDictionaryInterface(t);

            if (dictionaryInterface != null || typeof(IDictionary).IsAssignableFrom(t))
            {
                // The entries are read through IDictionary, which needs no generic code made at run time
                return dictionaryInterface != null && typeof(IDictionary).IsAssignableFrom(t) && IsDistinctlyFormattedKey(dictionaryInterface[0])
                    ? new DirectTypeInfo { Kind = DirectKind.Dictionary, Entries = new NonGenericDictionaryEntryReader(dictionaryInterface[0], dictionaryInterface[1]) }
                    : new DirectTypeInfo { Kind = DirectKind.Tree };
            }

            return new DirectTypeInfo { Kind = typeof(IEnumerable).IsAssignableFrom(t) ? DirectKind.Collection : DirectKind.Tree };
        });
    }

    /// <summary>The key and value types of the generic dictionary interface a type implements, or null.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "The type is the runtime type of a written value; the interfaces of a constructed type are kept.")]
    private static Type[]? FindDictionaryInterface(Type type)
    {
        foreach (var candidate in type.GetInterfaces())
        {
            if (candidate.IsGenericType &&
                (candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>) || candidate.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)))
            {
                return candidate.GetGenericArguments();
            }
        }

        return null;
    }

    /// <summary>Whether distinct keys of the type always format to distinct names (others could collide, which the tree resolves).</summary>
    private static bool IsDistinctlyFormattedKey(Type keyType) =>
        keyType == typeof(string) || keyType == typeof(int) || keyType == typeof(long) || keyType == typeof(short) ||
        keyType == typeof(byte) || keyType == typeof(sbyte) || keyType == typeof(ushort) || keyType == typeof(uint) ||
        keyType == typeof(ulong) || keyType == typeof(char) || keyType == typeof(Guid) || keyType.IsEnum;

    /// <summary>Enumerates the entries of a dictionary through <see cref="IDictionary"/>, in the order of its generic enumerator.</summary>
    private sealed class NonGenericDictionaryEntryReader(Type keyType, Type valueType) : DictionaryEntryReader
    {
        public override Type KeyType { get; } = keyType;

        public override Type ValueType { get; } = valueType;

        public override IEnumerable<(object Key, object? Value)> Read(object dictionary)
        {
            var enumerator = ((IDictionary)dictionary).GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return (enumerator.Key, enumerator.Value);
            }
        }
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
    private sealed partial class DirectWriter : IToonSerializationHost
    {
        private readonly ToonSerializerOptions _options;
        private readonly ToonWriter _writer;
        private readonly ToonDomWriter _tree;
        private readonly bool _hasConverters;
        private readonly bool _ignoreNulls;
        private readonly bool _includeReadOnly;

        /// <summary>
        ///     Whether only generated code may convert values (writing with a <see cref="ToonTypeInfo"/>): what would need
        ///     reflection throws <see cref="GeneratedOnlyFallbackException"/> instead.
        /// </summary>
        private readonly bool _generatedOnly;

        /// <summary>With converters: tells which types generated code can still write.</summary>
        private readonly ConverterCheck? _converterCheck;

        /// <summary>Per type, whether a converter of the options writes it (types are looked up once per document).</summary>
        private Dictionary<Type, bool>? _convertedTypes;

        private bool IsConverted(Type type)
        {
            _convertedTypes ??= [];

            if (!_convertedTypes.TryGetValue(type, out var converted))
            {
                _convertedTypes[type] = converted = _options.GetConverter(type) != null;
            }

            return converted;
        }

        /// <summary>
        ///     Whether a converter writes values declared as <paramref name="type"/> (a nullable value type's values have its
        ///     underlying runtime type), so they cannot be written by the typed fast paths.
        /// </summary>
        private bool IsConvertedDeclared(Type type) =>
            _hasConverters && (IsConverted(type) || (Nullable.GetUnderlyingType(type) is { } underlying && IsConverted(underlying)));

        /// <summary>
        ///     Whether a converter writes the values of a property, which then cannot be written by the typed fast paths;
        ///     without converters, no property type is looked up.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsConvertedProperty(PropertyMetadata property) => _hasConverters && IsConvertedDeclared(property.Property.PropertyType);

        /// <summary>
        ///     With converters: per runtime type, how it is written when no converter applies to it, or null when one
        ///     does (looked up once per document).
        /// </summary>
        private Dictionary<Type, DirectTypeInfo?>? _infosWithConverters;

        // The last types looked up: the items of a collection usually share their type
        private Type? _lastRuntimeType;
        private DirectTypeInfo? _lastInfo;
        private Type? _lastDeclaredType;
        private bool _lastDeclaredConverted;

        /// <summary>Whether a converter applies to the declared type of a value, remembering the last answer.</summary>
        private bool IsDeclaredTypeConverted(Type declaredType)
        {
            if (declaredType != _lastDeclaredType)
            {
                _lastDeclaredConverted = IsConverted(declaredType);
                _lastDeclaredType = declaredType;
            }

            return _lastDeclaredConverted;
        }

        private DirectTypeInfo? GetInfoWithConverters(Type runtimeType)
        {
            if (runtimeType == _lastRuntimeType)
            {
                return _lastInfo;
            }

            _lastRuntimeType = runtimeType;
            return _lastInfo = LookUpInfoWithConverters(runtimeType);
        }

        private DirectTypeInfo? LookUpInfoWithConverters(Type runtimeType)
        {
            _infosWithConverters ??= [];

            if (!_infosWithConverters.TryGetValue(runtimeType, out var info))
            {
                if (!IsConverted(runtimeType))
                {
                    info = GetDirectTypeInfo(runtimeType);

                    // Generated code does not consult converters: it is used only when none applies to what it converts
                    if (info.HasGeneratedCode && (!_options.UseGeneratedCode || !_converterCheck!.AllowsGeneratedCode(runtimeType)))
                    {
                        info = GetReflectionTypeInfo(runtimeType);
                    }
                }

                _infosWithConverters[runtimeType] = info;
            }

            return info;
        }

        public DirectWriter(ToonTextBuffer buffer, ToonSerializerOptions options, bool generatedOnly = false)
        {
            _options = options;
            _generatedOnly = generatedOnly;
            _converterCheck = GetConverterCheck(options);
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

            var kind = Classify(value, type, null, out var info);

            switch (kind)
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
                case DirectKind.Object or DirectKind.Dictionary or DirectKind.Generated when MayBeKeyed(value!, kind, info!) &&
                                                                                           TryWriteKeyedRoot(value!, kind, info!, type):
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

            if (propertyConverter != null)
            {
                return DirectKind.Tree;
            }

            if (_generatedOnly)
            {
                // A converter writes the value through its tree, and so does generated code that would not consult one
                // it should (the generated tree-based method does)
                if (_hasConverters && (IsDeclaredTypeConverted(declaredType) || IsConverted(runtimeType) ||
                                       (FindGeneratedSerializer(runtimeType) != null && !_converterCheck!.AllowsGeneratedCode(runtimeType))))
                {
                    return DirectKind.Tree;
                }

                info = GetGeneratedOnlyTypeInfo(runtimeType);
                return info.Kind;
            }

            if (_hasConverters)
            {
                // A converter for the runtime or the declared type writes the value through its tree
                if (IsDeclaredTypeConverted(declaredType) || GetInfoWithConverters(runtimeType) is not { } converterFree)
                {
                    return DirectKind.Tree;
                }

                info = converterFree;
            }
            else
            {
                info = GetDirectTypeInfo(runtimeType);

                if (info.HasGeneratedCode && !_options.UseGeneratedCode)
                {
                    // Without generated code the type is converted with reflection, which direct writing does as well
                    info = GetReflectionTypeInfo(runtimeType);
                }
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
            if (_generatedOnly)
            {
                return ToGeneratedTree(value, declaredType, depth);
            }

            return SerializeValue(value, declaredType, _options, depth, new SerializerState(), propertyConverter);
        }

        /// <summary>
        ///     The tree of a value without reflection: through a converter of the options, as a <see cref="ToonValue"/> (a
        ///     <see cref="ToonDocument"/> or JSON value holds one), or through the generated tree-based method of its
        ///     type; anything else needs the reflection-based serializer.
        /// </summary>
        private ToonValue? ToGeneratedTree(object? value, Type declaredType, int depth)
        {
            if (value == null)
            {
                return _ignoreNulls ? null : ToonNull.Instance;
            }

            var runtimeType = value.GetType();

            if (_hasConverters && (_converterCheck!.GetConverter(runtimeType) ?? _converterCheck.GetConverter(declaredType)) is { } converter)
            {
                return converter.Write(value, _options);
            }

            if (value is ToonValue toonValue)
            {
                return toonValue;
            }

            if (ToonJsonInterop.TryToToon(value, out var documentValue))
            {
                return documentValue;
            }

            return GetGeneratedSerializer(runtimeType, _options) is { } generated
                ? generated.Serialize(value, _options, depth)
                : throw new GeneratedOnlyFallbackException();
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

                if (property.Primitive is { } primitive && property.Converter == null && !IsConvertedProperty(property))
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
            if (!_hasConverters && !_generatedOnly &&
                info.Entries!.TryWritePrimitiveEntries(_writer, dictionary, lineDepth, firstOnCurrentLine, _ignoreNulls, out var written))
            {
                return written;
            }

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
                case DirectKind.Object or DirectKind.Dictionary or DirectKind.Generated when MayBeKeyed(value!, kind, info!) &&
                                                                                           TryWriteKeyedField(key, value!, kind, info!, declaredType,
                                                                                               propertyConverter, childDepth, depth):
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

        /// <summary>
        ///     Whether an object, dictionary or generated object in a field or root position may qualify for the keyed
        ///     tabular form (spec §9.5): at least two written fields, each an object. When it may, the tree decides and
        ///     writes it. Most objects have a primitive or array field, which rules the form out at once.
        /// </summary>
        private bool MayBeKeyed(object value, DirectKind kind, DirectTypeInfo info)
        {
            var objects = 0;

            switch (kind)
            {
                case DirectKind.Generated:
                    return info.Writer!.MayBeKeyedOf(value, _writer);
                case DirectKind.Dictionary:
                    if (IsPrimitiveType(info.Entries!.ValueType) && !(_hasConverters && IsConvertedDeclared(info.Entries.ValueType)))
                    {
                        return false;
                    }

                    foreach (var (_, entryValue) in info.Entries!.Read(value))
                    {
                        switch (ClassifyKeyedField(entryValue, entryValue?.GetType() ?? typeof(object), null))
                        {
                            case KeyedField.NotObject:
                                return false;
                            case KeyedField.Unknown:
                                return true;
                            case KeyedField.Object:
                                objects++;
                                break;
                        }
                    }

                    return objects >= 2;
                default:
                    foreach (var property in info.Metadata!.Properties)
                    {
                        if (!_includeReadOnly && !property.HasSetter)
                        {
                            continue;
                        }

                        if (property.Primitive is { } primitive && property.Converter == null && !IsConvertedProperty(property))
                        {
                            if (_ignoreNulls && primitive.IsNull(value))
                            {
                                continue;
                            }

                            return false;
                        }

                        switch (ClassifyKeyedField(property.Getter(value), property.Property.PropertyType, property.Converter))
                        {
                            case KeyedField.NotObject:
                                return false;
                            case KeyedField.Unknown:
                                return true;
                            case KeyedField.Object:
                                objects++;
                                break;
                        }
                    }

                    return objects >= 2;
            }
        }

        /// <summary>
        ///     Writes a root value that may be a keyed tabular object: planned, or through the tree when only the tree
        ///     knows. Returns false when it is not one (and nothing was written).
        /// </summary>
        private bool TryWriteKeyedRoot(object value, DirectKind kind, DirectTypeInfo info, Type type)
        {
            switch (PlanKeyed(value, kind, info, out var entries, out var plan))
            {
                case PlanResult.Tabular:
                    EnterValue(0);
                    _writer.StartLine(0);
                    _writer.Nesting = -1; // the root object is at depth 0
                    WriteKeyed(null, entries, plan, 1, 0);
                    return true;
                case PlanResult.Unknown:
                    DirectWriteTreeShapes++;
                    _tree.WriteRoot(ToTree(value, type, null, 0)!);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Writes a field that may be a keyed tabular object, like <see cref="TryWriteKeyedRoot"/>.</summary>
        private bool TryWriteKeyedField(string key, object value, DirectKind kind, DirectTypeInfo info, Type declaredType,
                                        IToonConverter? propertyConverter, int childDepth, int depth)
        {
            switch (PlanKeyed(value, kind, info, out var entries, out var plan))
            {
                case PlanResult.Tabular:
                    EnterValue(depth);
                    WriteKeyed(key, entries, plan, childDepth, depth);
                    return true;
                case PlanResult.Unknown:
                    DirectWriteTreeShapes++;
                    _tree.WriteField(key, ToTree(value, declaredType, propertyConverter, depth)!, childDepth);
                    return true;
                default:
                    return false;
            }
        }

        private enum KeyedField
        {
            /// <summary>A null value that is not written.</summary>
            Skipped,
            NotObject,
            Object,

            /// <summary>Converted with a converter or written by the tree, so only the tree knows.</summary>
            Unknown
        }

        private KeyedField ClassifyKeyedField(object? value, Type declaredType, IToonConverter? converter)
        {
            return Classify(value, declaredType, converter, out _) switch
            {
                DirectKind.Null => _ignoreNulls ? KeyedField.Skipped : KeyedField.NotObject,
                DirectKind.Primitive or DirectKind.Collection => KeyedField.NotObject,
                DirectKind.Tree => KeyedField.Unknown,
                _ => KeyedField.Object
            };
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

            // Arrays and lists of primitives are written from their typed items, without boxing each one
            if (!_hasConverters && !_generatedOnly && PrimitiveListWriter.For(enumerable.GetType()) is { } primitives)
            {
                primitives.Write(_writer, key, enumerable, position);
                return;
            }

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
                                // Nested objects may form nested field groups; the plan decides, or leaves it to the tree
                                switch (PlanRows(items, kinds, infos, out var plan))
                                {
                                    case PlanResult.Tabular:
                                        WritePlannedRows(key, items, plan, childDepth, depth);
                                        return;
                                    case PlanResult.Unknown:
                                        DirectWriteTreeShapes++;
                                        WriteCollectionAsTree(key, items, childDepth, position, depth);
                                        return;
                                }

                                break;
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

        /// <summary>Writes a <c>T[]</c> or <c>List&lt;T&gt;</c> of typed primitives (or their nullable forms) as an inline array.</summary>
        private abstract class PrimitiveListWriter
        {
            private static readonly ConcurrentDictionary<Type, PrimitiveListWriter?> Writers = new();

            public abstract void Write(ToonWriter writer, string? key, IEnumerable items, ToonArrayPosition position);

            /// <summary>The writer for exactly <c>T[]</c> or <c>List&lt;T&gt;</c> of a typed primitive, or null.</summary>
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionJustification)]
            [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = ReflectionJustification)]
            [UnconditionalSuppressMessage("Trimming", "IL2076", Justification = ReflectionJustification)]
            public static PrimitiveListWriter? For(Type type) => Writers.GetOrAdd(type, static t =>
            {
                var element = t.IsArray && t.GetArrayRank() == 1 ? t.GetElementType()
                    : t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) ? t.GetGenericArguments()[0]
                    : null;

                if (element == null || !PrimitiveAccessor.IsTypedPrimitive(Nullable.GetUnderlyingType(element) ?? element))
                {
                    return null;
                }

                // Nullable items are unwrapped, so their values are not boxed either
                var writer = Nullable.GetUnderlyingType(element) is { } underlying
                    ? typeof(NullablePrimitiveListWriter<>).MakeGenericType(underlying)
                    : typeof(PrimitiveListWriter<>).MakeGenericType(element);

                return (PrimitiveListWriter)Activator.CreateInstance(writer)!;
            });
        }

        private sealed class PrimitiveListWriter<T> : PrimitiveListWriter
        {
            public override void Write(ToonWriter writer, string? key, IEnumerable items, ToonArrayPosition position)
            {
                ReadOnlySpan<T> span = items is T[] array ? array : CollectionsMarshal.AsSpan((List<T>)items);

                if (span.IsEmpty)
                {
                    ToonDomWriter.WriteEmptyArray(writer, key, position);
                    return;
                }

                writer.EnterNesting();
                writer.WriteArrayHeader(key, span.Length);
                writer.Write(": ");

                for (var i = 0; i < span.Length; i++)
                {
                    if (i > 0)
                    {
                        writer.WriteDelimiter();
                    }

                    if (default(T) == null && span[i] is null)
                    {
                        writer.WriteNull();
                    }
                    else
                    {
                        WriteTypedText(writer, span[i]);
                    }
                }

                writer.ExitNesting();
            }
        }

        private sealed class NullablePrimitiveListWriter<T> : PrimitiveListWriter where T : struct
        {
            public override void Write(ToonWriter writer, string? key, IEnumerable items, ToonArrayPosition position)
            {
                ReadOnlySpan<T?> span = items is T?[] array ? array : CollectionsMarshal.AsSpan((List<T?>)items);

                if (span.IsEmpty)
                {
                    ToonDomWriter.WriteEmptyArray(writer, key, position);
                    return;
                }

                writer.EnterNesting();
                writer.WriteArrayHeader(key, span.Length);
                writer.Write(": ");

                for (var i = 0; i < span.Length; i++)
                {
                    if (i > 0)
                    {
                        writer.WriteDelimiter();
                    }

                    if (span[i] is { } item)
                    {
                        WriteTypedText(writer, item);
                    }
                    else
                    {
                        writer.WriteNull();
                    }
                }

                writer.ExitNesting();
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

            // An object value may form a nested field group (§9.3), which the tree decides; an array value never can
            var nested = false;

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

                    if (property.Primitive is { } primitive && !IsConvertedProperty(property))
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
                            case DirectKind.Collection:
                                return TabularShape.NotTabular;
                            case DirectKind.Tree:
                                return TabularShape.Unknown;
                            default:
                                nested = true; // a later array value still rules the form out
                                isNull = false;
                                break;
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

            if (nested)
            {
                return TabularShape.Unknown;
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
            var hasReference = false;
            included = null;

            for (var i = 0; i < items.Count; i++)
            {
                if (kinds[i] != DirectKind.Generated || items[i]!.GetType() != type)
                {
                    return TabularShape.Unknown;
                }

                // The first row's columns are the reference (an object of unknown shape leaves its columns incomplete)
                var target = hasReference ? current : columns.AsSpan();

                switch (objectWriter.GetRowShapeOf(items[i]!, _writer, target))
                {
                    case ToonRowShape.NotRow:
                        return TabularShape.NotTabular;
                    case ToonRowShape.Unknown:
                        unknown = true; // a later object may still decide it is not tabular
                        continue;
                }

                if (!hasReference)
                {
                    hasReference = true;
                }
                else if (!current.SequenceEqual(columns))
                {
                    return TabularShape.NotTabular; // the key sets of two rows differ
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

                    if (property.Primitive is { } primitive && !IsConvertedProperty(property))
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

            /// <summary>The items.</summary>
            public readonly ReadOnlySpan<object?> Span => _items.AsSpan(0, Count);

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
                    Span<char> text = stackalloc char[ToonDateFormat.MaxLength];
                    writer.WriteString(text[..ToonDateFormat.Format(dt, text)]);
                    break;
                }
                case DateTimeOffset dt:
                {
                    Span<char> text = stackalloc char[ToonDateFormat.MaxLength];
                    writer.WriteString(text[..ToonDateFormat.Format(dt, text)]);
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
