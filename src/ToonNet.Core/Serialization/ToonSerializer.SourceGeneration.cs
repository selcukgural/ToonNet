using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Core.Serialization;

// Internal entry points behind ToonSourceGenerationHelpers, so generated code and the reflection-based serializer
// share one implementation of each conversion rule.
public static partial class ToonSerializer
{
    /// <summary>
    ///     The generated methods of a <c>[ToonSerializable]</c> type, registered by its module initializer.
    /// </summary>
    private sealed class GeneratedSerializer(Type type,
                                             Func<object, ToonSerializerOptions, int, ToonValue> serialize,
                                             Func<ToonValue, ToonSerializerOptions, int, object?>? deserialize,
                                             ToonObjectWriter? writer,
                                             ToonObjectReader? reader)
    {
        /// <summary>Writes objects straight to text; null for code from a generator without writers.</summary>
        public ToonObjectWriter? Writer { get; } = writer;

        /// <summary>Reads objects straight from text; null for code from a generator without readers.</summary>
        public ToonObjectReader? Reader { get; } = reader;

        public Func<object, ToonSerializerOptions, int, ToonValue> Serialize { get; } = serialize;

        /// <summary>Null when the type has no constructor generated code can use.</summary>
        public Func<ToonValue, ToonSerializerOptions, int, object?>? Deserialize { get; } = deserialize;

        /// <summary>What the generated code of this type applies itself (see <see cref="ToonObjectWriter.FeatureLevel"/>).</summary>
        public int OwnFeatureLevel { get; } = Math.Max(writer?.FeatureLevel ?? 0, reader?.FeatureLevel ?? 0);

        /// <summary>
        ///     What the generated code applies, counting the generated code of the types it calls directly (which may come
        ///     from an older generator in another assembly).
        /// </summary>
        public int FeatureLevel => GetFeatureLevel(type);

        /// <summary>Whether the generated code may read with the options (code from older generators ignores some).</summary>
        public bool ReadsWith(ToonSerializerOptions options) => options.AllowsGeneratedReading || FeatureLevel >= 1;
    }

    private static readonly ConcurrentDictionary<Type, GeneratedSerializer> RegisteredGeneratedSerializers = new();

    /// <summary>
    ///     The generated serializer per type, or null; caches the lookup for types without one.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, GeneratedSerializer?> GeneratedSerializerLookup = new();

    internal static void RegisterGenerated<T>(Func<T, ToonSerializerOptions, int, ToonValue> serialize,
                                              Func<ToonValue, ToonSerializerOptions, int, T>? deserialize,
                                              ToonObjectWriter<T>? writer,
                                              ToonObjectReader<T>? reader)
    {
        ArgumentNullException.ThrowIfNull(serialize);

        // Only the generated code of a type registers it (its module initializer, with methods of the type's module),
        // and the first registration stays: other code cannot replace how a type, or a type of the framework, is
        // written and read for the whole process
        if (serialize.Method.Module != typeof(T).Module || (deserialize != null && deserialize.Method.Module != typeof(T).Module))
        {
            throw new InvalidOperationException($"Generated code for {typeof(T).Name} can only be registered by code in the module of the type.");
        }

        if (RegisteredGeneratedSerializers.ContainsKey(typeof(T)))
        {
            return;
        }

        RegisteredGeneratedSerializers[typeof(T)] = new GeneratedSerializer(
            typeof(T),
            (value, options, depth) => serialize((T)value, options, depth),
            deserialize == null ? null : (value, options, depth) => deserialize(value, options, depth),
            writer,
            reader);

        // Code from a generator without readers reads through the tree
        ToonTypeInfo<T>.Generated = new ToonTypeInfo<T>(
            serialize,
            deserialize,
            deserialize == null ? null : reader != null ? reader.Read : (ToonReader r, in ToonReaderValue v, int depth) => deserialize(r.BuildTree(in v), r.Options, depth),
            static () => GetFeatureLevel(typeof(T)),
            static check => check.AllowsGeneratedCode(typeof(T)));
    }

    /// <summary>Per type with generated code, the lowest feature level of its generated code and what it calls directly.</summary>
    private static readonly ConcurrentDictionary<Type, int> FeatureLevels = new();

