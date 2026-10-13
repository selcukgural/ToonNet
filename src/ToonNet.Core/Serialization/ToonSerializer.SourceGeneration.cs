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
    private sealed class GeneratedSerializer(Func<object, ToonSerializerOptions, int, ToonValue> serialize,
                                             Func<ToonValue, ToonSerializerOptions, int, object?>? deserialize)
    {
        public Func<object, ToonSerializerOptions, int, ToonValue> Serialize { get; } = serialize;

        /// <summary>Null when the type has no constructor generated code can use.</summary>
        public Func<ToonValue, ToonSerializerOptions, int, object?>? Deserialize { get; } = deserialize;
    }

    private static readonly ConcurrentDictionary<Type, GeneratedSerializer> RegisteredGeneratedSerializers = new();

    /// <summary>
    ///     The generated serializer per type, or null; caches the lookup for types without one.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, GeneratedSerializer?> GeneratedSerializerLookup = new();

    internal static void RegisterGenerated<T>(Func<T, ToonSerializerOptions, int, ToonValue> serialize,
                                              Func<ToonValue, ToonSerializerOptions, int, T>? deserialize)
    {
        ArgumentNullException.ThrowIfNull(serialize);

        RegisteredGeneratedSerializers[typeof(T)] = new GeneratedSerializer(
            (value, options, depth) => serialize((T)value, options, depth),
            deserialize == null ? null : (value, options, depth) => deserialize(value, options, depth));
    }

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
    ///     as this serializer but defers to it when the options contain converters.
    /// </summary>
    private static GeneratedSerializer? GetGeneratedSerializer(Type type, ToonSerializerOptions options)
    {
        return options.Converters.Count == 0 && options.UseGeneratedCode ? FindGeneratedSerializer(type) : null;
    }

    internal static ToonValue WritePrimitiveForGeneratedCode<T>(T value)
    {
        if (value is null)
        {
            return ToonNull.Instance;
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

        throw Error($"Cannot convert {value.ValueType} to {(underlyingType ?? targetType).Name}", targetType, value, state);
    }

    internal static string FormatDictionaryKeyForGeneratedCode(object key) => FormatDictionaryKey(key);

    internal static ToonValue SerializeForGeneratedCode(object? value, Type type, ToonSerializerOptions options, int depth)
    {
        return SerializeValue(value, type, options, depth, new SerializerState { FromGeneratedCode = true }) ?? ToonNull.Instance;
    }

    internal static object? DeserializeForGeneratedCode(ToonValue value, Type type, ToonSerializerOptions options, int depth)
    {
        return DeserializeValue(value, type, options, depth, new SerializerState { FromGeneratedCode = true });
    }
}
