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

    /// <summary>Deserializes a document, reading straight from the text when the options allow it.</summary>
    private static object? DeserializeText(string toonString, Type type, ToonSerializerOptions options)
    {
        if (options.Converters.Count == 0)
        {
            try
            {
                return DirectReader.Read(toonString, type, options);
            }
            catch
            {
                // Report the error exactly as the tree-based path does (or read what direct reading gave up on)
                DirectReadFallbacks++;
            }
        }

        var document = new ToonParser(options.ToonOptions).Parse(toonString);
        return DeserializeValue(document.Root, type, options, 0, new SerializerState());
    }

    /// <summary>Thrown when direct reading cannot produce the tree-based result; the tree-based path runs instead.</summary>
    private sealed class DirectReadFallbackException : Exception;

    private enum ReadKind : byte
    {
        Tree,
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

    private static ReadTypeInfo CreateReadTypeInfo(Type t, bool useGeneratedCode)
    {
        {
            var underlying = Nullable.GetUnderlyingType(t);
            var target = underlying ?? t;
            var acceptsNull = !t.IsValueType || underlying != null;

            ReadTypeInfo Info(ReadKind kind) => new() { Kind = kind, Target = target, AcceptsNull = acceptsNull };

            if (typeof(ToonValue).IsAssignableFrom(t) || target == typeof(object))
            {
                return Info(ReadKind.Tree);
            }

            if (IsScalarType(target))
            {
                return Info(ReadKind.Primitive);
            }

            // Types with generated code are read by it, without reflection metadata (DeserializeValue decides when)
            if (useGeneratedCode && FindGeneratedSerializer(target)?.Deserialize != null)
            {
                return new ReadTypeInfo { Kind = ReadKind.Tree, Target = target, AcceptsNull = acceptsNull, HasGeneratedReader = true };
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
    private sealed class ReadLookup(Dictionary<string, (int Parameter, int Property)> slots, bool shared)
    {
        public Dictionary<string, (int Parameter, int Property)> Slots { get; } = slots;

        /// <summary>Whether a key fills both a parameter and a property (read separately by the tree-based path).</summary>
        public bool HasSharedKeys { get; } = shared;
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

        var lookup = new ReadLookup(slots, shared);

        if ((uint)policy < NamingPolicyCount)
        {
            metadata.ReadLookups[(int)policy] = lookup;
        }

        return lookup;
    }

    /// <summary>
    ///     Reads one document. An instance is used once and is not thread-safe.
    /// </summary>
    private sealed class DirectReader(ToonParser parser, ToonSerializerOptions options)
    {
        private readonly ToonParser _parser = parser;
        private readonly ToonSerializerOptions _options = options;
        private SerializerState? _state;

        public static object? Read(string text, Type type, ToonSerializerOptions options)
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

        /// <summary>Reads a value as <paramref name="targetType"/>, like <see cref="DeserializeValue"/> would from its tree.</summary>
        private object? ReadValue(in ToonParser.ValueRef value, Type targetType, IToonConverter? propertyConverter, int depth)
        {
            var info = GetReadTypeInfo(targetType, _options.UseGeneratedCode);

            if (propertyConverter != null || info.Kind == ReadKind.Tree)
            {
                return ReadTree(value, targetType, propertyConverter, depth);
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
        ///     Creates the instance, then sets properties as their keys are read. Keys must come in property order, as
        ///     the tree-based path sets properties in that order.
        /// </summary>
        private object ReadWithSetters(in ToonParser.ValueRef value, TypeMetadata metadata, ReadLookup lookup, int depth)
        {
            var instance = metadata.Activator.New([]);
            var properties = metadata.Properties;
            var cursor = _parser.BeginObject(value);
            var last = -1;

            while (_parser.NextProperty(ref cursor, out var key, out var fieldValue))
            {
                if (!lookup.Slots.TryGetValue(key, out var slot) || slot.Property < 0)
                {
                    _parser.Skip(fieldValue);
                    continue;
                }

                if (slot.Property <= last)
                {
                    throw new DirectReadFallbackException(); // another order, or a repeated key
                }

                last = slot.Property;
                var property = properties[slot.Property];

                if (property.Primitive is { } primitive && property.Converter == null && primitive.TryReadToken(instance, _parser, fieldValue))
                {
                    continue;
                }

                property.Setter!(instance, ReadValue(fieldValue, property.Property.PropertyType, property.Converter, depth + 1));
            }

            return instance;
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
            var cursor = _parser.BeginObject(value);

            while (_parser.NextProperty(ref cursor, out var key, out var fieldValue))
            {
                if (!lookup.Slots.TryGetValue(key, out var slot))
                {
                    _parser.Skip(fieldValue);
                    continue;
                }

                if (slot.Parameter >= 0)
                {
                    if (argumentRead[slot.Parameter])
                    {
                        throw new DirectReadFallbackException(); // a repeated key
                    }

                    var argument = activator.Arguments[slot.Parameter];
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
                }
            }

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

            while (_parser.NextItem(ref cursor, out var item))
            {
                if (factory.HasPrimitiveItems && factory.Helpers.TryAddToken(items, _parser, item))
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
            var cursor = _parser.BeginObject(value);

            while (_parser.NextProperty(ref cursor, out var key, out var fieldValue))
            {
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
    private static bool TryReadToken<T>(ToonParser parser, in ToonParser.ValueRef value, out T result)
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

        if (token.Length == 0 || token[0] == '"' || !ToonParser.TryParseNumberParts(token, out var parsed, out var decimalValue, out var isExact))
        {
            return false;
        }

        var asDouble = isExact ? (double)decimalValue : parsed;

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