    /// <summary>
    ///     The lowest <see cref="ToonObjectWriter.FeatureLevel"/> of the generated code of a type and of the types with
    ///     generated code it reaches (listed in <see cref="ToonObjectWriter.InlineTypes"/>), whose generated methods it
    ///     calls directly; 0 for a type without generated code.
    /// </summary>
    private static int GetFeatureLevel(Type type) => FeatureLevels.GetOrAdd(type, static t =>
    {
        var level = int.MaxValue;
        var pending = new Stack<Type>();
        var visited = new HashSet<Type> { t };
        pending.Push(t);

        while (pending.TryPop(out var current))
        {
            if (FindGeneratedSerializer(current) is not { } generated)
            {
                continue;
            }

            level = Math.Min(level, generated.OwnFeatureLevel);

            foreach (var inlineType in generated.Writer?.InlineTypes ?? [])
            {
                var target = Nullable.GetUnderlyingType(inlineType) ?? inlineType;

                if (visited.Add(target))
                {
                    pending.Push(target);
                }
            }
        }

        return level == int.MaxValue ? 0 : level;
    });

    /// <summary>
    ///     Finds the generated serializer for exactly <paramref name="type"/>.
    /// </summary>
    /// <remarks>
    ///     Generated types register themselves from a module initializer. Getting a <see cref="Type"/> does not run it
    ///     (for example <c>Deserialize&lt;T&gt;</c> before anything else in the module is used), so it is run here for
    ///     types marked <see cref="ToonSerializableAttribute"/>.
    /// </remarks>
    private static GeneratedSerializer? FindGeneratedSerializer(Type type)
    {
        if (RegisteredGeneratedSerializers.TryGetValue(type, out var registered))
        {
            return registered;
        }

        return GeneratedSerializerLookup.GetOrAdd(type, static t =>
        {
            if (!t.IsDefined(typeof(ToonSerializableAttribute), inherit: false))
            {
                return null;
            }

            RuntimeHelpers.RunModuleConstructor(t.Module.ModuleHandle);
            return RegisteredGeneratedSerializers.GetValueOrDefault(t);
        });
    }

    /// <summary>
    ///     The generated serializer to use for <paramref name="type"/>, if any. Generated code produces the same result
    ///     as this serializer; code from older generators defers to it when the options contain converters.
    /// </summary>
    private static GeneratedSerializer? GetGeneratedSerializer(Type type, ToonSerializerOptions options)
    {
        return options.UseGeneratedCode && FindGeneratedSerializer(type) is { } generated && (options.Converters.Count == 0 || generated.FeatureLevel >= 1)
            ? generated
            : null;
    }

    /// <summary>
    ///     Tells, for options with converters, whether generated code can convert a type: no converter may apply to the
    ///     type or to anything its generated code (and the generated code of the types it reaches) converts itself.
    /// </summary>
    internal sealed class ConverterCheck(IToonConverter[] converters)
    {
        private readonly ConcurrentDictionary<Type, bool> _allowed = new();
        private readonly ConcurrentDictionary<Type, IToonConverter?> _converters = new();

        /// <summary>The first converter that can convert the type, as <see cref="ToonSerializerOptions.GetConverter"/> finds it.</summary>
        public IToonConverter? GetConverter(Type type) => _converters.GetOrAdd(type, static (t, all) => Array.Find(all, c => c.CanConvert(t)), converters);

