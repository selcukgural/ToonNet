using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Serialization;

// Deserialization straight from TOON text. The parser's cursors are read into objects, collections and dictionaries
// without building a ToonValue tree; values the reader does not handle itself (ToonValue and object targets, types
// with generated code or [ToonConverter], property converters, non-generic collections) are built as a tree for that
// value and converted with DeserializeValue. The result is the object the tree-based path creates: properties are set
// in metadata order (a document in another order, or with repeated keys, takes the tree-based path), and on any error
// the tree-based path runs instead, so exceptions, messages and paths are exactly the same. Options with converters
// always use the tree-based path.
public static partial class ToonSerializer
{
    /// <summary>The number of times direct reading fell back to the tree-based path on this thread (tests read it).</summary>
    [ThreadStatic]
    internal static int DirectReadFallbacks;

    /// <summary>
    ///     Deserializes a document straight from the text; values with a converter are converted from their own tree.
    /// </summary>
    private static object? DeserializeText(ToonText toonString, Type type, ToonSerializerOptions options)
    {
        try
        {
            return DirectReader.Read(toonString, type, options);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Report the error exactly as the tree-based path does (or read what direct reading gave up on)
            DirectReadFallbacks++;
        }

        var document = new ToonParser(options.ToonOptions).Parse(toonString);
        return DeserializeValue(document.Root, type, options, 0, new SerializerState());
    }

    /// <summary>Thrown when direct reading cannot produce the tree-based result; the tree-based path runs instead.</summary>
    private sealed class DirectReadFallbackException : Exception;

    private enum ReadKind : byte
    {
        Tree,

        /// <summary>An object of a type with a generated reader.</summary>
        Generated,
        Primitive,
        Object,
        Collection,
        Dictionary
    }

    /// <summary>What direct reading needs to know about a target type.</summary>
    private sealed class ReadTypeInfo
    {
        public required ReadKind Kind { get; init; }

        /// <summary>The type values are converted to (the underlying type of a nullable value type).</summary>
        public required Type Target { get; init; }

        /// <summary>Whether null is a valid value (reference types and nullable value types).</summary>
        public required bool AcceptsNull { get; init; }

        /// <summary>Whether the target has generated code that reads objects.</summary>
        public bool HasGeneratedReader { get; init; }

        /// <summary>The generated reader, which reads object values straight from the text.</summary>
        public ToonObjectReader? Reader { get; init; }

        public TypeMetadata? Metadata { get; init; }

        public CollectionFactory? Collection { get; init; }

        public Type? ElementType { get; init; }

        public DictionaryFactory? Dictionary { get; init; }
    }

    private static readonly ConcurrentDictionary<Type, ReadTypeInfo> ReadTypeInfos = new();
    private static readonly ConcurrentDictionary<Type, ReadTypeInfo> ReflectionReadTypeInfos = new();

    /// <summary>
    ///     Classifies a target type in the order <see cref="DeserializeValue"/> checks it; with generated code turned
    ///     off, types that have it are classified like any other.
    /// </summary>
    private static ReadTypeInfo GetReadTypeInfo(Type type, bool useGeneratedCode)
    {
        return useGeneratedCode
            ? ReadTypeInfos.GetOrAdd(type, static t => CreateReadTypeInfo(t, useGeneratedCode: true))
            : ReflectionReadTypeInfos.GetOrAdd(type, static t => CreateReadTypeInfo(t, useGeneratedCode: false));
    }

    private static readonly ConcurrentDictionary<Type, ReadTypeInfo> GeneratedOnlyReadTypeInfos = new();

    /// <summary>
    ///     Classifies a target type without reflection metadata, for reading with a <see cref="ToonTypeInfo"/>: scalars,
    ///     types with generated code, and <see cref="ReadKind.Tree"/> for everything else, which ends direct reading in
    ///     favor of the generated tree-based methods.
    /// </summary>
    private static ReadTypeInfo GetGeneratedOnlyReadTypeInfo(Type type)
    {
        return GeneratedOnlyReadTypeInfos.GetOrAdd(type, static t =>
        {
            var underlying = Nullable.GetUnderlyingType(t);
            var target = underlying ?? t;
            var acceptsNull = !t.IsValueType || underlying != null;

            if (IsScalarType(target))
            {
                return new ReadTypeInfo { Kind = ReadKind.Primitive, Target = target, AcceptsNull = acceptsNull };
            }

            if (FindGeneratedSerializer(target) is { Deserialize: not null, Reader: { } reader })
            {
                return new ReadTypeInfo
                {
                    Kind = ReadKind.Generated, Target = target, AcceptsNull = acceptsNull, HasGeneratedReader = true, Reader = reader
                };
            }

            return new ReadTypeInfo { Kind = ReadKind.Tree, Target = target, AcceptsNull = acceptsNull };
        });
    }

