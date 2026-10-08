using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToonNet.Core.Models;

namespace ToonNet.Core.Serialization;

/// <summary>
///     Entry points used by code that <c>ToonNet.SourceGenerators</c> generates. Not intended to be called directly.
/// </summary>
/// <remarks>
///     The methods share the conversion rules of <see cref="ToonSerializer"/>, so generated code produces the same
///     output. Generated code calls the primitive methods only when the options contain no converters (otherwise it
///     defers to <see cref="ToonSerializer"/> entirely), which is why they do not consult converters.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ToonSourceGenerationHelpers
{
    /// <summary>
    ///     Converts a string, boolean, number, date/time, <see cref="Guid"/>, <see cref="Uri"/> or enum value.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> is not one of these types.</exception>
    public static ToonValue WritePrimitive<T>(T value)
    {
        return ToonSerializer.WritePrimitiveForGeneratedCode(value);
    }

    /// <summary>
    ///     Reads a string, boolean, number, date/time, <see cref="Guid"/>, <see cref="Uri"/> or enum value (or its
    ///     nullable form), with the same parsing rules and errors as <see cref="ToonSerializer"/>.
    /// </summary>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted.</exception>
    public static T ReadPrimitive<T>(ToonValue value)
    {
        return ToonSerializer.ReadPrimitiveForGeneratedCode<T>(value);
    }

    /// <summary>
    ///     Formats a dictionary key like <see cref="ToonSerializer"/> (invariant culture, ISO 8601 dates, enum names).
    /// </summary>
    public static string FormatKey<TKey>(TKey key) where TKey : notnull
    {
        return ToonSerializer.FormatDictionaryKeyForGeneratedCode(key);
    }

    /// <summary>
    ///     Serializes a value with the reflection-based serializer, continuing at the given depth.
    /// </summary>
    public static ToonValue SerializeWithReflection<T>(T value, ToonSerializerOptions options, int depth)
    {
        return ToonSerializer.SerializeForGeneratedCode(value, typeof(T), options, depth);
    }

    /// <summary>
    ///     Deserializes a value with the reflection-based serializer, continuing at the given depth.
    /// </summary>
    public static T DeserializeWithReflection<T>(ToonValue value, ToonSerializerOptions options, int depth)
    {
        return (T)ToonSerializer.DeserializeForGeneratedCode(value, typeof(T), options, depth)!;
    }

    /// <summary>
    ///     Throws when <paramref name="depth"/> exceeds <see cref="ToonSerializerOptions.MaxDepth"/> or the stack is
    ///     nearly exhausted. Generated code calls this for every object, collection and dictionary it writes, which
    ///     also stops circular references.
    /// </summary>
    /// <exception cref="ToonEncodingException">Thrown when the limit is exceeded.</exception>
    public static void CheckSerializationDepth(ToonSerializerOptions options, int depth)
    {
        if (depth > options.MaxDepth)
        {
            throw ToonEncodingException.Create($"Maximum depth of {options.MaxDepth} exceeded during serialization", suggestion:
                                               "The object graph may contain a circular reference; mark the back-reference with [ToonIgnore].");
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw ToonEncodingException.Create("Object graph is nested too deeply to serialize with the available stack space");
        }
    }

    /// <summary>
    ///     Throws when <paramref name="depth"/> exceeds <see cref="ToonSerializerOptions.MaxDepth"/> or the stack is
    ///     nearly exhausted.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown when the limit is exceeded.</exception>
    public static void CheckDeserializationDepth(ToonSerializerOptions options, int depth)
    {
        if (depth > options.MaxDepth)
        {
            throw new ToonParseException($"Maximum depth of {options.MaxDepth} exceeded during deserialization", 0, 0);
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw new ToonParseException("Document is nested too deeply to deserialize with the available stack space", 0, 0);
        }
    }
}
