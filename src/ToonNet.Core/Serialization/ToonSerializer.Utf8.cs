using System.Diagnostics.CodeAnalysis;
using ToonNet.Core.Encoding;

namespace ToonNet.Core.Serialization;

// UTF-8 and span input and output: text is decoded into, or encoded from, pooled buffers, so no intermediate string is
// created (a large one would be allocated on the large object heap).
public static partial class ToonSerializer
{
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