        /// <summary>Whether the check was made for exactly these converters (the list can change between calls).</summary>
        public bool IsFor(List<IToonConverter> current)
        {
            if (current.Count != converters.Length)
            {
                return false;
            }

            for (var i = 0; i < converters.Length; i++)
            {
                if (!ReferenceEquals(current[i], converters[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool AllowsGeneratedCode(Type type) => _allowed.GetOrAdd(type, static (t, check) => check.Compute(t), this);

        private bool Compute(Type type)
        {
            var pending = new Stack<Type>();
            var visited = new HashSet<Type> { type };
            pending.Push(type);

            while (pending.TryPop(out var current))
            {
                // Code from generators that do not list what they convert is not used with converters
                if (IsConverted(current) || FindGeneratedSerializer(current)?.Writer?.InlineTypes is not { } inlineTypes)
                {
                    return false;
                }

                foreach (var inlineType in inlineTypes)
                {
                    if (IsConverted(inlineType))
                    {
                        return false;
                    }

                    var target = Nullable.GetUnderlyingType(inlineType) ?? inlineType;

                    if (target != current && FindGeneratedSerializer(target) != null && visited.Add(target))
                    {
                        pending.Push(target);
                    }
                }
            }

            return true;
        }

        /// <summary>Whether a converter reads or writes the type, directly or through a nullable's underlying type.</summary>
        public bool IsConverted(Type type) =>
            GetConverter(type) != null || (Nullable.GetUnderlyingType(type) is { } underlying && GetConverter(underlying) != null);
    }

    /// <summary>
    ///     Writes a non-null value with the converter of the options for it, as <see cref="SerializeValue"/> finds it: for
    ///     its runtime type, else (a property value, <paramref name="useDeclaredType"/>) its declared type; false when no
    ///     converter applies. Items and dictionary values are looked up by their runtime type only.
    /// </summary>
    internal static bool TryWriteConverted<T>(T value, ToonSerializerOptions options, bool useDeclaredType, out ToonValue? result)
    {
        result = null;

        if (value is null || GetConverterCheck(options) is not { } check)
        {
            return false;
        }

        // A value type is its own runtime type (a nullable's value its underlying type), without boxing to ask
        var declared = typeof(T);
        var runtime = declared.IsValueType ? Nullable.GetUnderlyingType(declared) ?? declared : value.GetType();

        if ((check.GetConverter(runtime) ?? (useDeclaredType && runtime != declared ? check.GetConverter(declared) : null)) is not { } converter)
        {
            return false;
        }

        result = converter.Write(value, options);
        return true;
    }

    /// <summary>
    ///     Reads a value with the converter of the options for <typeparamref name="T"/> (or, for a non-null value of a
    ///     nullable type, its underlying type), as <see cref="DeserializeValue"/> does; false when no converter applies.
    /// </summary>
    internal static bool TryReadConverted<T>(ToonValue value, ToonSerializerOptions options, out T result)
    {
        result = default!;

        if (GetConverterCheck(options) is not { } check)
        {
            return false;
        }

        var target = typeof(T);

        if (check.GetConverter(target) is { } converter)
        {
            result = (T)converter.Read(value, target, options)!;
            return true;
        }

        if (value is not ToonNull && Nullable.GetUnderlyingType(target) is { } underlying && check.GetConverter(underlying) is { } underlyingConverter)
        {
            result = (T)underlyingConverter.Read(value, underlying, options)!;
            return true;
        }

        return false;
    }

    /// <summary>The converter check for the options' current converters, or null when they have none.</summary>
    internal static ConverterCheck? GetConverterCheck(ToonSerializerOptions options)
    {
        if (options.Converters.Count == 0)
        {
            return null;
        }

        var check = options.ConverterCheck;

        if (check == null || !check.IsFor(options.Converters))
        {
            options.ConverterCheck = check = new ConverterCheck([.. options.Converters]);
        }

        return check;
    }

    internal static ToonValue WritePrimitiveForGeneratedCode<T>(T value)
    {
        if (value is null)
        {
            return ToonNull.Instance;
        }

        // Typed conversion for the common primitives (no boxing); other types go through TrySerializePrimitive
        if (PrimitiveAccessor.IsTypedPrimitive(Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)))
        {
            return WriteTyped(value)!;
        }

        return TrySerializePrimitive(value, out var result)
            ? result!
            : throw new ArgumentException($"{typeof(T).Name} is not a primitive TOON type", nameof(value));
    }

    internal static T ReadPrimitiveForGeneratedCode<T>(ToonValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var targetType = typeof(T);
        var state = SerializerState.Root;
        var underlyingType = Nullable.GetUnderlyingType(targetType);

        if (value is ToonNull)
        {
            if (targetType.IsValueType && underlyingType == null)
            {
                throw Error($"Cannot convert null to the non-nullable type {targetType.Name}", targetType, value, state);
            }

            return default!;
        }

        if (TryDeserializePrimitive(value, underlyingType ?? targetType, state, out var result))
        {
            return (T)result!;
        }

        throw Error($"Cannot convert {value.ValueType} to {(underlyingType ?? targetType).Name}", underlyingType ?? targetType, value, state);
    }

    internal static string FormatDictionaryKeyForGeneratedCode(object key) => FormatDictionaryKey(key);

    internal static ToonSerializationException UnmappedKeyErrorForGeneratedCode(string key, Type targetType, ToonValue value) =>
        UnmappedKeyError(key, targetType, value, new SerializerState());

    internal static ToonSerializationException MissingRequiredErrorForGeneratedCode(string name, Type targetType, ToonValue value) =>
        MissingRequiredError(name, targetType, value, SerializerState.Root);

    internal static ToonValue SerializeForGeneratedCode(object? value, Type type, ToonSerializerOptions options, int depth)
    {
        return SerializeValue(value, type, options, depth, new SerializerState { FromGeneratedCode = true }) ?? ToonNull.Instance;
    }

    internal static object? DeserializeForGeneratedCode(ToonValue value, Type type, ToonSerializerOptions options, int depth)
    {
        return DeserializeValue(value, type, options, depth, new SerializerState { FromGeneratedCode = true });
    }
}
