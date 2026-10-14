using System.Buffers;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Serialization;

// Overloads that take a ToonTypeInfo: the same output as the other overloads, through generated code only, so they are
// compatible with trimming and NativeAOT.
public static partial class ToonSerializer
{
    #region Serialization with a type info

    /// <summary>
    ///     Serializes a value to TOON text with its <see cref="ToonTypeInfo{T}"/>, without reflection.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional serialization options; converters are not supported.</param>
    /// <returns>The TOON text, the same as <see cref="Serialize{T}(T, ToonSerializerOptions)"/> writes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the options contain converters.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    public static string Serialize<T>(T? value, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        options = CheckTypeInfo(typeInfo, options);

        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, typeInfo, options);
        return buffer.ToString();
    }

    /// <summary>
    ///     Serializes a value to UTF-8 encoded TOON text (without a byte order mark) with its <see cref="ToonTypeInfo{T}"/>,
    ///     without reflection or an intermediate string.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional serialization options; converters are not supported.</param>
    /// <returns>The UTF-8 encoded TOON text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the options contain converters.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    public static byte[] SerializeToUtf8Bytes<T>(T? value, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        options = CheckTypeInfo(typeInfo, options);

        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, typeInfo, options);
        return buffer.ToUtf8Bytes();
    }

    /// <summary>
    ///     Serializes a value with its <see cref="ToonTypeInfo{T}"/> and writes it as UTF-8 encoded TOON text (without a
    ///     byte order mark) to a buffer writer, without reflection.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="utf8Destination">The buffer writer to write to; the text is written only after the value is serialized.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional serialization options; converters are not supported.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Destination"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the options contain converters.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    public static void Serialize<T>(IBufferWriter<byte> utf8Destination, T? value, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(utf8Destination);
        options = CheckTypeInfo(typeInfo, options);

        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, typeInfo, options);
        buffer.WriteUtf8(utf8Destination);
    }

    /// <summary>
    ///     Serializes a value with its <see cref="ToonTypeInfo{T}"/> and writes it as UTF-8 encoded TOON text (without a
    ///     byte order mark) to a stream, without reflection.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="utf8Stream">The stream to write to; it is left open. The text is written only after the value is serialized.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional serialization options; converters are not supported.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the options contain converters.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    public static void Serialize<T>(Stream utf8Stream, T? value, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(utf8Stream);
        options = CheckTypeInfo(typeInfo, options);

        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, typeInfo, options);
        buffer.WriteUtf8(utf8Stream);
    }

    /// <summary>
    ///     Asynchronously serializes a value with its <see cref="ToonTypeInfo{T}"/> and writes it as UTF-8 encoded TOON text
    ///     (without a byte order mark) to a stream, without reflection.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="stream">The stream to write to; it is left open.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional serialization options; converters are not supported.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the options contain converters.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async ValueTask SerializeToStreamAsync<T>(T? value, Stream stream, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null,
                                                            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options = CheckTypeInfo(typeInfo, options);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, typeInfo, options);
        await buffer.WriteUtf8Async(stream, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Deserialization with a type info

    /// <summary>
    ///     Deserializes TOON text with a <see cref="ToonTypeInfo{T}"/>, without reflection.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="toonString">The TOON text.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options; converters are not supported.</param>
    /// <returns>The deserialized value, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toonString"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">
    ///     Thrown when the options contain converters, or generated code cannot create <typeparamref name="T"/> (no
    ///     usable constructor).
    /// </exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    public static T? Deserialize<T>(string toonString, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toonString);
        options = CheckTypeInfo(typeInfo, options);

        return DeserializeText(toonString, typeInfo, options);
    }

    /// <summary>
    ///     Deserializes UTF-8 encoded TOON text with a <see cref="ToonTypeInfo{T}"/>, without reflection or an intermediate
    ///     string.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Toon">The UTF-8 encoded TOON text; a byte order mark at the start is ignored.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options; converters are not supported.</param>
    /// <returns>The deserialized value, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">
    ///     Thrown when the options contain converters, or generated code cannot create <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Toon, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        options = CheckTypeInfo(typeInfo, options);

        using var text = ToonTextEncoding.Decode(utf8Toon);
        return DeserializeText(text.Text, typeInfo, options);
    }

    /// <summary>
    ///     Deserializes TOON text in a span of characters with a <see cref="ToonTypeInfo{T}"/>, without reflection.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="toon">The TOON text.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options; converters are not supported.</param>
    /// <returns>The deserialized value, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">
    ///     Thrown when the options contain converters, or generated code cannot create <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    public static T? Deserialize<T>(ReadOnlySpan<char> toon, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        options = CheckTypeInfo(typeInfo, options);

        using var text = ToonPooledText.Copy(toon);
        return DeserializeText(text.Text, typeInfo, options);
    }

    /// <summary>
    ///     Reads UTF-8 encoded TOON text from a stream to its end and deserializes it with a <see cref="ToonTypeInfo{T}"/>,
    ///     without reflection or an intermediate string.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Toon">The stream to read; it is read to the end and left open.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options; converters are not supported.</param>
    /// <returns>The deserialized value, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Toon"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">
    ///     Thrown when the options contain converters, or generated code cannot create <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    public static T? Deserialize<T>(Stream utf8Toon, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(utf8Toon);
        options = CheckTypeInfo(typeInfo, options);

        using var text = ToonTextEncoding.Read(utf8Toon);
        return DeserializeText(text.Text, typeInfo, options);
    }

    /// <summary>
    ///     Asynchronously reads UTF-8 encoded TOON text from a stream to its end and deserializes it with a
    ///     <see cref="ToonTypeInfo{T}"/>, without reflection or an intermediate string.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream to read; it is read to the end and left open.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options; converters are not supported.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The deserialized value, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">
    ///     Thrown when the options contain converters, or generated code cannot create <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async ValueTask<T?> DeserializeFromStreamAsync<T>(Stream stream, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null,
                                                                     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options = CheckTypeInfo(typeInfo, options);

        using var text = await ToonTextEncoding.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return DeserializeText(text.Text, typeInfo, options);
    }

    #endregion

    /// <summary>Checks the arguments common to the overloads that take a type info and returns the options to use.</summary>
    private static ToonSerializerOptions CheckTypeInfo(ToonTypeInfo typeInfo, ToonSerializerOptions? options)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        options ??= ToonSerializerOptions.Default;

        if (options.Converters.Count != 0)
        {
            throw new NotSupportedException(
                "Serializing with a ToonTypeInfo does not support converters, which generated code hands to the reflection-based serializer; " +
                "use the overloads without a type info.");
        }

        return options;
    }

    /// <summary>
    ///     Serializes a value straight to text through generated code; when a value would need reflection, the whole
    ///     document is converted by the generated tree-based method instead (the same text).
    /// </summary>
    private static void SerializeTo<T>(ToonTextBuffer buffer, T? value, ToonTypeInfo<T> typeInfo, ToonSerializerOptions options)
    {
        var start = buffer.Length;

        try
        {
            new DirectWriter(buffer, options, generatedOnly: true).WriteRoot(value, typeof(T));
        }
        catch
        {
            // Also reports errors exactly as the tree-based path does
            DirectWriteFallbacks++;
            buffer.Truncate(start);

            var tree = value is null ? ToonNull.Instance : typeInfo.Serialize(value, options, 0);
            new ToonEncoder(options.ToonOptions).Write(new ToonDocument(tree), buffer);
        }
    }

    /// <summary>
    ///     Deserializes a document straight from the text through generated code; when a value would need reflection (or
    ///     the document has an error), the generated tree-based method converts the document instead.
    /// </summary>
    private static T? DeserializeText<T>(ToonText text, ToonTypeInfo<T> typeInfo, ToonSerializerOptions options)
    {
        var deserialize = typeInfo.Deserialize ?? throw new NotSupportedException(
            $"Generated code cannot create {typeof(T).Name}: it needs a public parameterless constructor or a constructor whose parameters match its properties.");

        if (typeInfo.Read != null)
        {
            try
            {
                return DirectReader.ReadGenerated(text, typeInfo, options);
            }
            catch
            {
                // Report the error exactly as the tree-based path does (or read what direct reading gave up on)
                DirectReadFallbacks++;
            }
        }

        var document = new ToonParser(options.ToonOptions).Parse(text);
        return deserialize(document.Root, options, 0);
    }
}