    private static ReadTypeInfo CreateReadTypeInfo(Type t, bool useGeneratedCode)
    {
        {
            var underlying = Nullable.GetUnderlyingType(t);
            var target = underlying ?? t;
            var acceptsNull = !t.IsValueType || underlying != null;

            ReadTypeInfo Info(ReadKind kind) => new() { Kind = kind, Target = target, AcceptsNull = acceptsNull };

            if (typeof(ToonValue).IsAssignableFrom(t) || target == typeof(object) || ToonJsonInterop.IsDocumentType(target))
            {
                return Info(ReadKind.Tree);
            }

            if (IsScalarType(target))
            {
                return Info(ReadKind.Primitive);
            }

            // Types with generated code are read by it, without reflection metadata (DeserializeValue decides when)
            if (useGeneratedCode && FindGeneratedSerializer(target) is { Deserialize: not null } generated)
            {
                // Code from a generator without readers builds the tree
                return new ReadTypeInfo
                {
                    Kind = generated.Reader != null ? ReadKind.Generated : ReadKind.Tree,
                    Target = target,
                    AcceptsNull = acceptsNull,
                    HasGeneratedReader = true,
                    Reader = generated.Reader
                };
            }

            if (target.GetCustomAttribute<Attributes.ToonConverterAttribute>() != null)
            {
                return Info(ReadKind.Tree);
            }

            if (DictionaryFactories.GetOrAdd(target, static d => DictionaryFactory.Create(d)) is { } dictionary)
            {
                return new ReadTypeInfo { Kind = ReadKind.Dictionary, Target = target, AcceptsNull = acceptsNull, Dictionary = dictionary };
            }

            if (typeof(IDictionary).IsAssignableFrom(target))
            {
                return Info(ReadKind.Tree);
            }

            if (GetElementType(target) is { } elementType)
            {
                return new ReadTypeInfo
                {
                    Kind = ReadKind.Collection, Target = target, AcceptsNull = acceptsNull, ElementType = elementType,
                    Collection = CollectionFactories.GetOrAdd(target, static (c, e) => CollectionFactory.Create(c, e), elementType)
                };
            }

            if (typeof(IEnumerable).IsAssignableFrom(target) && target != typeof(string))
            {
                return Info(ReadKind.Tree); // non-generic lists
            }

            return new ReadTypeInfo { Kind = ReadKind.Object, Target = target, AcceptsNull = acceptsNull, Metadata = GetTypeMetadata(target) };
        }
    }

    /// <summary>
    ///     For each key of an object type: the constructor parameter and the settable property (not bound to the
    ///     constructor) it fills, by index (-1 for none).
    /// </summary>
    private sealed class ReadLookup(Dictionary<string, (int Parameter, int Property)> slots, bool shared,
                                    Dictionary<string, (int Parameter, int Property)> caseInsensitiveSlots)
    {
        public Dictionary<string, (int Parameter, int Property)> Slots { get; } = slots;

        /// <summary>Whether a key fills both a parameter and a property (read separately by the tree-based path).</summary>
        public bool HasSharedKeys { get; } = shared;

        /// <summary>The slots of the names no other name equals ignoring case, matched ignoring case.</summary>
        public Dictionary<string, (int Parameter, int Property)> CaseInsensitiveSlots { get; } = caseInsensitiveSlots;

        /// <summary>The slot of a key: an exact match, else (with case-insensitive reading) a match ignoring case.</summary>
        public bool TryGetSlot(string key, bool ignoreCase, out (int Parameter, int Property) slot) =>
            Slots.TryGetValue(key, out slot) || (ignoreCase && CaseInsensitiveSlots.TryGetValue(key, out slot));
    }

