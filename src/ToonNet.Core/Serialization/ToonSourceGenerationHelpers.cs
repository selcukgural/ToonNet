using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToonNet.Core.Models;

namespace ToonNet.Core.Serialization;

/// <summary>
///     Entry points used by code that <c>ToonNet.SourceGenerators</c> generates. Not intended to be called directly.
/// </summary>
/// <remarks>
///     The methods share the conversion rules of <see cref="ToonSerializer"/>, so generated code produces the same
///     output. Generated types register themselves (and a <see cref="ToonObjectWriter{T}"/> that writes their objects
///     straight to text) with <c>Register</c>, and <see cref="ToonSerializer"/> then uses the generated code instead of
///     reflection. With converters in the options, generated code asks <see cref="TryWriteConverted{T}"/> and
///     <see cref="TryReadConverted{T}"/> first, which is why the primitive methods do not consult converters.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ToonSourceGenerationHelpers
{
    /// <summary>
    ///     Registers the generated methods of <typeparamref name="T"/>, so <see cref="ToonSerializer"/> (and everything
    ///     built on it) uses them for values of exactly that type. Generated module initializers call this.
    /// </summary>
    /// <param name="serialize">The generated <c>__ToonSerializeValue</c> method.</param>
    /// <param name="deserialize">The generated <c>__ToonDeserializeValue</c> method, or null when there is none.</param>
    public static void Register<T>(Func<T, ToonSerializerOptions, int, ToonValue> serialize,
                                   Func<ToonValue, ToonSerializerOptions, int, T>? deserialize)
    {
        ToonSerializer.RegisterGenerated(serialize, deserialize, null, null);
    }

    /// <summary>
    ///     Registers the generated methods of <typeparamref name="T"/> and the generated writer that writes its objects
    ///     straight to text.
    /// </summary>
    /// <param name="serialize">The generated <c>__ToonSerializeValue</c> method.</param>
    /// <param name="deserialize">The generated <c>__ToonDeserializeValue</c> method, or null when there is none.</param>
    /// <param name="writer">The generated writer, or null when there is none.</param>
    public static void Register<T>(Func<T, ToonSerializerOptions, int, ToonValue> serialize,
                                   Func<ToonValue, ToonSerializerOptions, int, T>? deserialize,
                                   ToonObjectWriter<T>? writer)
    {
        ToonSerializer.RegisterGenerated(serialize, deserialize, writer, null);
    }

    /// <summary>
    ///     Registers the generated methods of <typeparamref name="T"/> with the generated writer and reader that write
    ///     and read its objects straight from text.
    /// </summary>
    /// <param name="serialize">The generated <c>__ToonSerializeValue</c> method.</param>
    /// <param name="deserialize">The generated <c>__ToonDeserializeValue</c> method, or null when there is none.</param>
    /// <param name="writer">The generated writer, or null when there is none.</param>
    /// <param name="reader">The generated reader, or null when there is none.</param>
    public static void Register<T>(Func<T, ToonSerializerOptions, int, ToonValue> serialize,
                                   Func<ToonValue, ToonSerializerOptions, int, T>? deserialize,
                                   ToonObjectWriter<T>? writer,
                                   ToonObjectReader<T>? reader)
    {
        ToonSerializer.RegisterGenerated(serialize, deserialize, writer, reader);
    }

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
    ///     Serializes a value with the reflection-based serializer, continuing at the given depth, after generated code
    ///     has asked <see cref="TryWriteConverted{T}"/> for a converter: the serializer then looks one up only for the
    ///     value's runtime type (as for collection items, which have no declared type of their own).
    /// </summary>
    public static ToonValue SerializeConvertedWithReflection<T>(T value, ToonSerializerOptions options, int depth)
    {
        return ToonSerializer.SerializeForGeneratedCode(value, value?.GetType() ?? typeof(T), options, depth);
    }

    /// <summary>
    ///     Whether the options need the reflection-based reader (case-insensitive keys, unmapped key errors, required
    ///     properties), which code from older generators defers to.
    /// </summary>
    public static bool ReadsWithReflection(ToonSerializerOptions options) => !options.AllowsGeneratedReading;

    /// <summary>
    ///     Writes a non-null value with the converter of the options for it, as <see cref="ToonSerializer"/> finds it:
    ///     for its runtime type, else (with <paramref name="useDeclaredType"/>, for property values) for
    ///     <typeparamref name="T"/>. Returns false when no converter applies; a null result means no value (a property
    ///     is left out).
    /// </summary>
    public static bool TryWriteConverted<T>(T value, ToonSerializerOptions options, bool useDeclaredType, out ToonValue? result) =>
        ToonSerializer.TryWriteConverted(value, options, useDeclaredType, out result);

    /// <summary>
    ///     Reads a value with the converter of the options for <typeparamref name="T"/> (or, for a non-null value of a
    ///     nullable type, its underlying type), as <see cref="ToonSerializer"/> does; false when no converter applies.
    /// </summary>
    public static bool TryReadConverted<T>(ToonValue value, ToonSerializerOptions options, out T result) =>
        ToonSerializer.TryReadConverted(value, options, out result);

    /// <summary>
    ///     Gets the value of a key as <see cref="ToonSerializer"/> does: the exact key, or with <paramref name="ignoreCase"/>
    ///     (case-insensitive reading of a name that no other member name equals ignoring case) the last key that equals
    ///     the name ignoring case.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetProperty(ToonObject obj, string name, bool ignoreCase, out ToonValue value) =>
        ignoreCase ? ToonSerializer.TryGetKeyIgnoringCase(obj, name, out value) : obj.Properties.TryGetValue(name, out value!);

    /// <summary>
    ///     The error <see cref="ToonSerializer"/> reports for a key that matches no member of <typeparamref name="T"/> with
    ///     <see cref="ToonUnmappedMemberHandling.Disallow"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The object value.</param>
    public static ToonSerializationException UnmappedKeyError<T>(string key, ToonValue value) =>
        ToonSerializer.UnmappedKeyErrorForGeneratedCode(key, typeof(T), value);

    /// <summary>
    ///     The error <see cref="ToonSerializer"/> reports for a required property of <typeparamref name="T"/> without a key
    ///     with <see cref="ToonSerializerOptions.RespectRequiredMembers"/>.
    /// </summary>
    /// <param name="name">The serialized name of the property.</param>
    /// <param name="value">The object value.</param>
    public static ToonSerializationException MissingRequiredError<T>(string name, ToonValue value) =>
        ToonSerializer.MissingRequiredErrorForGeneratedCode(name, typeof(T), value);

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
        if (depth > options.ValidatedMaxDepth)
        {
            throw ToonEncodingException.AtPath($"Maximum depth of {options.MaxDepth} exceeded during serialization", "$", suggestion:
                                               "The object graph may contain a circular reference; mark the back-reference with [ToonIgnore].");
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw ToonEncodingException.AtPath("Object graph is nested too deeply to serialize with the available stack space", "$");
        }
    }

    /// <summary>
    ///     Throws when <paramref name="depth"/> exceeds <see cref="ToonSerializerOptions.MaxDepth"/> or the stack is
    ///     nearly exhausted.
    /// </summary>
    /// <exception cref="ToonSerializationException">Thrown when the limit is exceeded.</exception>
    public static void CheckDeserializationDepth(ToonSerializerOptions options, int depth)
    {
        if (depth > options.ValidatedMaxDepth)
        {
            throw ToonSerializer.DepthError($"Maximum depth of {options.MaxDepth} exceeded during deserialization", typeof(object), "$");
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw ToonSerializer.DepthError("Document is nested too deeply to deserialize with the available stack space", typeof(object), "$");
        }
    }

    /// <summary>
    ///     Puts a property name or dictionary key in front of the path of an error that <see cref="ToonSerializer"/> or
    ///     generated code raised while converting the value at that key, as the error leaves the value; other errors
    ///     are left as they are. Returns false, for use as an exception filter.
    /// </summary>
    /// <param name="exception">The error.</param>
    /// <param name="name">The serialized property name or the dictionary key.</param>
    public static bool AddPath(ToonException exception, string name)
    {
        exception.PrependPath(ToonException.PathOf(name));
        return false;
    }

    /// <summary>
    ///     Puts an item index in front of the path of an error raised while converting the item, as
    ///     <see cref="AddPath(ToonException, string)"/> does for a key. Returns false, for use as an exception filter.
    /// </summary>
    /// <param name="exception">The error.</param>
    /// <param name="index">The index of the item.</param>
    public static bool AddPath(ToonException exception, int index)
    {
        exception.PrependPath(ToonException.PathOf(index));
        return false;
    }
}
