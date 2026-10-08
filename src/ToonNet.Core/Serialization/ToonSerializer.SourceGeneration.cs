using ToonNet.Core.Models;

namespace ToonNet.Core.Serialization;

// Internal entry points behind ToonSourceGenerationHelpers, so generated code and the reflection-based serializer
// share one implementation of each conversion rule.
public static partial class ToonSerializer
{
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
        var state = new SerializerState();
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
        return SerializeValue(value, type, options, depth, new SerializerState()) ?? ToonNull.Instance;
    }

    internal static object? DeserializeForGeneratedCode(ToonValue value, Type type, ToonSerializerOptions options, int depth)
    {
        return DeserializeValue(value, type, options, depth, new SerializerState());
    }
}