    private static ReadLookup GetReadLookup(TypeMetadata metadata, string[] names, ToonSerializerOptions options)
    {
        var policy = metadata.FixedNamingPolicy ?? options.PropertyNamingPolicy;

        if ((uint)policy < NamingPolicyCount && metadata.ReadLookups[(int)policy] is { } cached)
        {
            return cached;
        }

        var activator = metadata.Activator;
        var slots = new Dictionary<string, (int Parameter, int Property)>(StringComparer.Ordinal);
        var shared = false;

        for (var i = 0; i < activator.Arguments.Length; i++)
        {
            var argument = activator.Arguments[i];
            var name = argument.Property != null ? names[Array.IndexOf(metadata.Properties, argument.Property)] : argument.Parameter.Name!;
            slots[name] = (i, -1);
        }

        for (var p = 0; p < metadata.Properties.Length; p++)
        {
            if (metadata.Properties[p].Setter == null || activator.BoundToConstructor[p])
            {
                continue;
            }

            if (slots.TryGetValue(names[p], out var slot))
            {
                shared = true;
                slots[names[p]] = (slot.Parameter, p);
            }
            else
            {
                slots[names[p]] = (-1, p);
            }
        }

        var unambiguous = GetCaseInsensitiveNames(metadata, options);
        var caseInsensitive = new Dictionary<string, (int Parameter, int Property)>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, slot) in slots)
        {
            if (unambiguous.Contains(name))
            {
                caseInsensitive[name] = slot;
            }
        }

        var lookup = new ReadLookup(slots, shared, caseInsensitive);

        if ((uint)policy < NamingPolicyCount)
        {
            metadata.ReadLookups[(int)policy] = lookup;
        }

        return lookup;
    }

    /// <summary>
    ///     Reads one document. An instance is used once and is not thread-safe.
    /// </summary>
    private sealed class DirectReader(ToonParser parser, ToonSerializerOptions options, bool generatedOnly = false) : IToonDeserializationHost
    {
        private readonly ToonParser _parser = parser;

        /// <summary>The parser of the document being read.</summary>
        public ToonParser Parser => _parser;
        private readonly ToonSerializerOptions _options = options;

        /// <summary>
        ///     Whether only generated code may convert values (reading with a <see cref="ToonTypeInfo"/>): what would need
        ///     reflection falls back instead.
        /// </summary>
        private readonly bool _generatedOnly = generatedOnly;

        private readonly bool _hasConverters = options.Converters.Count != 0;

        /// <summary>With converters: tells which types generated code can still read.</summary>
        private readonly ConverterCheck? _converterCheck = GetConverterCheck(options);

        /// <summary>Per target type, whether a converter of the options reads it (types are looked up once per document).</summary>
        private Dictionary<Type, bool>? _convertedTypes;

        /// <summary>
        ///     Whether a converter of the options reads values of <paramref name="type"/>, directly or (for a nullable value
        ///     type) through its underlying type, as <see cref="DeserializeValue"/> checks; such values are read from their
        ///     tree.
        /// </summary>
        /// <summary>Whether generated code (if the type has any) may read the type with the options.</summary>
        private bool GeneratedCodeReads(Type type)
        {
            if (_converterCheck != null && !_converterCheck.AllowsGeneratedCode(type))
            {
                return false;
            }

            return _options.AllowsGeneratedReading || FindGeneratedSerializer(type)?.ReadsWith(_options) != false;
        }

        private bool IsConverted(Type type)
        {
            if (!_hasConverters)
            {
                return false;
            }

            _convertedTypes ??= [];

            if (!_convertedTypes.TryGetValue(type, out var converted))
            {
                converted = _options.GetConverter(type) != null ||
                            (Nullable.GetUnderlyingType(type) is { } underlying && _options.GetConverter(underlying) != null);
                _convertedTypes[type] = converted;
            }

            return converted;
        }
        private SerializerState? _state;
        private ToonReader? _reader;

        public ToonSerializerOptions Options => _options;

        public object? ReadValue(in ToonParser.ValueRef value, Type targetType, int depth) => ReadValue(value, targetType, null, depth);

        public void Fallback() => throw new DirectReadFallbackException();

        private ToonReader Reader => _reader ??= new ToonReader(_parser, this);

        public static object? Read(ToonText text, Type type, ToonSerializerOptions options)
        {
            var parser = new ToonParser(options.ToonOptions);
            parser.BeginDocument(text);

            try
            {
                var root = parser.ReadRoot();
                var result = new DirectReader(parser, options).ReadValue(root, type, null, 0);
                parser.EndDocument();
                return result;
            }
            finally
            {
                parser.ReleaseDocument();
            }
        }

        /// <summary>
        ///     Reads one document with a type info, only through generated code; what would need reflection throws, and the
        ///     caller then converts the document with the generated tree-based methods.
        /// </summary>
        public static T ReadGenerated<T>(ToonText text, ToonTypeInfo<T> typeInfo, ToonSerializerOptions options)
        {
            var parser = new ToonParser(options.ToonOptions);
            parser.BeginDocument(text);

            try
            {
                var root = parser.ReadRoot();
                var result = typeInfo.Read!(new DirectReader(parser, options, generatedOnly: true).Reader, new ToonReaderValue(root), 0);
                parser.EndDocument();
                return result;
            }
            finally
            {
                parser.ReleaseDocument();
            }
        }

        /// <summary>Reads a value as <paramref name="targetType"/>, like <see cref="DeserializeValue"/> would from its tree.</summary>
        private object? ReadValue(in ToonParser.ValueRef value, Type targetType, IToonConverter? propertyConverter, int depth)
        {
            if (_generatedOnly)
            {
                return _hasConverters && TryReadConvertedTree(value, targetType, depth, out var converted)
                    ? converted
                    : ReadGeneratedValue(value, GetGeneratedOnlyReadTypeInfo(targetType), depth);
            }

            // Generated code does not consult converters: with converters, it reads only types where none applies.
            // Code from older generators matches keys exactly and checks neither unmapped keys nor required properties:
            // with those options, such types are read with reflection
            var info = GetReadTypeInfo(targetType, _options.UseGeneratedCode && GeneratedCodeReads(Nullable.GetUnderlyingType(targetType) ?? targetType));

            if (propertyConverter != null || info.Kind == ReadKind.Tree || IsConverted(targetType))
            {
                return ReadTree(value, targetType, propertyConverter, depth);
            }

            if (info.Kind == ReadKind.Generated)
            {
                // Generated code reads objects; other values follow the rules for the type, as in DeserializeValue
                return value.NodeKind == ToonNodeKind.Object
                    ? info.Reader!.ReadOf(Reader, new ToonReaderValue(value), depth)
                    : ReadTree(value, targetType, null, depth);
            }

            if (value.NodeKind == ToonNodeKind.Primitive && _parser.IsNull(value))
            {
                return info.AcceptsNull ? null : throw new DirectReadFallbackException();
            }

            switch (info.Kind)
            {
                case ReadKind.Primitive:
                    if (value.NodeKind != ToonNodeKind.Primitive)
                    {
                        throw new DirectReadFallbackException(); // an object or array for a scalar target
                    }

                    if (info.Target == typeof(string))
                    {
                        return ReadString(value);
                    }

                    return DeserializeValue(_parser.ReadPrimitive(value), targetType, _options, depth, State);
                case ReadKind.Object:
                    return ReadObject(value, info, depth);
                case ReadKind.Collection:
                    return ReadCollection(value, info, depth);
                default:
                    return ReadDictionary(value, info, depth);
            }
        }

        /// <summary>
        ///     Reads a value with generated code, as a scalar, or (for <see cref="object"/> and <see cref="ToonValue"/>
        ///     targets) from its tree like <see cref="DeserializeValue"/>; anything else falls back.
        /// </summary>
        private object? ReadGeneratedValue(in ToonParser.ValueRef value, ReadTypeInfo info, int depth)
        {
            if (info.Kind == ReadKind.Generated && value.NodeKind == ToonNodeKind.Object)
            {
                if (!_options.AllowsGeneratedReading && info.Reader!.FeatureLevel < 1)
                {
                    throw new DirectReadFallbackException(); // code from an older generator
                }

                return info.Reader!.ReadOf(Reader, new ToonReaderValue(value), depth);
            }

            if (info.Kind == ReadKind.Tree)
            {
                if (info.Target == typeof(object))
                {
                    return ToUntyped(_parser.BuildValue(value), _options, depth);
                }

                // A ToonValue target takes the tree as it is, ToonNull included
                if (typeof(ToonValue).IsAssignableFrom(info.Target))
                {
                    return _parser.BuildValue(value) is var tree && info.Target.IsInstanceOfType(tree) ? tree : throw new DirectReadFallbackException();
                }

                if (ToonJsonInterop.IsDocumentType(info.Target) && ToonJsonInterop.TryFromToon(_parser.BuildValue(value), info.Target, out var document))
                {
                    return document;
                }
            }

            if (value.NodeKind == ToonNodeKind.Primitive)
            {
                if (_parser.IsNull(value))
                {
                    return info.AcceptsNull ? null : throw new DirectReadFallbackException();
                }

                if (info.Kind == ReadKind.Primitive)
                {
                    if (info.Target == typeof(string))
                    {
                        return ReadString(value);
                    }

                    // The conversions of DeserializeValue for scalars; its errors are reported by the tree-based path
                    return TryDeserializePrimitive(_parser.ReadPrimitive(value), info.Target, State, out var result)
                        ? result
                        : throw new DirectReadFallbackException();
                }
            }

            throw new DirectReadFallbackException();
        }

        /// <summary>
        ///     For reading with a type info and converters: reads a value from its tree with the converter of the options
        ///     for its type (as <see cref="DeserializeValue"/> finds it), or with the generated tree-based method when
        ///     the generated reader would not consult a converter it should; false for values generated code reads
        ///     straight from the text.
        /// </summary>
        private bool TryReadConvertedTree(in ToonParser.ValueRef value, Type targetType, int depth, out object? result)
        {
            var check = _converterCheck!;
            var underlying = Nullable.GetUnderlyingType(targetType);
            var isNull = value.NodeKind == ToonNodeKind.Primitive && _parser.IsNull(value);

            if (check.GetConverter(targetType) is { } converter)
            {
                result = converter.Read(_parser.BuildValue(value), targetType, _options);
                return true;
            }

            if (underlying != null && !isNull && check.GetConverter(underlying) is { } underlyingConverter)
            {
                result = underlyingConverter.Read(_parser.BuildValue(value), underlying, _options);
                return true;
            }

            var target = underlying ?? targetType;

            if (FindGeneratedSerializer(target) is { Deserialize: { } deserialize } generated && !check.AllowsGeneratedCode(target))
            {
                if (isNull && (underlying != null || !target.IsValueType))
                {
                    result = null;
                    return true;
                }

                // Code from older generators hands converters to reflection; other non-object values are reported by
                // the tree-based path
                if (generated.FeatureLevel < 1 || value.NodeKind != ToonNodeKind.Object)
                {
                    throw new DirectReadFallbackException();
                }

                result = deserialize(_parser.BuildValue(value), _options, depth);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>Builds the value's tree and converts it with <see cref="DeserializeValue"/>.</summary>
        private object? ReadTree(in ToonParser.ValueRef value, Type targetType, IToonConverter? propertyConverter, int depth)
        {
            return DeserializeValue(_parser.BuildValue(value), targetType, _options, depth, State, propertyConverter);
        }

        private SerializerState State => _state ??= new SerializerState();

        /// <summary>The checks <see cref="DeserializeValue"/> makes before reading an object, collection or dictionary.</summary>
        private void EnterValue(int depth)
        {
            if (depth > _options.ValidatedMaxDepth || !RuntimeHelpers.TryEnsureSufficientExecutionStack())
            {
                throw new DirectReadFallbackException();
            }
        }

        private object? ReadString(in ToonParser.ValueRef value)
        {
            return TryReadToken(_parser, value, out string? text) ? text : throw new DirectReadFallbackException();
        }

        #region Objects

        private object ReadObject(in ToonParser.ValueRef value, ReadTypeInfo info, int depth)
        {
            var targetType = info.Target;
            var metadata = info.Metadata!;

            if (value.NodeKind != ToonNodeKind.Object || targetType.IsAbstract || targetType.IsInterface ||
                (metadata.Constructor == null && !targetType.IsValueType))
            {
                throw new DirectReadFallbackException();
            }

            EnterValue(depth);

            var names = GetDistinctPropertyNames(metadata, _options) ?? throw new DirectReadFallbackException();
            var lookup = GetReadLookup(metadata, names, _options);

            return metadata.Activator.Arguments.Length == 0
                ? ReadWithSetters(value, metadata, lookup, depth)
                : ReadWithConstructor(value, metadata, lookup, depth);
        }

        /// <summary>
        ///     Creates the instance, then sets properties as their keys are read, in any order. The object ends up as the
        ///     tree-based path makes it (which sets properties in property order): a repeated key (non-strict mode) sets
        ///     the last value; only the order in which setters run follows the document.
        /// </summary>
        private object ReadWithSetters(in ToonParser.ValueRef value, TypeMetadata metadata, ReadLookup lookup, int depth)
        {
            var instance = metadata.Activator.New([]);
            var properties = metadata.Properties;
            var seen = RequiredTracking(metadata);
            var cursor = _parser.BeginObject(value);

            while (_parser.NextProperty(ref cursor, out var key, out var fieldValue))
            {
                if (!lookup.TryGetSlot(key, _options.PropertyNameCaseInsensitive, out var slot) || slot.Property < 0)
                {
                    SkipUnmapped(metadata, key, fieldValue);
                    continue;
                }

                if (seen != null)
                {
                    seen[slot.Property] = true;
                }

                var property = properties[slot.Property];

                if (property.Primitive is { } primitive && property.Converter == null && (!_hasConverters || !IsConverted(property.Property.PropertyType)) &&
                    primitive.TryReadToken(instance, _parser, fieldValue))
                {
                    continue;
                }

                property.Setter!(instance, ReadValue(fieldValue, property.Property.PropertyType, property.Converter, depth + 1));
            }

            CheckRequired(metadata, seen);
            return instance;
        }

        /// <summary>Which properties had a key, when required properties are checked; otherwise null.</summary>
        private bool[]? RequiredTracking(TypeMetadata metadata) =>
            _options.RespectRequiredMembers && metadata.RequiredProperties.Length > 0 ? new bool[metadata.Properties.Length] : null;

        /// <summary>A missing required property is reported by the tree-based path, with its path.</summary>
        private static void CheckRequired(TypeMetadata metadata, bool[]? seen)
        {
            if (seen == null)
            {
                return;
            }

            foreach (var p in metadata.RequiredProperties)
            {
                if (!seen[p])
                {
                    throw new DirectReadFallbackException();
                }
            }
        }

        /// <summary>Skips a key that fills nothing; one that matches no member at all is reported by the tree-based path when unmapped keys are disallowed.</summary>
        private void SkipUnmapped(TypeMetadata metadata, string key, in ToonParser.ValueRef fieldValue)
        {
            if (_options.UnmappedMemberHandling == ToonUnmappedMemberHandling.Disallow && !IsMappedKey(metadata, _options, key))
            {
                throw new DirectReadFallbackException();
            }

            _parser.Skip(fieldValue);
        }

        /// <summary>
        ///     Reads constructor arguments and the remaining settable properties, creates the instance, then sets the
        ///     properties in property order.
        /// </summary>
        private object ReadWithConstructor(in ToonParser.ValueRef value, TypeMetadata metadata, ReadLookup lookup, int depth)
        {
            if (lookup.HasSharedKeys)
            {
                throw new DirectReadFallbackException();
            }

            var activator = metadata.Activator;
            var properties = metadata.Properties;
            var arguments = new object?[activator.Arguments.Length];
            var argumentRead = new bool[arguments.Length];
            object?[]? pending = null;
            bool[]? pendingRead = null;
            var seen = RequiredTracking(metadata);
            var cursor = _parser.BeginObject(value);

            while (_parser.NextProperty(ref cursor, out var key, out var fieldValue))
            {
                if (!lookup.TryGetSlot(key, _options.PropertyNameCaseInsensitive, out var slot))
                {
                    SkipUnmapped(metadata, key, fieldValue);
                    continue;
                }

                if (slot.Parameter >= 0)
                {
                    if (argumentRead[slot.Parameter])
                    {
                        throw new DirectReadFallbackException(); // a repeated key
                    }

                    var argument = activator.Arguments[slot.Parameter];

                    if (seen != null && argument.Property != null)
                    {
                        seen[Array.IndexOf(properties, argument.Property)] = true;
                    }

                    arguments[slot.Parameter] = ReadValue(fieldValue, argument.Parameter.ParameterType, argument.Property?.Converter, depth + 1);
                    argumentRead[slot.Parameter] = true;
                }
                else
                {
                    pending ??= new object?[properties.Length];
                    pendingRead ??= new bool[properties.Length];

                    if (pendingRead[slot.Property])
                    {
                        throw new DirectReadFallbackException();
                    }

                    var property = properties[slot.Property];
                    pending[slot.Property] = ReadValue(fieldValue, property.Property.PropertyType, property.Converter, depth + 1);
                    pendingRead[slot.Property] = true;

                    if (seen != null)
                    {
                        seen[slot.Property] = true;
                    }
                }
            }

            CheckRequired(metadata, seen);

            for (var i = 0; i < arguments.Length; i++)
            {
                if (!argumentRead[i])
                {
                    arguments[i] = activator.Arguments[i].MissingValue;
                }
            }

            var instance = activator.New(arguments);

            if (pendingRead != null)
            {
                for (var p = 0; p < properties.Length; p++)
                {
                    if (pendingRead[p])
                    {
                        properties[p].Setter!(instance, pending![p]);
                    }
                }
            }

            return instance;
        }

        #endregion

        #region Collections and dictionaries

        private object ReadCollection(in ToonParser.ValueRef value, ReadTypeInfo info, int depth)
        {
            if (value.NodeKind != ToonNodeKind.Array)
            {
                throw new DirectReadFallbackException();
            }

            EnterValue(depth);

            var factory = info.Collection!;
            var elementType = info.ElementType!;
            var cursor = _parser.BeginArray(value);
            var items = factory.Helpers.CreateList(cursor.CapacityHint);
            var primitiveItems = factory.HasPrimitiveItems && !IsConverted(elementType);

            while (_parser.NextItem(ref cursor, out var item))
            {
                if (primitiveItems && factory.Helpers.TryAddToken(items, _parser, item))
                {
                    continue;
                }

                items.Add(ReadValue(item, elementType, null, depth + 1));
            }

            return factory.Complete(items) ?? throw new DirectReadFallbackException();
        }

        private object ReadDictionary(in ToonParser.ValueRef value, ReadTypeInfo info, int depth)
        {
            if (value.NodeKind != ToonNodeKind.Object)
            {
                throw new DirectReadFallbackException();
            }

            EnterValue(depth);

            var factory = info.Dictionary!;
            var dictionary = factory.CreateEntries();
            var keyType = factory.KeyType;
            var typedKeys = factory.HasTypedKeys && !IsConverted(keyType);
            var primitiveValues = factory.HasPrimitiveValues && !IsConverted(factory.ValueType);
            var cursor = _parser.BeginObject(value);

            while (_parser.NextProperty(ref cursor, out var key, out var fieldValue))
            {
                // Keys and primitive values without boxing; other keys (and their errors) take the general path
                if (typedKeys && factory.Helpers.TryAddEntry(this, dictionary, key, fieldValue, primitiveValues, depth + 1))
                {
                    continue;
                }

                var typedKey = keyType == typeof(string)
                    ? key
                    : DeserializeValue(new ToonString(key), keyType, _options, depth + 1, State)!;

                dictionary[typedKey] = ReadValue(fieldValue, factory.ValueType, null, depth + 1);
            }

            return factory.Complete(dictionary) ?? throw new DirectReadFallbackException();
        }

        #endregion
    }

    /// <summary>
    ///     Reads a primitive token as <typeparamref name="T"/> (or its nullable form) without boxing, giving what
    ///     <see cref="DeserializeValue"/> gives for the token's <see cref="ToonValue"/>: exact matches only, everything
    ///     else returns <c>false</c> for the general path.
    /// </summary>
    internal static bool TryReadToken<T>(ToonParser parser, in ToonParser.ValueRef value, out T result)
    {
        result = default!;

        if (value.NodeKind != ToonNodeKind.Primitive)
        {
            return false;
        }

        if (parser.IsNull(value))
        {
            return default(T) == null; // null for reference and nullable types
        }

        var token = parser.TokenOf(value);

        if (typeof(T) == typeof(string))
        {
            string text;

            if (token.Length > 0 && token[0] == '"')
            {
                text = parser.ReadQuotedString(value);
            }
            else if (token.SequenceEqual("true") || token.SequenceEqual("false"))
            {
                text = token[0] == 't' ? "true" : "false";
            }
            else if (ToonParser.TryParseNumberParts(token, out var number, out var exact, out var hasExact))
            {
                // A number read into a string is written in canonical form, as ToonNumber.ToString does
                text = hasExact ? ToonNumberFormatter.Format(exact) : ToonNumberFormatter.Format(number)!;
            }
            else
            {
                text = token.ToString();
            }

            result = Unsafe.As<string, T>(ref text);
            return true;
        }

        if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
        {
            return token.SequenceEqual("true") ? SetToken(true, out result) : token.SequenceEqual("false") && SetToken(false, out result);
        }

        // Types read from string values: a quoted token (without escapes, which take the general path) or an unquoted
        // one that is not a number or boolean, parsed with the rules of TryDeserializePrimitive
        if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?) || typeof(T) == typeof(DateTimeOffset) ||
            typeof(T) == typeof(DateTimeOffset?) || typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?) ||
            typeof(T) == typeof(DateOnly) || typeof(T) == typeof(DateOnly?) || typeof(T) == typeof(TimeOnly) ||
            typeof(T) == typeof(TimeOnly?) || typeof(T) == typeof(TimeSpan) || typeof(T) == typeof(TimeSpan?) ||
            (Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)).IsEnum)
        {
            if (!TryGetStringToken(token, out var text))
            {
                return false;
            }

            var invariant = System.Globalization.CultureInfo.InvariantCulture;

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                return DateTime.TryParse(text, invariant, System.Globalization.DateTimeStyles.RoundtripKind, out var dateTime) &&
                       SetToken(dateTime, out result);
            }

            if (typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTimeOffset?))
            {
                return DateTimeOffset.TryParse(text, invariant, System.Globalization.DateTimeStyles.RoundtripKind, out var offset) &&
                       SetToken(offset, out result);
            }

            if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
            {
                return Guid.TryParse(text, out var guid) && SetToken(guid, out result);
            }

            if (typeof(T) == typeof(DateOnly) || typeof(T) == typeof(DateOnly?))
            {
                return DateOnly.TryParse(text, invariant, System.Globalization.DateTimeStyles.None, out var date) && SetToken(date, out result);
            }

            if (typeof(T) == typeof(TimeOnly) || typeof(T) == typeof(TimeOnly?))
            {
                return TimeOnly.TryParse(text, invariant, System.Globalization.DateTimeStyles.None, out var time) && SetToken(time, out result);
            }

            if (typeof(T) == typeof(TimeSpan) || typeof(T) == typeof(TimeSpan?))
            {
                return TimeSpan.TryParse(text, invariant, out var span) && SetToken(span, out result);
            }

            // Enums: exact names without boxing, else names (case-insensitive), numeric strings and flag lists, like
            // Enum.Parse
            if (typeof(T).IsEnum && EnumText<T>.TryParse(text, out result))
            {
                return true;
            }

            if (!Enum.TryParse(Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), text, ignoreCase: true, out var parsedEnum))
            {
                return false;
            }

            result = (T)parsedEnum!;
            return true;
        }

        if (token.Length == 0 || token[0] == '"' || !ToonParser.TryParseNumberParts(token, out var parsed, out var decimalValue, out var isExact))
        {
            return false;
        }

        var asDouble = parsed; // the double closest to the text, as ToonNumber.Value

        if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
        {
            return SetToken(asDouble, out result);
        }

        if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
        {
            var single = (float)asDouble;
            return float.IsFinite(single) && SetToken(single, out result);
        }

        if (!isExact)
        {
            return false;
        }

        if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
        {
            return SetToken(decimalValue, out result);
        }

        if (decimalValue != decimal.Truncate(decimalValue))
        {
            return false;
        }

        if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
        {
            return decimalValue is >= int.MinValue and <= int.MaxValue && SetToken((int)decimalValue, out result);
        }

        if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
        {
            return decimalValue is >= long.MinValue and <= long.MaxValue && SetToken((long)decimalValue, out result);
        }

        if (typeof(T) == typeof(short) || typeof(T) == typeof(short?))
        {
            return decimalValue is >= short.MinValue and <= short.MaxValue && SetToken((short)decimalValue, out result);
        }

        if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
        {
            return decimalValue is >= byte.MinValue and <= byte.MaxValue && SetToken((byte)decimalValue, out result);
        }

        if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(sbyte?))
        {
            return decimalValue is >= sbyte.MinValue and <= sbyte.MaxValue && SetToken((sbyte)decimalValue, out result);
        }

        if (typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?))
        {
            return decimalValue is >= ushort.MinValue and <= ushort.MaxValue && SetToken((ushort)decimalValue, out result);
        }

        if (typeof(T) == typeof(uint) || typeof(T) == typeof(uint?))
        {
            return decimalValue is >= uint.MinValue and <= uint.MaxValue && SetToken((uint)decimalValue, out result);
        }

        if (typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?))
        {
            return decimalValue is >= ulong.MinValue and <= ulong.MaxValue && SetToken((ulong)decimalValue, out result);
        }

        return false;

        // The text of a string token: inside the quotes when there are no escapes, or an unquoted token that is not a
        // number or boolean (those are other TOON types, which the general path reports)
        static bool TryGetStringToken(ReadOnlySpan<char> token, out ReadOnlySpan<char> text)
        {
            text = default;

            if (token.Length == 0)
            {
                return false;
            }

            if (token[0] == '"')
            {
                if (token.Length < 2 || token[^1] != '"' || token[1..^1].IndexOfAny('\\', '"') >= 0)
                {
                    return false;
                }

                text = token[1..^1];
                return true;
            }

            if (token.SequenceEqual("true") || token.SequenceEqual("false") || ToonParser.TryParseNumberParts(token, out _, out _, out _))
            {
                return false;
            }

            text = token;
            return true;
        }

        // Stores a primitive in T, which is that primitive or its Nullable<>
        static bool SetToken<TPrimitive>(TPrimitive primitive, out T target) where TPrimitive : struct
        {
            if (typeof(T) == typeof(TPrimitive))
            {
                target = Unsafe.As<TPrimitive, T>(ref primitive);
            }
            else
            {
                TPrimitive? nullable = primitive;
                target = Unsafe.As<TPrimitive?, T>(ref nullable);
            }

            return true;
        }
    }
}
