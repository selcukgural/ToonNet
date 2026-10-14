using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using ToonNet.Core.Encoding;

namespace ToonNet.Core.Serialization;

// UTF-8 and span input and output: text is decoded into, or encoded from, pooled buffers, so no intermediate string is
// created (a large one would be allocated on the large object heap).
public static partial class ToonSerializer
{
    #region UTF-8 serialization

    /// <summary>
    ///     Serializes an object to UTF-8 encoded TOON text (without a byte order mark), without creating an intermediate
    ///     string.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>The UTF-8 encoded TOON text.</returns>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static byte[] SerializeToUtf8Bytes<T>(T? value, ToonSerializerOptions? options = null)
    {
        return SerializeToUtf8Bytes(value, typeof(T), options);
    }

    /// <summary>
    ///     Serializes an object of the specified type to UTF-8 encoded TOON text (without a byte order mark), without
    ///     creating an intermediate string.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The type of object to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>The UTF-8 encoded TOON text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static byte[] SerializeToUtf8Bytes(object? value, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, type, options ?? ToonSerializerOptions.Default);
        return buffer.ToUtf8Bytes();
    }

    /// <summary>
    ///     Serializes an object and writes it as UTF-8 encoded TOON text (without a byte order mark) to a buffer writer,
    ///     such as a <c>PipeWriter</c> or an <see cref="ArrayBufferWriter{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="utf8Destination">The buffer writer to write to; the text is written only after the value is serialized.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Destination"/> is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static void Serialize<T>(IBufferWriter<byte> utf8Destination, T? value, ToonSerializerOptions? options = null)
    {
        Serialize(utf8Destination, value, typeof(T), options);
    }

    /// <summary>
    ///     Serializes an object of the specified type and writes it as UTF-8 encoded TOON text (without a byte order mark)
    ///     to a buffer writer, such as a <c>PipeWriter</c> or an <see cref="ArrayBufferWriter{T}"/>.
    /// </summary>
    /// <param name="utf8Destination">The buffer writer to write to; the text is written only after the value is serialized.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The type of object to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Destination"/> or <paramref name="type"/> is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static void Serialize(IBufferWriter<byte> utf8Destination, object? value, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(utf8Destination);
        ArgumentNullException.ThrowIfNull(type);

        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, type, options ?? ToonSerializerOptions.Default);
        buffer.WriteUtf8(utf8Destination);
    }

    /// <summary>
    ///     Serializes an object and writes it as UTF-8 encoded TOON text (without a byte order mark) to a stream, through
    ///     pooled buffers.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="utf8Stream">The stream to write to; it is left open. The text is written only after the value is serialized.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static void Serialize<T>(Stream utf8Stream, T? value, ToonSerializerOptions? options = null)
    {
        Serialize(utf8Stream, value, typeof(T), options);
    }

    /// <summary>
    ///     Serializes an object of the specified type and writes it as UTF-8 encoded TOON text (without a byte order mark)
    ///     to a stream, through pooled buffers.
    /// </summary>
    /// <param name="utf8Stream">The stream to write to; it is left open. The text is written only after the value is serialized.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The type of object to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> or <paramref name="type"/> is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static void Serialize(Stream utf8Stream, object? value, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(utf8Stream);
        ArgumentNullException.ThrowIfNull(type);

        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, type, options ?? ToonSerializerOptions.Default);
        buffer.WriteUtf8(utf8Stream);
    }

    #endregion

    #region Asynchronous streams in the System.Text.Json shape

    /// <summary>
    ///     Asynchronously serializes a value and writes it to a stream as UTF-8 (the shape of
    ///     <c>JsonSerializer.SerializeAsync</c>; the same as <see cref="SerializeToStreamAsync{T}(T, Stream, ToonSerializerOptions, CancellationToken)"/>).
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="utf8Stream">The stream to write to; it is left open.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ValueTask SerializeAsync<T>(Stream utf8Stream, T? value, ToonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Stream);
        return SerializeToUtf8StreamAsync(value, typeof(T), utf8Stream, options, cancellationToken);
    }

    /// <summary>
    ///     Asynchronously serializes a value of the specified type and writes it to a stream as UTF-8.
    /// </summary>
    /// <param name="utf8Stream">The stream to write to; it is left open.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The type of the value.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> or <paramref name="type"/> is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ValueTask SerializeAsync(Stream utf8Stream, object? value, Type type, ToonSerializerOptions? options = null,
                                           CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Stream);
        ArgumentNullException.ThrowIfNull(type);
        return SerializeToUtf8StreamAsync(value, type, utf8Stream, options, cancellationToken);
    }

    /// <summary>
    ///     Asynchronously reads UTF-8 encoded TOON text from a stream to its end and deserializes it (the shape of
    ///     <c>JsonSerializer.DeserializeAsync</c>; the same as <see cref="DeserializeFromStreamAsync{T}(Stream, ToonSerializerOptions, CancellationToken)"/>).
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Stream">The stream to read; it is read to the end and left open.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The deserialized value, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask<T?> DeserializeAsync<T>(Stream utf8Stream, ToonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        return (T?)await DeserializeFromStreamAsync(utf8Stream, typeof(T), options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously reads UTF-8 encoded TOON text from a stream to its end and deserializes it to the specified type.
    /// </summary>
    /// <param name="utf8Stream">The stream to read; it is read to the end and left open.</param>
    /// <param name="type">The target type to deserialize to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The deserialized value, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> or <paramref name="type"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ValueTask<object?> DeserializeAsync(Stream utf8Stream, Type type, ToonSerializerOptions? options = null,
                                                      CancellationToken cancellationToken = default)
    {
        return DeserializeFromStreamAsync(utf8Stream, type, options, cancellationToken);
    }

    /// <summary>
    ///     Asynchronously serializes a value with its <see cref="ToonTypeInfo{T}"/> and writes it to a stream as UTF-8,
    ///     without reflection.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="utf8Stream">The stream to write to; it is left open.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional serialization options; converters are not supported.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the options contain converters.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    public static ValueTask SerializeAsync<T>(Stream utf8Stream, T? value, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null,
                                              CancellationToken cancellationToken = default) =>
        SerializeToStreamAsync(value, utf8Stream, typeInfo, options, cancellationToken);

    /// <summary>
    ///     Asynchronously reads UTF-8 encoded TOON text from a stream to its end and deserializes it with a
    ///     <see cref="ToonTypeInfo{T}"/>, without reflection.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Stream">The stream to read; it is read to the end and left open.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options; converters are not supported.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The deserialized value, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the options contain converters or ask for case-insensitive reading.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    public static ValueTask<T?> DeserializeAsync<T>(Stream utf8Stream, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null,
                                                    CancellationToken cancellationToken = default) =>
        DeserializeFromStreamAsync(utf8Stream, typeInfo, options, cancellationToken);

    #endregion

    #region UTF-8 and span deserialization

    /// <summary>
    ///     Deserializes UTF-8 encoded TOON text to an object, without creating an intermediate string.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Toon">The UTF-8 encoded TOON text; a byte order mark at the start is ignored.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Toon, ToonSerializerOptions? options = null)
    {
        return (T?)Deserialize(utf8Toon, typeof(T), options);
    }

    /// <summary>
    ///     Deserializes UTF-8 encoded TOON text to an object of the specified type, without creating an intermediate string.
    /// </summary>
    /// <param name="utf8Toon">The UTF-8 encoded TOON text; a byte order mark at the start is ignored.</param>
    /// <param name="type">The target type to deserialize to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static object? Deserialize(ReadOnlySpan<byte> utf8Toon, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        using var text = ToonTextEncoding.Decode(utf8Toon);
        return DeserializeText(text.Text, type, options ?? ToonSerializerOptions.Default);
    }

    /// <summary>
    ///     Deserializes TOON text in a span of characters to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="toon">The TOON text.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static T? Deserialize<T>(ReadOnlySpan<char> toon, ToonSerializerOptions? options = null)
    {
        return (T?)Deserialize(toon, typeof(T), options);
    }

    /// <summary>
    ///     Deserializes TOON text in a span of characters to an object of the specified type.
    /// </summary>
    /// <param name="toon">The TOON text.</param>
    /// <param name="type">The target type to deserialize to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static object? Deserialize(ReadOnlySpan<char> toon, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        using var text = ToonPooledText.Copy(toon);
        return DeserializeText(text.Text, type, options ?? ToonSerializerOptions.Default);
    }

    /// <summary>
    ///     Reads UTF-8 encoded TOON text from a stream to its end and deserializes it to an object, without creating an
    ///     intermediate string.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Toon">The stream to read; it is read to the end and left open.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Toon"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static T? Deserialize<T>(Stream utf8Toon, ToonSerializerOptions? options = null)
    {
        return (T?)Deserialize(utf8Toon, typeof(T), options);
    }

    /// <summary>
    ///     Reads UTF-8 encoded TOON text from a stream to its end and deserializes it to an object of the specified type,
    ///     without creating an intermediate string.
    /// </summary>
    /// <param name="utf8Toon">The stream to read; it is read to the end and left open.</param>
    /// <param name="type">The target type to deserialize to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Toon"/> or <paramref name="type"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static object? Deserialize(Stream utf8Toon, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(utf8Toon);
        ArgumentNullException.ThrowIfNull(type);

        using var text = ToonTextEncoding.Read(utf8Toon);
        return DeserializeText(text.Text, type, options ?? ToonSerializerOptions.Default);
    }

    /// <summary>
    ///     Asynchronously reads UTF-8 encoded TOON text from a stream to its end and deserializes it to an object of the
    ///     specified type, without creating an intermediate string.
    /// </summary>
    /// <param name="stream">The stream to read; it is read to the end and left open.</param>
    /// <param name="type">The target type to deserialize to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The deserialized object, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="type"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask<object?> DeserializeFromStreamAsync(Stream stream, Type type, ToonSerializerOptions? options = null,
                                                                      CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);

        using var text = await ToonTextEncoding.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return DeserializeText(text.Text, type, options ?? ToonSerializerOptions.Default);
    }

    #endregion
}
